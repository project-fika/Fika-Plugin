using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class ReloadBoltActionPacket : IPoolSubPacket
{
    private ReloadBoltActionPacket()
    {

    }

    public static ReloadBoltActionPacket FromValue()
    {
        return FirearmSubPacketPoolManager.Instance.GetPacket<ReloadBoltActionPacket>(EFirearmSubPacketType.ReloadBoltAction);
    }

    public static ReloadBoltActionPacket CreateInstance()
    {
        return new();
    }

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.HandleObservedBoltAction();
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
