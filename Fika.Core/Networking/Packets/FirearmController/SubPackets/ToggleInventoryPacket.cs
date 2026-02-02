using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ToggleInventoryPacket : IPoolSubPacket
{
    private ToggleInventoryPacket()
    {

    }

    public static ToggleInventoryPacket FromValue(bool open)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ToggleInventoryPacket>(EFirearmSubPacketType.ToggleInventory);
        packet.Open = open;
        return packet;
    }

    public static ToggleInventoryPacket CreateInstance()
    {
        return new();
    }

    public bool Open;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.SetInventoryOpened(Open);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Open);
    }

    public void Deserialize(NetDataReader reader)
    {
        Open = reader.GetBool();
    }

    public void Dispose()
    {
        Open = false;
    }
}
