using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController
{
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
}
