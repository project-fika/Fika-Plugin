using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class RollCylinderPacket : IPoolSubPacket
{
    private RollCylinderPacket()
    {

    }

    public static RollCylinderPacket FromValue(bool rollToZeroCamora)
    {
        RollCylinderPacket packet = FirearmSubPacketPoolManager.Instance.GetPacket<RollCylinderPacket>(EFirearmSubPacketType.RollCylinder);
        packet.RollToZeroCamora = rollToZeroCamora;
        return packet;
    }

    public static RollCylinderPacket CreateInstance()
    {
        return new();
    }

    public bool RollToZeroCamora;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller && controller.Weapon is RevolverItemClass)
        {
            controller.RollCylinder(RollToZeroCamora);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RollToZeroCamora);
    }

    public void Deserialize(NetDataReader reader)
    {
        RollToZeroCamora = reader.GetBool();
    }

    public void Dispose()
    {
        RollToZeroCamora = false;
    }
}
