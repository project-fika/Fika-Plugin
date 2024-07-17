using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets
{
    public struct QuestDropItemPacket(string nickname, string itemId, string zoneId) : INetSerializable
    {
        public string Nickname = nickname;
        public string ItemId = itemId;
        public string ZoneId = zoneId;

        public void Deserialize(NetDataReader reader)
        {
            ItemId = reader.GetString();
            ZoneId = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ItemId);
            writer.Put(ZoneId);
        }
    }
}
