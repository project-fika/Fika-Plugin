using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ArmorDamagePacket : INetSerializable
    {
        public int NetId;
        public string ItemId;
        public float Durability;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            ItemId = reader.GetString();
            Durability = reader.GetPackedFloat(0f, 200f);
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(ItemId);
            writer.PutPackedFloat(Durability, 0f, 200f);
        }
    }
}
