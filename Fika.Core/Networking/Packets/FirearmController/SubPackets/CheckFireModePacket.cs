using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public sealed class CheckFireModePacket : IPoolSubPacket
{
    private CheckFireModePacket()
    {

    }

    public static CheckFireModePacket FromValue()
    {
        return FirearmSubPacketPoolManager.Instance.GetPacket<CheckFireModePacket>(EFirearmSubPacketType.CheckFireMode);
    }

    public static CheckFireModePacket CreateInstance()
    {
        return new();
    }

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.CheckFireMode();
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
