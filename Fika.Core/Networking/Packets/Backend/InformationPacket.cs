// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;
using System;

namespace Fika.Core.Networking
{
	public struct InformationPacket : INetSerializable
	{
		public bool RaidStarted;
		public bool RequestStart;
		public int ReadyPlayers;
		public int AmountOfPeers;
		public bool HostReady;
		public bool HostLoaded;
		public DateTime GameTime;
		public TimeSpan SessionTime;

		public void Deserialize(NetDataReader reader)
		{
			RaidStarted = reader.GetBool();
			RequestStart = reader.GetBool();
			ReadyPlayers = reader.GetInt();
			AmountOfPeers = reader.GetInt();
			HostReady = reader.GetBool();
			if (HostReady)
			{
				GameTime = reader.GetDateTime();
				SessionTime = TimeSpan.FromTicks(reader.GetLong());
			}
			HostLoaded = reader.GetBool();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(RaidStarted);
			writer.Put(RequestStart);
			writer.Put(ReadyPlayers);
			writer.Put(AmountOfPeers);
			writer.Put(HostReady);
			if (HostReady)
			{
				writer.Put(GameTime);
				writer.Put(SessionTime.Ticks);
			}
			writer.Put(HostLoaded);
		}
	}
}
