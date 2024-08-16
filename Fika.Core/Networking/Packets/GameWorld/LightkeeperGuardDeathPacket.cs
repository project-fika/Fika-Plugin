using EFT;
using LiteNetLib.Utils;

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
