using Comfort.Common;
using Dissonance.Networking;
using Fika.Core.Networking.Packets.Communication;
using System;

namespace Fika.Core.Networking.VOIP;

public class FikaVOIPClient(ICommsNetworkState network) : BaseClient<FikaVOIPServer, FikaVOIPClient, FikaVOIPPeer>(network)
{
    private readonly FikaCommsNetwork _commsNet = (FikaCommsNetwork)network;

    public override void Connect()
    {
        Connected();
    }

    protected override void ReadMessages()
    {

    }

    public override void Disconnect()
    {
        base.Disconnect();
    }

    public override void SendVoiceData(ArraySegment<byte> encodedAudio)
    {
        TalkClass.SetTalkDateTime();
        if (!TalkClass.Blocked)
        {
            base.SendVoiceData(encodedAudio);
        }
    }

    protected override void SendReliable(ArraySegment<byte> packet)
    {
        if (_commsNet.PreprocessPacketToServer(packet))
        {
            return;
        }

        Singleton<IFikaNetworkManager>.Instance.SendVOIPData(packet, DeliveryMethod.ReliableOrdered);
    }

    protected override void SendUnreliable(ArraySegment<byte> packet)
    {
        if (_commsNet.PreprocessPacketToServer(packet))
        {
            return;
        }

        Singleton<IFikaNetworkManager>.Instance.SendVOIPData(packet, DeliveryMethod.Sequenced);
    }
}
