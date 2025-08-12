using BepInEx.Logging;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fika.Core.Networking;

/// <summary>
/// Client used to verify that a connection can be established before initializing the <see cref="FikaClient"/> and <see cref="CoopGame"/>
/// </summary>
public class FikaPingingClient : MonoBehaviour, INetEventListener, INatPunchListener
{
    public NetManager NetClient;
    public bool Received;
    public bool Rejected;
    public bool InProgress;

    private ManualLogSource _logger;
    private IPEndPoint _remoteEndPoint;
    private IPEndPoint _localEndPoint;
    private Coroutine _keepAliveRoutine;

    public void Awake()
    {
        _logger = Logger.CreateLogSource("Fika.PingingClient");
    }

    public bool Init(string serverId)
    {
        NetClient = new(this)
        {
            UnconnectedMessagesEnabled = true,
            NatPunchEnabled = true
        };

        GetHostRequest body = new(serverId);
        GetHostResponse result = FikaRequestHandler.GetHost(body);
        FikaBackendUtils.ServerGuid = result.ServerGuid;
        _logger.LogInfo(result.ToString());

        FikaBackendUtils.IsHostNatPunch = result.NatPunch;
        FikaBackendUtils.IsHeadlessGame = result.IsHeadless;

        NetClient.Start();

        if (FikaBackendUtils.IsHostNatPunch)
        {
            NetClient.NatPunchModule.Init(this);

            string natPunchServerIP = FikaPlugin.Instance.NatPunchServerIP;
            int natPunchServerPort = FikaPlugin.Instance.NatPunchServerPort;
            string token = $"client:{serverId}";

            NetClient.NatPunchModule.SendNatIntroduceRequest(natPunchServerIP, natPunchServerPort, token);

            _logger.LogInfo($"SendNatIntroduceRequest: {natPunchServerIP}:{natPunchServerPort}");
        }
        else
        {
            string ip = result.Ips[0];
            string localIp = null;
            if (result.Ips.Length > 1)
            {
                localIp = result.Ips[1];
            }
            int port = result.Port;

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

            _remoteEndPoint = ResolveRemoteAddress(ip, port);
            if (!string.IsNullOrEmpty(localIp))
            {
                _localEndPoint = new(IPAddress.Parse(localIp), port);
            }
        }

        return true;
    }

    private IPEndPoint ResolveRemoteAddress(string ip, int port)
    {
        if (IPAddress.TryParse(ip, out IPAddress address))
        {
            return new(address, port);
        }

        IPHostEntry hostEntry = Dns.GetHostEntry(ip);
        if (hostEntry != null & hostEntry.AddressList.Length > 0)
        {
            return new(hostEntry.AddressList[0], port);
        }

        throw new ParseException($"ResolveRemoteAddress::Could not parse the address {ip}");
    }

    public void PingEndPoint(string message, bool reconnect = false)
    {
        NetDataWriter writer = new();
        writer.Put(message);
        writer.Put(reconnect);

        if (_localEndPoint != null)
        {
            NetClient.SendUnconnectedMessage(writer.AsReadOnlySpan, _localEndPoint);
        }
        if (_remoteEndPoint != null)
        {
            NetClient.SendUnconnectedMessage(writer.AsReadOnlySpan, _remoteEndPoint);
        }
    }

    public void StartKeepAliveRoutine()
    {
        _keepAliveRoutine = StartCoroutine(KeepAlive());
    }

    public void StopKeepAliveRoutine()
    {
        if (_keepAliveRoutine != null)
        {
            StopCoroutine(_keepAliveRoutine);
        }
    }

    public IEnumerator KeepAlive()
    {
        WaitForSeconds waitForSeconds = new(1f);
        while (true)
        {
            PingEndPoint("fika.keepalive");
            NetClient.PollEvents();
            NetClient.NatPunchModule.PollEvents();

            yield return waitForSeconds;
        }
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Do nothing
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        // Do nothing
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Do nothing
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        // Do nothing
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (reader.TryGetString(out string result))
        {
            switch (result)
            {
                case "fika.hello":
                    if (Received)
                    {
                        break;
                    }
                    Received = true;
                    FikaBackendUtils.RemoteIp = remoteEndPoint.Address.ToString();
                    FikaBackendUtils.RemotePort = remoteEndPoint.Port;
                    FikaBackendUtils.LocalPort = NetClient.LocalPort;
                    _logger.LogInfo($"Got response from {FikaBackendUtils.RemoteIp}:{FikaBackendUtils.RemotePort}, using LocalPort: {NetClient.LocalPort}");
                    break;
                case "fika.keepalive":
                    // Do nothing
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

    public void OnPeerConnected(NetPeer peer)
    {
        // Do nothing
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // Do nothing
    }

    public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
    {
        // Do nothing
    }

    public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
    {
        // Do nothing
    }

    public void OnNatIntroductionResponse(IPEndPoint natLocalEndPoint, IPEndPoint natRemoteEndPoint, string token)
    {
        _logger.LogInfo($"OnNatIntroductionResponse: {_remoteEndPoint}");

        _localEndPoint = natLocalEndPoint;
        _remoteEndPoint = natRemoteEndPoint;

        Task.Run(async () =>
        {
            NetDataWriter data = new();
            data.Put("fika.hello");

            for (int i = 0; i < 20; i++)
            {
                NetClient.SendUnconnectedMessage(data.AsReadOnlySpan, _localEndPoint);
                NetClient.SendUnconnectedMessage(data.AsReadOnlySpan, _remoteEndPoint);
                await Task.Delay(250);
            }
        });
    }
}
