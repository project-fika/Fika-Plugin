using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct PhrasePacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.Phrase;

    public PhrasePacket(EPhraseTrigger trigger, int index)
    {
        PhraseTrigger = trigger;
        PhraseIndex = index;
    }

    public PhrasePacket(NetDataReader reader)
    {
        PhraseTrigger = reader.GetEnum<EPhraseTrigger>();
        PhraseIndex = reader.GetInt();
    }

    public readonly EPhraseTrigger PhraseTrigger;
    public readonly int PhraseIndex;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.gameObject.activeSelf && player.HealthController.IsAlive)
        {
            player.Speaker.PlayDirect(PhraseTrigger, PhraseIndex);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(PhraseTrigger);
        writer.Put(PhraseIndex);
    }
}
