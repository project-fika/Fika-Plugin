using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController;

public class UnderbarrelSightingRangeDownPacket : IPoolSubPacket
{
    private UnderbarrelSightingRangeDownPacket()
    {

    }

    public static UnderbarrelSightingRangeDownPacket FromValue()
    {
        return FirearmSubPacketPoolManager.Instance.GetPacket<UnderbarrelSightingRangeDownPacket>(EFirearmSubPacketType.UnderbarrelSightingRangeDown);
    }


    public static UnderbarrelSightingRangeDownPacket CreateInstance()
    {
        return new();
    }

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.UnderbarrelSightingRangeDown();
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
