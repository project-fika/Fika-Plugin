using EFT;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct ResyncInventoryIdPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public MongoID? MongoId;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			MongoId = reader.GetMongoID();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.PutMongoID(MongoId);
		}
	}
}
