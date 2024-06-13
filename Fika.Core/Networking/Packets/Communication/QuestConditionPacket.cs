using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets
{
    public struct QuestConditionPacket(string nickname, string id, string sourceId) : INetSerializable
    {
        public string Nickname = nickname;
        public string Id = id;
        public string SourceId = sourceId;
        public string ItemId;

        public void Deserialize(NetDataReader reader)
        {
            Nickname = reader.GetString();
            Id = reader.GetString();
            SourceId = reader.GetString();
            ItemId = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Nickname);
            writer.Put(Id);
            writer.Put(SourceId);
            writer.Put(ItemId);
        }
    }
}
