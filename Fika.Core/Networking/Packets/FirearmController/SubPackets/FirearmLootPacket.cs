using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct FirearmLootPacket : IFirearmPacket
{
    public readonly void Execute(FikaPlayer player)
    {
        player.HandsController.Loot(true);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        // do nothing
    }

    public EFirearmPacketType Type => EFirearmPacketType.Loot;
}
