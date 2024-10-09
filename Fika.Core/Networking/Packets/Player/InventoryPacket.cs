using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct InventoryPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public uint CallbackId;
		public byte[] OperationBytes;

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(CallbackId);
			writer.PutByteArray(OperationBytes);
		}

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			CallbackId = reader.GetUInt();
			OperationBytes = reader.GetByteArray();
		}
	}
}
