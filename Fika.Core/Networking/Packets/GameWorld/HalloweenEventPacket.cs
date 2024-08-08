using EFT;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
	public struct HalloweenEventPacket(EHalloweenPacketType packetType) : INetSerializable
	{
		public EHalloweenPacketType PacketType = packetType;
		public Vector3 SummonPosition;
		public EEventState EventState;
		public string Exit;

		public void Deserialize(NetDataReader reader)
		{
			PacketType = (EHalloweenPacketType)reader.GetInt();
			switch (PacketType)
			{
				case EHalloweenPacketType.Summon:
					SummonPosition = reader.GetVector3();
					break;
				case EHalloweenPacketType.Sync:
					EventState = (EEventState)reader.GetInt();
					break;
				case EHalloweenPacketType.Exit:
					Exit = reader.GetString();
					break;
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put((int)PacketType);
			switch (PacketType)
			{
				case EHalloweenPacketType.Summon:
					writer.Put(SummonPosition);
					break;
				case EHalloweenPacketType.Sync:
					writer.Put((int)EventState);
					break;
				case EHalloweenPacketType.Exit:
					writer.Put(Exit);
					break;
			}
		}
	}

	public enum EHalloweenPacketType
	{
		Summon,
		Sync,
		Exit
	}
}
