using JsonType;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct TransitInteractPacket : INetSerializable
	{
		public int NetId;
		public GStruct176 Data;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Data = new()
			{
				hasInteraction = true,
				pointId = reader.GetInt(),
				keyId = reader.GetString(),
				time = (EDateTime)reader.GetByte()
			};
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(Data.pointId);
			writer.Put(Data.keyId);
			writer.Put((byte)Data.time);
		}
	}
}
