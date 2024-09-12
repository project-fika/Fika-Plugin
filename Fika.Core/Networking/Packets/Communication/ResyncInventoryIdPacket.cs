using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct ResyncInventoryIdPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public string MongoId = string.Empty;
		public ushort NextId = 0;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			MongoId = reader.GetString();
			NextId = reader.GetUShort();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(MongoId);
			writer.Put(NextId);
		}
	}
}
