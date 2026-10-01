using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct RollCylinderPacket : IFirearmPacket
{
    public RollCylinderPacket(bool rollToZeroCamora)
    {
        RollToZeroCamora = rollToZeroCamora;
    }

    public RollCylinderPacket(NetDataReader reader)
    {
        RollToZeroCamora = reader.GetBool();
    }

    public readonly bool RollToZeroCamora;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller && controller.Weapon is Revolver)
        {
            controller.RollCylinder(RollToZeroCamora);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(RollToZeroCamora);
    }

    public EFirearmPacketType Type => EFirearmPacketType.RollCylinder;
}