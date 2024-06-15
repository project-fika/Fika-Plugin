using BepInEx.Logging;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.Networking.NatPunch;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Fika.Core.Networking
{
    internal class FikaPingingClient(string serverId) : INetEventListener
    {
        public NetManager NetClient;
        private readonly ManualLogSource _logger = Logger.CreateLogSource("Fika.PingingClient");
        private readonly string serverId = serverId;
        private IPEndPoint remoteEndPoint;
        private IPEndPoint localEndPoint;
        private IPEndPoint remoteStunEndPoint;
        public bool Received = false;

        public bool Init()
        {
            NetClient = new(this)
            {
                UnconnectedMessagesEnabled = true
            };

            GetHostRequest body = new(serverId);
            GetHostResponse result = FikaRequestHandler.GetHost(body);

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

            remoteEndPoint = new(IPAddress.Parse(ip), port);
            if (!string.IsNullOrEmpty(localIp))
            {
                localEndPoint = new(IPAddress.Parse(localIp), port);
            }

            //TODO: add config to enable this

            var localStunEndPoint = NatPunchUtils.CreateStunEndPoint(FikaPlugin.UDPPort.Value);

            FikaNatPunchClient fikaNatPunchClient = new FikaNatPunchClient();

            fikaNatPunchClient.Connect();

            GetHostStunRequest getStunRequest = new GetHostStunRequest(localStunEndPoint.Remote.Address.ToString(), localStunEndPoint.Remote.Port);
            GetHostStunResponse getStunResponse = fikaNatPunchClient.GetHostStun(getStunRequest).Result;

            remoteStunEndPoint = new IPEndPoint(IPAddress.Parse(getStunResponse.StunIp), getStunResponse.StunPort); 

            NetClient.Start();

            return true;
        }

        public void PingEndPoint()
        {
            if (Received)
            {
                return;
            }

            NetDataWriter writer = new();
            writer.Put("fika.hello");

            NetClient.SendUnconnectedMessage(writer, remoteEndPoint);
            if (localEndPoint != null)
            {
                NetClient.SendUnconnectedMessage(writer, localEndPoint);
            }

            if (remoteStunEndPoint != null)
            {
                NetClient.SendUnconnectedMessage(writer, remoteStunEndPoint);
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
            if (Received)
            {
                return;
            }
            _logger.LogInfo("Received response from server, parsing...");

            if (reader.TryGetString(out string result))
            {
                if (result == "fika.hello")
                {
                    Received = true;
                    FikaBackendUtils.RemoteIp = remoteEndPoint.Address.ToString();
                    FikaBackendUtils.RemotePort = remoteEndPoint.Port;
                }
                else
                {
                    _logger.LogError("Data was not as expected");
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
    }
}
