using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking
{
	public struct CommonPlayerPacket : INetSerializable
	{
		public int NetId;
		public ECommonSubPacketType Type;
		public ISubPacket SubPacket;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Type = (ECommonSubPacketType)reader.GetByte();
			SubPacket = reader.GetCommonSubPacket(Type);
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put((byte)Type);
			writer.PutCommonSubPacket(SubPacket);
		}
	}
}
