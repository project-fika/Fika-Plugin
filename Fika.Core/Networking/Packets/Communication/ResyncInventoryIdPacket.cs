using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Communication
{
    public struct ResyncInventoryIdPacket(int netId) : INetSerializable
    {
        public int NetId = netId;
        public MongoID MongoId;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            MongoId = reader.GetMongoID();
        }

        public readonly void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutMongoID(MongoId);
        }
    }
}
