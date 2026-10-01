using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct CheckChamberPacket : IFirearmPacket
{
    public EFirearmPacketType Type => EFirearmPacketType.CheckChamber;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.CheckChamber();
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        // do nothing
    }
}
