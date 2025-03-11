using Comfort.Common;
using Dissonance.Networking;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System;

namespace Fika.Core.Networking.VOIP
{
    class FikaVOIPClient : BaseClient<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer>
    {
        private readonly FikaCommsNetwork commsNet;

        public FikaVOIPClient(ICommsNetworkState network) : base(network)
        {
            commsNet = (FikaCommsNetwork)network;
        }

        public override void Connect()
        {
            Connected();
        }

        protected override void ReadMessages()
        {

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
            if (FikaBackendUtils.IsServer)
            {
                Singleton<FikaServer>.Instance.VOIPServer.NetworkReceivedPacket(new(new LocalPeer()), packet);
                return;
            }

            VOIPPacket voipPacket = new()
            {
                Data = packet.Array
            };

            Singleton<IFikaNetworkManager>.Instance.SendVOIPPacket(ref voipPacket, true);
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            if (FikaBackendUtils.IsServer)
            {
                Singleton<FikaServer>.Instance.VOIPServer.NetworkReceivedPacket(new(new LocalPeer()), packet);
                return;
            }

            VOIPPacket voipPacket = new()
            {
                Data = packet.Array
            };

            Singleton<IFikaNetworkManager>.Instance.SendVOIPPacket(ref voipPacket, false);
        }
    }
}
