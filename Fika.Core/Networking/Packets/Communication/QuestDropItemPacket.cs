using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets
{
    public struct QuestDropItemPacket(string itemId, string zoneId) : INetSerializable
    {
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
