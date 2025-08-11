using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public class ToggleLauncherPacket : IPoolSubPacket
    {
        private ToggleLauncherPacket()
        {

        }

        public static ToggleLauncherPacket FromValue()
        {
            return FirearmSubPacketPoolManager.Instance.GetPacket<ToggleLauncherPacket>(EFirearmSubPacketType.ToggleLauncher);
        }

        public static ToggleLauncherPacket CreateInstance()
        {
            return new();
        }

        public void Execute(FikaPlayer player)
        {
            if (player.HandsController is ObservedFirearmController controller)
            {
                controller.ToggleLauncher();
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
