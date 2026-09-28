using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public readonly struct CancelGrenadePacket : IFirearmPacket
{
    public EFirearmPacketType Type => EFirearmPacketType.CancelGrenade;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedGrenadeController grenadeController)
        {
            grenadeController.CurrentOperation.PutGrenadeBack();
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        // do nothing
    }
}
