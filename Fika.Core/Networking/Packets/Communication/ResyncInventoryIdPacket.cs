using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
    public struct ResyncInventoryIdPacket(ushort netId) : INetSerializable
    {
        public ushort NetId = netId;
        public MongoID? MongoId;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();
            MongoId = reader.GetMongoID();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutMongoID(MongoId);
        }
    }
}
