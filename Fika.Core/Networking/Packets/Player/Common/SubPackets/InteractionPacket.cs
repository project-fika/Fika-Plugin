using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct InteractionPacket : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.Interaction;

    public InteractionPacket(EInteraction interaction)
    {
        Interaction = interaction;
    }

    public InteractionPacket(NetDataReader reader)
    {
        Interaction = reader.GetEnum<EInteraction>();
    }

    public readonly EInteraction Interaction;

    public readonly void Execute(FikaPlayer player)
    {
        player.SetInteractInHands(Interaction);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Interaction);
    }
}
