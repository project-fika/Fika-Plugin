using BepInEx.Logging;
using EFT.UI;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using LiteNetLib;
using LiteNetLib.Utils;
using SPT.Common.Http;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Fika.Core.Networking
{
    public class FikaPingingClient : MonoBehaviour, INetEventListener, INatPunchListener
    {
        public NetManager NetClient;
        private readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("Fika.PingingClient");
        private IPEndPoint remoteEndPoint;
        private IPEndPoint localEndPoint;
        public bool Received = false;
        private Coroutine keepAliveRoutine;

        public bool Init(string serverId)
        {
            NetClient = new(this)
            {
                UnconnectedMessagesEnabled = true,
                NatPunchEnabled = true
            };

            GetHostRequest body = new(serverId);
            GetHostResponse result = FikaRequestHandler.GetHost(body);

            FikaBackendUtils.IsHostNatPunch = result.NatPunch;

            NetClient.Start();

            if (FikaBackendUtils.IsHostNatPunch)
            {
                NetClient.NatPunchModule.Init(this);

                string natHost = RequestHandler.Host.Replace("http://", "").Split(':')[0];
                int natPort = 6970;

                NetClient.NatPunchModule.SendNatIntroduceRequest(natHost, natPort, $"client:{serverId}");
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

                remoteEndPoint = new(IPAddress.Parse(ip), port);
                if (!string.IsNullOrEmpty(localIp))
                {
                    localEndPoint = new(IPAddress.Parse(localIp), port);
                }
            }

            return true;
        }

        public void PingEndPoint(string message)
        {
            NetDataWriter writer = new();
            writer.Put(message);

            if (remoteEndPoint != null)
            {
                NetClient.SendUnconnectedMessage(writer, remoteEndPoint);
            }
            if (localEndPoint != null)
            {
                NetClient.SendUnconnectedMessage(writer, localEndPoint);
            }
        }

        public void StartKeepAliveRoutine()
        {
            keepAliveRoutine = StartCoroutine(KeepAlive());
        }

        public void StopKeepAliveRoutine()
        {
            if(keepAliveRoutine != null)
            {
                StopCoroutine(keepAliveRoutine);
            }
        }

        public IEnumerator KeepAlive()
        {
            while(true)
            {
                PingEndPoint("fika.keepalive");
                NetClient.PollEvents();
                NetClient.NatPunchModule.PollEvents();

                yield return new WaitForSeconds(1.0f);
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
                switch(result)
                {
                    case "fika.hello":
                        Received = true;
                        FikaBackendUtils.RemoteIp = remoteEndPoint.Address.ToString();
                        FikaBackendUtils.RemotePort = remoteEndPoint.Port;
                        FikaBackendUtils.LocalPort = NetClient.LocalPort;
                        break;
                    case "fika.keepalive":
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
            localEndPoint = natLocalEndPoint;
            remoteEndPoint = natRemoteEndPoint;

            NetDataWriter data = new();
            data.Put("fika.hello");

            for (int i = 0; i < 10; i++)
            {
                NetClient.SendUnconnectedMessage(data, natLocalEndPoint);
                NetClient.SendUnconnectedMessage(data, natRemoteEndPoint);
                Task.Delay(100);
            }
        }
    }
}
