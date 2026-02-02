using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ToggleAimPacket : IPoolSubPacket
{
    private ToggleAimPacket()
    {

    }

    public static ToggleAimPacket FromValue(int aimingIndex)
    {
        var packet = FirearmSubPacketPoolManager.Instance.GetPacket<ToggleAimPacket>(EFirearmSubPacketType.ToggleAim);
        packet.AimingIndex = aimingIndex;
        return packet;
    }

    public static ToggleAimPacket CreateInstance()
    {
        return new();
    }

    public int AimingIndex;

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.SetAim(AimingIndex);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(AimingIndex);
    }

    public void Deserialize(NetDataReader reader)
    {
        AimingIndex = reader.GetInt();
    }

    public void Dispose()
    {
        AimingIndex = 0;
    }
}
