using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct ResyncInventoryPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public string MongoId = string.Empty;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			MongoId = reader.GetString();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(MongoId);
		}
	}
}
