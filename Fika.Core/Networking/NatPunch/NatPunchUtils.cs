using LiteNetLib.Utils;
using LiteNetLib;
using STUN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Networking.NatPunch
{
    public class StunIpEndPoint
    {
        public IPEndPoint Local { get; set; }
        public IPEndPoint Remote { get; set; }

        public StunIpEndPoint(IPEndPoint localIpEndPoint, IPEndPoint remoteIpEndPoint)
        {
            Local = localIpEndPoint;
            Remote = remoteIpEndPoint;
        }
    }

    public static class NatPunchUtils
    {
        public static StunIpEndPoint CreateStunEndPoint(int localPort = 0)
        {
            var stunUdpClient = new UdpClient();
            var stunQueryResult = new STUNQueryResult();

            try
            {
                if (localPort > 0)
                    stunUdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, localPort));

                IPAddress stunIp = Array.Find(Dns.GetHostEntry("stun.l.google.com").AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                int stunPort = 19302;

                var stunQueryIpEndPoint = new IPEndPoint(stunIp, stunPort);

                stunQueryResult = STUNClient.Query(stunUdpClient.Client, stunQueryIpEndPoint, STUNQueryType.ExactNAT, NATTypeDetectionRFC.Rfc3489);

                if (stunQueryResult.PublicEndPoint != null)
                {
                    var stunIpEndPointResult = new StunIpEndPoint((IPEndPoint)stunUdpClient.Client.LocalEndPoint, stunQueryResult.PublicEndPoint);
                    return stunIpEndPointResult;
                }
            }
            catch (Exception ex)
            {
                //log exception
            }
            finally
            {
                stunUdpClient.Client.Close();
            }

            return null;
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
