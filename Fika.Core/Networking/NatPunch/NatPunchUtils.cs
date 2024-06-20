using LiteNetLib.Utils;
using LiteNetLib;
using STUN;
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EFT.UI;
using BepInEx.Logging;

namespace Fika.Core.Networking.NatPunch
{
    public class StunIPEndPoint
    {
        public IPEndPoint Local { get; set; }
        public IPEndPoint Remote { get; set; }

        public StunIPEndPoint(IPEndPoint localIPEndPoint, IPEndPoint remoteIPEndPoint)
        {
            Local = localIPEndPoint;
            Remote = remoteIPEndPoint;
        }
    }

    public static class NatPunchUtils
    {
        private static ManualLogSource logger = Logger.CreateLogSource("Fika.NatPunchUtils");

        public static StunIPEndPoint CreateStunEndPoint(int localPort = 0)
        {
            var stunUdpClient = new UdpClient();
            var stunQueryResult = new STUNQueryResult();

            try
            {
                if (localPort > 0)
                {
                    stunUdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, localPort));
                }

                IPAddress stunIp = Array.Find(Dns.GetHostEntry("stun.l.google.com").AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                int stunPort = 19302;

                IPEndPoint stunQueryIpEndPoint = new IPEndPoint(stunIp, stunPort);

                stunQueryResult = STUNClient.Query(stunUdpClient.Client, stunQueryIpEndPoint, STUNQueryType.ExactNAT, NATTypeDetectionRFC.Rfc3489);

                if (stunQueryResult.PublicEndPoint != null)
                {
                    var stunIpEndPointResult = new StunIPEndPoint((IPEndPoint)stunUdpClient.Client.LocalEndPoint, stunQueryResult.PublicEndPoint);
                    return stunIpEndPointResult;
                }
            }
            catch (Exception ex)
            {
               logger.LogError($"Error during STUN query: {ex.Message}");
            }
            finally
            {
                stunUdpClient.Client.Close();
            }

            return null;
        }

        public static async void StunQueryRoutine(NetManager netManager, FikaNatPunchServer fikaNatPunchServer, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                netManager.Stop();

                int localPort = 0;

                StunIPEndPoint currentStunIpEndPoint = fikaNatPunchServer.StunIPEndpoint;

                if (currentStunIpEndPoint != null)
                    localPort = currentStunIpEndPoint.Local.Port;

                StunIPEndPoint stunIpEndPoint = CreateStunEndPoint(localPort);

                if(stunIpEndPoint == null)
                {
                    logger.LogError("Error during STUN query routine: Stun Endpoint is null.");
                    break;
                }

                fikaNatPunchServer.StunIPEndpoint = stunIpEndPoint;

                netManager.Start(stunIpEndPoint.Local.Port);

                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        public static void PunchNat(NetManager netManager, IPEndPoint endPoint)
        {
            // bogus punch data
            var resp = new NetDataWriter();
            resp.Put("fika.punchnat");

            // send a couple of packets to punch a hole
            for (int i = 0; i < 10; i++)
            {
                netManager.SendUnconnectedMessage(resp, endPoint);
            }
        }
    }
}
