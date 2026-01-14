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
    private bool _reconnect;

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
    public bool Init(string serverId, bool reconnect)
    {
        NetClient = new(this)
        {
            UnconnectedMessagesEnabled = true,
            NatPunchEnabled = true
        };

        _reconnect = reconnect;

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
            var token = $"Client:{FikaBackendUtils.ServerGuid}";

            _logger.LogInfo("Sending NAT Introduce Request to NAT Punch Server...");
            NetClient.NatPunchModule.SendNatIntroduceRequest(natPunchServerIP, natPunchServerPort, token);
        }

        var ip = result.Ips[0];
        var port = result.Port;

        _endPoints = [];

        foreach (var address in result.Ips)
        {
            _endPoints.Add(NetworkUtils.ResolveRemoteAddress(address, port));
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
    /// Sends a ping message to the configured endpoints.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="reconnect">Whether this is a reconnect attempt.</param>
    public void PingEndPoint(string message)
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
        _writer.Put(_reconnect);

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

        _logger.LogInfo($"Picked candidate {ep.Address}:{ep.Port}, using LocalPort: {NetClient.LocalPort}");
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
                        _logger.LogInfo($"Received candidate: {remoteEndPoint}");
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

    private bool IsPrivate(IPAddress ip)
    {
        switch (ip.AddressFamily)
        {
            case AddressFamily.InterNetwork: // IPv4
                {
#if DEBUG
                    _logger.LogInfo($"Checking {ip} which is IPv4");
#endif
                    var b = ip.GetAddressBytes();
                    return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
                }
            case AddressFamily.InterNetworkV6: // IPv6
                {
#if DEBUG
                    _logger.LogInfo($"Checking {ip} which is IPv6");
#endif
                    var b = ip.GetAddressBytes();
                    // fc00::/7 > check top 7 bits of first byte (0b1111_1100 = 0xfc)
                    return (b[0] & 0b1111_1100) == 0b1111_1100;
                }
            default:
                return false;
        }
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
    /// Handles the NAT introduction response from NAT Punch Server and proceeds to NAT Punch the host endpoint.
    /// </summary>
    /// <param name="localEndPoint">The local NAT endpoint.</param>
    /// <param name="remoteEndPoint">The remote NAT endpoint.</param>
    /// <param name="token">The NAT punch token.</param>
    public void OnNatIntroductionResponse(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
    {
        _logger.LogInfo($"Received server endpoints from Nat Punch Server. Local: {localEndPoint}, Remote: {remoteEndPoint}.");
        
        // Add received server endpoints
        _endPoints.Add(localEndPoint);
        _endPoints.Add(remoteEndPoint);

        // Nat Punch
        Task.Run(async () => 
        {
            NetDataWriter _natPunchWriter = new();

            _natPunchWriter.Put(FikaBackendUtils.ServerGuid.ToString());
            _writer.Put(_reconnect);

            _logger.LogInfo($"Punching server endpoints. Local: {localEndPoint}, Remote: {remoteEndPoint}.");

            for (var i = 0; i < 50; i++)
            {
                NetClient.SendUnconnectedMessage(_writer.AsReadOnlySpan, localEndPoint);
                NetClient.SendUnconnectedMessage(_writer.AsReadOnlySpan, remoteEndPoint);

                await Task.Delay(50);
            }

            _natPunchWriter.Reset();
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
