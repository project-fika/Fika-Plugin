using LiteNetLib.Utils;
using UnityEngine;
using static Fika.Core.Networking.Packets.SubPackets;

namespace Fika.Core.Networking
{
	public struct SendCharacterPacket(PlayerInfoPacket playerInfoPacket, bool isAlive, bool isAi, Vector3 position, int netId) : INetSerializable
	{
		public PlayerInfoPacket PlayerInfoPacket = playerInfoPacket;
		public bool IsAlive = isAlive;
		public bool IsAI = isAi;
		public Vector3 Position = position;
		public int NetId = netId;

		public void Deserialize(NetDataReader reader)
		{
			PlayerInfoPacket = reader.GetPlayerInfoPacket();
			IsAlive = reader.GetBool();
			IsAI = reader.GetBool();
			Position = reader.GetVector3();
			NetId = reader.GetInt();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.PutPlayerInfoPacket(PlayerInfoPacket);
			writer.Put(IsAlive);
			writer.Put(IsAI);
			writer.Put(Position);
			writer.Put(NetId);
		}
	}
}
