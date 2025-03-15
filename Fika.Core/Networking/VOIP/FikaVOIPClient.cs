using Comfort.Common;
using Dissonance.Networking;
using Fika.Core.Coop.Utils;
using System;

namespace Fika.Core.Networking.VOIP
{
    public class FikaVOIPClient(ICommsNetworkState network) : BaseClient<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer>(network)
    {
        private readonly FikaCommsNetwork commsNet = (FikaCommsNetwork)network;

        public override void Connect()
        {
            Connected();
        }

        protected override void ReadMessages()
        {

        }

        public override void Disconnect()
        {
            if (!commsNet.Mode.IsServerEnabled())
            {
                Singleton<IFikaNetworkManager>.Instance.RegisterPacket<VOIPPacket>(OnVoicePacketReceived); 
            }
            base.Disconnect();
        }

        private void OnVoicePacketReceived(VOIPPacket packet)
        {
            // Do nothing
        }

        public override void SendVoiceData(ArraySegment<byte> encodedAudio)
        {
            GClass1207.SetTalkDateTime();
            if (!GClass1207.Blocked)
            {
                base.SendVoiceData(encodedAudio);
            }
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            if (commsNet.PreprocessPacketToServer(packet))
            {
                return;
            }

            VOIPPacket voipPacket = new()
            {
                Data = packet.Array
            };

            Singleton<IFikaNetworkManager>.Instance.SendVOIPPacket(ref voipPacket);
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            if (commsNet.PreprocessPacketToServer(packet))
            {
                return;
            }

            Singleton<IFikaNetworkManager>.Instance.SendVOIPData(packet);
        }
    }
}
