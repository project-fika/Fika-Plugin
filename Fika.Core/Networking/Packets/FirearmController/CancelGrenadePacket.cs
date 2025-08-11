using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public class CancelGrenadePacket : IPoolSubPacket
    {
        private CancelGrenadePacket()
        {

        }

        public static CancelGrenadePacket FromValue()
        {
            return FirearmSubPacketPoolManager.Instance.GetPacket<CancelGrenadePacket>(EFirearmSubPacketType.CancelGrenade);
        }

        public static CancelGrenadePacket CreateInstance()
        {
            return new();
        }

        public void Execute(FikaPlayer player)
        {
            if (player.HandsController is ObservedGrenadeController grenadeController)
            {
                grenadeController.vmethod_3();
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
