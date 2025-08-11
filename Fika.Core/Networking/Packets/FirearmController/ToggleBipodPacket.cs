using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public class ToggleBipodPacket : IPoolSubPacket
    {
        private ToggleBipodPacket()
        {

        }

        public static ToggleBipodPacket FromValue()
        {
            return FirearmSubPacketPoolManager.Instance.GetPacket<ToggleBipodPacket>(EFirearmSubPacketType.ToggleBipod);
        }


        public static ToggleBipodPacket CreateInstance()
        {
            return new();
        }

        public void Execute(FikaPlayer player)
        {
            if (player.HandsController is ObservedFirearmController controller)
            {
                controller.ToggleBipod();
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
