using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public class SideEffectPacket : INetSerializable
	{
		public string ItemId;
		public float Value;

		public void Deserialize(NetDataReader reader)
		{
			ItemId = reader.GetString();
			Value = reader.GetFloat();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(ItemId);
			writer.Put(Value);
		}
	}
}
