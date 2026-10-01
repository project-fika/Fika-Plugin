using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct CheckFireModePacket : IFirearmPacket
{
    public EFirearmPacketType Type => EFirearmPacketType.CheckFireMode;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.CheckFireMode();
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        // do nothing
    }
}
