using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct InventoryChangedPacket
{
    public InventoryChangedPacket(bool inventoryOpen)
    {
        InventoryOpen = inventoryOpen;
    }

    public readonly bool InventoryOpen;

    public readonly void Execute(FikaPlayer player)
    {
        player.HandleInventoryOpenedPacket(InventoryOpen);
    }
}
