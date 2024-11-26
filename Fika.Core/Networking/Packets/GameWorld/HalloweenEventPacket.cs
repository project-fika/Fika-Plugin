using EFT;
using EFT.GlobalEvents;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct HalloweenEventPacket : INetSerializable
	{
		public EHalloweenPacketType PacketType;
		public BaseSyncEvent SyncEvent;

		public void Deserialize(NetDataReader reader)
		{
			PacketType = (EHalloweenPacketType)reader.GetInt();
			switch (PacketType)
			{
				case EHalloweenPacketType.Summon:
					SyncEvent = new HalloweenSummonStartedEvent()
					{
						PointPosition = reader.GetVector3()
					};
					break;
				case EHalloweenPacketType.Sync:
					SyncEvent = new HalloweenSyncStateEvent()
					{
						EventState = (EEventState)reader.GetByte()
					};
					break;
				case EHalloweenPacketType.Exit:
					SyncEvent = new HalloweenSyncExitsEvent()
					{
						ExitName = reader.GetString()
					};
					break;
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put((int)PacketType);
			switch (PacketType)
			{
				case EHalloweenPacketType.Summon:
					if (SyncEvent is HalloweenSummonStartedEvent startedEvent)
					{
						writer.Put(startedEvent.PointPosition);
					}
					break;
				case EHalloweenPacketType.Sync:
					if (SyncEvent is HalloweenSyncStateEvent stateEvent)
					{
						writer.Put((byte)stateEvent.EventState);
					}
					break;
				case EHalloweenPacketType.Exit:
					if (SyncEvent is HalloweenSyncExitsEvent exitsEvent)
					{
						writer.Put(exitsEvent.ExitName);
					}
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
