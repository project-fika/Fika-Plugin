using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Player
{
    public struct ArmorDamagePacket : INetSerializable
    {
        public int NetId;
        public MongoID ItemId;
        public float Durability;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
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
