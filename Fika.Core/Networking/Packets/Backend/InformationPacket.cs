// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct InformationPacket(bool isRequest) : INetSerializable
	{
		public bool IsRequest = isRequest;
		public int NumberOfPlayers = 0;
		public int ReadyPlayers = 0;
		public bool HostReady = false;

		public void Deserialize(NetDataReader reader)
		{
			IsRequest = reader.GetBool();
			NumberOfPlayers = reader.GetInt();
			ReadyPlayers = reader.GetInt();
			HostReady = reader.GetBool();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(IsRequest);
			writer.Put(NumberOfPlayers);
			writer.Put(ReadyPlayers);
			writer.Put(HostReady);
		}
	}
}
