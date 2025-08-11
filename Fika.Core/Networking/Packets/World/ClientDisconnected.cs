using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;
using Fika.Core.Utils;
using LiteNetLib.Utils;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Networking.Packets.World
{
    public class ClientDisconnected : IPoolSubPacket
    {
        public string Name;

        private ClientDisconnected() { }

        public static ClientDisconnected CreateInstance()
        {
            return new ClientDisconnected();
        }

        public static ClientDisconnected FromValue(string name)
        {
            ClientDisconnected packet = GenericSubPacketPoolManager.Instance.GetPacket<ClientDisconnected>(EGenericSubPacketType.ClientDisconnected);
            packet.Name = name;
            return packet;
        }

        public void Execute(FikaPlayer player = null)
        {
            string message = string.Format(LocaleUtils.UI_PLAYER_DISCONNECTED.Localized(), ColorizeText(EColor.BLUE, Name));
            NotificationManagerClass.DisplayMessageNotification(message);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Name);
        }

        public void Deserialize(NetDataReader reader)
        {
            Name = reader.GetString();
        }

        public void Dispose()
        {
            Name = null;
        }
    }
}
