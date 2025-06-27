using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct QuestItemPacket : INetSerializable
    {
        public string Nickname;
        public MongoID? ItemId;

        public void Deserialize(NetDataReader reader)
        {
            Nickname = reader.GetString();
            ItemId = reader.GetMongoID();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(Nickname);
            writer.PutMongoID(ItemId);
        }
    }
}
