using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ArmorDamagePacket : INetSerializable
    {
        public ushort NetId;
        public MongoID? ItemId;
        public float Durability;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            ItemId = reader.GetMongoID();
            Durability = reader.GetFloat();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutMongoID(ItemId);
            writer.Put(Durability);
        }
    }
}
