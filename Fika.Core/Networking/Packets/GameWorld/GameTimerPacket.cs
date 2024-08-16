// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct GameTimerPacket(bool isRequest, long tick = 0, long startTime = 0) : INetSerializable
	{
		public bool IsRequest = isRequest;
		public long Tick = tick;
		public long StartTime = startTime;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			Tick = reader.GetLong();
			StartTime = reader.GetLong();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			writer.Put(Tick);
			writer.Put(StartTime);
		}
	}
}
