using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BepInEx.Logging;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;

namespace Fika.Core.Networking;

/// <summary>
/// Client used to verify that a connection can be established before initializing the <see cref="FikaClient"/> and <see cref="CoopGame"/>
/// </summary>
public class FikaPingingClient : MonoBehaviour, INetEventListener, INatPunchListener
{
    /// <summary>
    /// The network client manager instance.
    /// </summary>
    public NetManager NetClient;

    /// <summary>
    /// Indicates if a successful response has been received from the server.
    /// </summary>
    public bool Received;

    /// <summary>
    /// Indicates if the connection attempt was rejected by the server.
    /// </summary>
    public bool Rejected;

    /// <summary>
    /// Indicates if the server is currently in progress (busy).
    /// </summary>
    public bool InProgress;

    private ManualLogSource _logger;
    private List<IPEndPoint> _endPoints;
    private NetDataWriter _writer;

    private List<IPEndPoint> _candidates;
    private float _firstResponseTime;
    private bool _hasResponse;

    private const float _responseWaitSeconds = 1f;

    /// <summary>
    /// Initializes the logger for the pinging client.
    /// </summary>
    public void Awake()
    {
        _logger = Logger.CreateLogSource("Fika.PingingClient");
    }

    /// <summary>
    /// Initializes the pinging client and attempts to resolve the server endpoint.
    /// </summary>
    /// <param name="serverId">The server identifier to connect to.</param>
    /// <returns>True if initialization was successful, otherwise false.</returns>
    public bool Init(string serverId)
    {
        NetClient = new(this)
        {
            UnconnectedMessagesEnabled = true,
            NatPunchEnabled = true
        };

        _endPoints = [];
        _candidates = [];
        _writer = new();

        GetHostRequest body = new(serverId);
        var result = FikaRequestHandler.GetHost(body);
        FikaBackendUtils.ServerGuid = result.ServerGuid;
        _logger.LogInfo(result.ToString());

        FikaBackendUtils.IsHostNatPunch = result.NatPunch;
        FikaBackendUtils.IsHeadlessGame = result.IsHeadless;

        NetClient.Start();

        if (FikaBackendUtils.IsHostNatPunch)
        {
            NetClient.NatPunchModule.Init(this);

            var natPunchServerIP = FikaPlugin.Instance.NatPunchServerIP;
            var natPunchServerPort = FikaPlugin.Instance.NatPunchServerPort;
            var token = $"Client:{serverId}";

            NetClient.NatPunchModule.SendNatIntroduceRequest(natPunchServerIP, natPunchServerPort, token);

            _logger.LogInfo($"SendNatIntroduceRequest: {natPunchServerIP}:{natPunchServerPort}");
        }
        else
        {
            var ip = result.Ips[0];
            var port = result.Port;
            _endPoints = new List<IPEndPoint>(result.Ips.Length);
            foreach (var address in result.Ips)
            {
                _endPoints.Add(ResolveRemoteAddress(address, port));
            }

            if (string.IsNullOrEmpty(ip))
            {
                _logger.LogError("IP was empty when pinging!");
                return false;
            }

            if (port == default)
            {
                _logger.LogError("Port was empty when pinging!");
                return false;
            }
        }

        return true;
    }

    private void RemoveEndpointIfExists(IPEndPoint remote)
    {
        for (var i = _endPoints.Count - 1; i >= 0; i--)
        {
            var ep = _endPoints[i];
            if (ep.Address.Equals(remote.Address) && ep.Port == remote.Port)
            {
                _endPoints.RemoveAt(i);
                _logger.LogInfo($"Stopped pinging {ep}");
                return;
            }
        }
    }

    /// <summary>
    /// Resolves a remote address from a string IP or hostname and port.
    /// </summary>
    /// <param name="ip">The IP address or hostname.</param>
    /// <param name="port">The port number.</param>
    /// <returns>The resolved <see cref="IPEndPoint"/>.</returns>
    /// <exception cref="ParseException">Thrown if the address cannot be resolved.</exception>
    private IPEndPoint ResolveRemoteAddress(string ip, int port)
    {
        if (IPAddress.TryParse(ip, out var address))
        {
            return new(address, port);
        }

        var hostEntry = Dns.GetHostEntry(ip);
        if (hostEntry?.AddressList.Length > 0)
        {
            return new(hostEntry.AddressList[0], port);
        }

        throw new ParseException($"ResolveRemoteAddress::Could not parse the address {ip}");
    }

    /// <summary>
    /// Sends a ping message to the configured endpoints.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="reconnect">Whether this is a reconnect attempt.</param>
    public void PingEndPoint(string message, bool reconnect = false)
    {
        // Finalize selection after wait window
        if (!Received && _hasResponse && Time.realtimeSinceStartup - _firstResponseTime >= _responseWaitSeconds)
        {
            var selected = SelectBestCandidate();

            if (selected != null)
            {
                CommitEndpoint(selected);
                Received = true;
                _endPoints.Clear(); // stop all further pings
                return;
            }

            _logger.LogError($"{_responseWaitSeconds} seconds has passed, but no candidate could be found?");
        }

        _writer.Reset();
        _writer.Put(message);
        _writer.Put(reconnect);

        foreach (var ipEndPoint in _endPoints)
        {
            NetClient.SendUnconnectedMessage(_writer.AsReadOnlySpan, ipEndPoint);
        }
    }

    private void CommitEndpoint(IPEndPoint ep)
    {
        FikaBackendUtils.RemoteIp = ep.Address.ToString();
        FikaBackendUtils.RemotePort = ep.Port;
        FikaBackendUtils.LocalPort = NetClient.LocalPort;

        _logger.LogInfo($"Got response from {ep.Address}:{ep.Port}, using LocalPort: {NetClient.LocalPort}");
    }

    private IPEndPoint SelectBestCandidate()
    {
        if (_candidates.Count == 0)
        {
            return null;
        }

        // prefer LAN
        var lan = _candidates.FirstOrDefault(c => IsPrivate(c.Address));
        if (lan != null)
        {
            return lan;
        }

        // otherwise first responder WAN
        return _candidates[0];
    }

    /// <inheritdoc/>
    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        // Do nothing
    }

    /// <summary>
    /// Handles unconnected network messages received from remote endpoints.
    /// </summary>
    /// <param name="remoteEndPoint">The remote endpoint that sent the message.</param>
    /// <param name="reader">The packet reader containing the message data.</param>
    /// <param name="messageType">The type of unconnected message received.</param>
    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (reader.TryGetString(out var result))
        {
            switch (result)
            {
                case "fika.hello":
                    RemoveEndpointIfExists(remoteEndPoint);

                    if (!_hasResponse)
                    {
                        _firstResponseTime = Time.realtimeSinceStartup;
                        _hasResponse = true;
                    }

                    if (!_candidates.Any(c =>
                        c.Address.Equals(remoteEndPoint.Address) &&
                        c.Port == remoteEndPoint.Port))
                    {
                        _candidates.Add(remoteEndPoint);
                        _logger.LogInfo($"Candidate: {remoteEndPoint}");
                    }

                    break;
                case "fika.inprogress":
                    InProgress = true;
                    break;
                case "fika.reject":
                    Rejected = true;
                    break;
                default:
                    _logger.LogError("Data was not as expected");
                    break;
            }
        }
        else
        {
            _logger.LogError("Could not parse string");
        }
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var b = ip.GetAddressBytes();

        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
    }

    /// <inheritdoc/>
    public void OnPeerConnected(NetPeer peer)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
    {
        // Do nothing
    }

    /// <summary>
    /// Handles the NAT introduction response and sends hello messages to both local and remote endpoints.
    /// </summary>
    /// <param name="natLocalEndPoint">The local NAT endpoint.</param>
    /// <param name="natRemoteEndPoint">The remote NAT endpoint.</param>
    /// <param name="token">The NAT punch token.</param>
    public void OnNatIntroductionResponse(IPEndPoint natLocalEndPoint, IPEndPoint natRemoteEndPoint, string token)
    {
        _logger.LogInfo($"OnNatIntroductionResponse: {natRemoteEndPoint}");
        _endPoints.Add(natLocalEndPoint);

        Task.Run(async () =>
        {
            for (var i = 0; i < 20; i++)
            {
                PingEndPoint("fika.hello");
                await Task.Delay(250);
            }
        });
    }

    public void OnDestroy()
    {
        _endPoints.Clear();
        _candidates.Clear();
        _endPoints = null;
        _candidates = null;
        _writer.Reset();
        _writer = null;
    }
}
