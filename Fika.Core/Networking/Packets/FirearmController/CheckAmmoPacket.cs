using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController;

public class CheckAmmoPacket : IPoolSubPacket
{
    private CheckAmmoPacket()
    {

    }

    public static CheckAmmoPacket FromValue()
    {
        return FirearmSubPacketPoolManager.Instance.GetPacket<CheckAmmoPacket>(EFirearmSubPacketType.CheckAmmo);
    }

    public static CheckAmmoPacket CreateInstance()
    {
        return new();
    }

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.CheckAmmo();
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        // do nothing
    }

    public void Deserialize(NetDataReader reader)
    {
        // do nothing
    }

    public void Dispose()
    {
        // do nothing
    }
}
