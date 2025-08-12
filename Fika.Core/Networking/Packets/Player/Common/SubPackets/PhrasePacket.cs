using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public class PhrasePacket : IPoolSubPacket
{
    private PhrasePacket()
    {

    }

    public static PhrasePacket CreateInstance()
    {
        return new();
    }

    public static PhrasePacket FromValue(EPhraseTrigger trigger, int index)
    {
        PhrasePacket packet = CommonSubPacketPoolManager.Instance.GetPacket<PhrasePacket>(ECommonSubPacketType.Phrase);
        packet.PhraseTrigger = trigger;
        packet.PhraseIndex = index;
        return packet;
    }

    public EPhraseTrigger PhraseTrigger;
    public int PhraseIndex;

    public void Execute(FikaPlayer player)
    {
        if (player.gameObject.activeSelf && player.HealthController.IsAlive)
        {
            player.Speaker.PlayDirect(PhraseTrigger, PhraseIndex);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(PhraseTrigger);
        writer.Put(PhraseIndex);
    }

    public void Deserialize(NetDataReader reader)
    {
        PhraseTrigger = reader.GetEnum<EPhraseTrigger>();
        PhraseIndex = reader.GetInt();
    }

    public void Dispose()
    {
        PhraseTrigger = EPhraseTrigger.None;
        PhraseIndex = 0;
    }
}
