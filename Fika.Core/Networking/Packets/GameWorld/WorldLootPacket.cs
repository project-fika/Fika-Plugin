using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct WorldLootPacket(bool isRequest) : INetSerializable
	{
		public bool IsRequest = isRequest;
		public byte[] Data;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			if (!IsRequest)
			{
				Data = reader.GetByteArray();
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			if (!IsRequest)
			{
				writer.PutByteArray(Data);
			}
		}
	}
}
