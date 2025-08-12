using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController;

public class UnderbarrelSightingRangeUpPacket : IPoolSubPacket
{
    private UnderbarrelSightingRangeUpPacket()
    {

    }

    public static UnderbarrelSightingRangeUpPacket FromValue()
    {
        return FirearmSubPacketPoolManager.Instance.GetPacket<UnderbarrelSightingRangeUpPacket>(EFirearmSubPacketType.UnderbarrelSightingRangeUp);
    }

    public static UnderbarrelSightingRangeUpPacket CreateInstance()
    {
        return new();
    }

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.UnderbarrelSightingRangeUp();
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
