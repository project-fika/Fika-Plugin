using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Player
{
	public struct InventoryHashPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public int Hash;
		public string Response;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Hash = reader.GetInt();
			Response = reader.GetString();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(Hash);
			writer.Put(Response);
		}
	}
}
