using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public class InteractionPacket : IPoolSubPacket
{
    private InteractionPacket()
    {

    }

    public static InteractionPacket CreateInstance()
    {
        return new();
    }

    public static InteractionPacket FromValue(EInteraction interaction)
    {
        InteractionPacket packet = CreateInstance();
        packet.Interaction = interaction;
        return packet;
    }

    public EInteraction Interaction;

    public void Execute(FikaPlayer player)
    {
        player.SetInteractInHands(Interaction);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Interaction);
    }

    public void Deserialize(NetDataReader reader)
    {
        Interaction = reader.GetEnum<EInteraction>();
    }

    public void Dispose()
    {
        Interaction = default;
    }
}
