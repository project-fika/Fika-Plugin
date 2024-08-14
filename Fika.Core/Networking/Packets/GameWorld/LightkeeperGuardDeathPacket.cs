using EFT;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Fika.Core.Networking.Packets.GameWorld
{
	public struct LightkeeperGuardDeathPacket : INetSerializable
	{
		public string ProfileId;
		public WildSpawnType WildType;

		public void Deserialize(NetDataReader reader)
		{
			ProfileId = reader.GetString();
			WildType = (WildSpawnType)reader.GetInt();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(ProfileId);
			writer.Put((int)WildType);
		}
	}
}
