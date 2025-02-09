using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ArmorDamagePacket : INetSerializable
    {
        public int NetId;
        public string[] ItemIds;
        public float[] Durabilities;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            ItemIds = reader.GetStringArray();
            Durabilities = reader.GetFloatArray();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutArray(ItemIds);
            writer.PutArray(Durabilities);
        }
    }
}
