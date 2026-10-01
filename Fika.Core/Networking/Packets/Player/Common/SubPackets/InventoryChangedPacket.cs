using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct InventoryChangedPacket(bool inventoryOpen) : IPlayerPacket
{
    public EPlayerPacketType Type => EPlayerPacketType.InventoryChanged;

    public readonly bool InventoryOpen = inventoryOpen;

    public readonly void Execute(FikaPlayer player)
    {
        player.HandleInventoryOpenedPacket(InventoryOpen);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(InventoryOpen);
    }
}
