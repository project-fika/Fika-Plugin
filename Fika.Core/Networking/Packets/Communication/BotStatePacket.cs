using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct BotStatePacket : INetSerializable
	{
		public int NetId;
		public EStateType Type;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Type = (EStateType)reader.GetByte();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put((byte)Type);
		}

		public enum EStateType
		{
			LoadBot,
			DisposeBot,
			EnableBot,
			DisableBot
		}
	}
}
