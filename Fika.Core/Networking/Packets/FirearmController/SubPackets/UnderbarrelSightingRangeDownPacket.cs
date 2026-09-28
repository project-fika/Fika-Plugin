using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct UnderbarrelSightingRangeDownPacket : IFirearmPacket
{
    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.UnderbarrelSightingRangeDown();
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        // do nothing
    }

    public EFirearmPacketType Type => EFirearmPacketType.UnderbarrelSightingRangeDown;
}
