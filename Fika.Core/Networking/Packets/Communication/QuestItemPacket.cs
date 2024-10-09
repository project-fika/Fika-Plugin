using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct QuestItemPacket : INetSerializable
	{
		public string Nickname;
		public string ItemId;

		public void Deserialize(NetDataReader reader)
		{
			Nickname = reader.GetString();
			ItemId = reader.GetString();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Nickname);
			writer.Put(ItemId);
		}
	}
}
