using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public class FirearmLootPacket : IPoolSubPacket
    {
        private FirearmLootPacket()
        {

        }

        public static FirearmLootPacket FromValue()
        {
            return FirearmSubPacketPoolManager.Instance.GetPacket<FirearmLootPacket>(EFirearmSubPacketType.Loot);
        }

        public static FirearmLootPacket CreateInstance()
        {
            return new();
        }

        public void Execute(FikaPlayer player)
        {
            player.HandsController.Loot(true);
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
