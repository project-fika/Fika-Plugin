// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking
{
	/// <summary>
	/// Packet used for many different things to reduce packet bloat
	/// </summary>
	/// <param name="packageType"></param>
	public class GenericPacket : INetSerializable
	{
		public int NetId;
		public EGenericSubPacketType Type;
		public ISubPacket SubPacket;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			Type = (EGenericSubPacketType)reader.GetByte();
			SubPacket = reader.GetGenericSubPacket(Type, NetId);
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put((byte)Type);
			SubPacket?.Serialize(writer);
		}
	}
}
