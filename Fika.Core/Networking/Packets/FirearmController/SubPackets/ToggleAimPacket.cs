using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct ToggleAimPacket : IFirearmPacket
{
    public ToggleAimPacket(int aimingIndex)
    {
        AimingIndex = aimingIndex;
    }

    public ToggleAimPacket(NetDataReader reader)
    {
        AimingIndex = reader.GetInt();
    }

    public readonly int AimingIndex;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.SetAim(AimingIndex);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(AimingIndex);
    }

    public EFirearmPacketType Type => EFirearmPacketType.ToggleAim;
}
