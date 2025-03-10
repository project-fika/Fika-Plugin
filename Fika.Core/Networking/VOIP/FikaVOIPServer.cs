using Dissonance.Networking;
using Fika.Core.Coop.Utils;
using System;

namespace Fika.Core.Networking.VOIP
{
    class FikaVOIPServer : BaseServer<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer>
    {
        private readonly FikaCommsNetwork commsNet;

        public FikaVOIPServer(FikaCommsNetwork commsNetwork)
        {
            commsNet = commsNetwork;
        }

        protected override void ReadMessages()
        {

        }

        protected override void SendReliable(FikaVOIPPeer connection, ArraySegment<byte> packet)
        {
            if (packet.Array != null && packet.Array.Length == 0)
            {
                FikaGlobals.LogError("Packet length was 0!");
                return;
            }

            connection.Peer.SendData(packet, true);
        }

        protected override void SendUnreliable(FikaVOIPPeer connection, ArraySegment<byte> packet)
        {
            if (packet.Array != null && packet.Array.Length == 0)
            {
                FikaGlobals.LogError("Packet length was 0!");
                return;
            }

            connection.Peer.SendData(packet, false);
        }
    }
}
