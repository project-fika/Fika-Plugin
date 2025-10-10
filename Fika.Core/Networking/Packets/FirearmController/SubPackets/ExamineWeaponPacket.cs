using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.FirearmController.SubPackets;

public class ExamineWeaponPacket : IPoolSubPacket
{
    private ExamineWeaponPacket()
    {

    }

    public static ExamineWeaponPacket FromValue()
    {
        return FirearmSubPacketPoolManager.Instance.GetPacket<ExamineWeaponPacket>(EFirearmSubPacketType.ExamineWeapon);
    }

    public static ExamineWeaponPacket CreateInstance()
    {
        return new();
    }

    public void Execute(FikaPlayer player)
    {
        if (player.HandsController is ObservedFirearmController controller)
        {
            controller.ExamineWeapon();
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
