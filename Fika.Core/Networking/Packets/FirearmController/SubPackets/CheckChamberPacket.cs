using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public class CheckChamberPacket : IPoolSubPacket
{
    private CheckChamberPacket()
    {

    }

    public static CheckChamberPacket FromValue()
    {
        return FirearmSubPacketPoolManager.Instance.GetPacket<CheckChamberPacket>(EFirearmSubPacketType.CheckChamber);
    }

    public static CheckChamberPacket CreateInstance()
    {
        return new();
    }

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.CheckChamber();
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
