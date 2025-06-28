using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ArmorDamagePacket : INetSerializable
    {
        public int NetId;
        public MongoID? ItemId;
        public float Durability;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            ItemId = reader.GetNullableMongoID();
            Durability = reader.GetFloat();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutNullableMongoID(ItemId);
            writer.Put(Durability);
        }
    }
}
