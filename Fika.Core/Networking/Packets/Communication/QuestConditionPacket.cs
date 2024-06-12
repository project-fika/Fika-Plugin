using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets
{
    public struct QuestConditionPacket(string nickname, string id, string sourceId) : INetSerializable
    {
        public string Nickname = nickname;
        public string Id = id;
        public string SourceId = sourceId;

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetString();
            SourceId = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(SourceId);
        }
    }
}
