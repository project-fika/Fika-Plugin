using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct QuestConditionPacket : INetSerializable
	{
		public string Nickname;
		public string Id;
		public string SourceId;

		public void Deserialize(NetDataReader reader)
		{
			Nickname = reader.GetString();
			Id = reader.GetString();
			SourceId = reader.GetString();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(Nickname);
			writer.Put(Id);
			writer.Put(SourceId);
		}
	}
}
