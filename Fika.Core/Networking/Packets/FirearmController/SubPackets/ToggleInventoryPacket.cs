using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ToggleInventoryPacket : IFirearmPacket
{
    public ToggleInventoryPacket(bool open)
    {
        Open = open;
    }

    public ToggleInventoryPacket(NetDataReader reader)
    {
        Open = reader.GetBool();
    }

    public readonly bool Open;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.SetInventoryOpened(Open);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Open);
    }

    public EFirearmPacketType Type => EFirearmPacketType.ToggleInventory;
}