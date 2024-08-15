using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct QuestItemPacket(string nickname, string itemId) : INetSerializable
	{
		public string Nickname = nickname;
		public string ItemId = itemId;

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
