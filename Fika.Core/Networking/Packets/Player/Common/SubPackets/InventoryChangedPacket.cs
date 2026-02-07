using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class InventoryChangedPacket : IPoolSubPacket
{
    private InventoryChangedPacket()
    {

    }

    public static InventoryChangedPacket CreateInstance()
    {
        return new();
    }

    public static InventoryChangedPacket FromValue(bool inventoryOpen)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<InventoryChangedPacket>(ECommonSubPacketType.InventoryChanged);
        packet.InventoryOpen = inventoryOpen;
        return packet;
    }

    public bool InventoryOpen;

    public void Execute(FikaPlayer player)
    {
        player.HandleInventoryOpenedPacket(InventoryOpen);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(InventoryOpen);
    }

    public void Deserialize(NetDataReader reader)
    {
        InventoryOpen = reader.GetBool();
    }

    public void Dispose()
    {
        InventoryOpen = false;
    }
}
