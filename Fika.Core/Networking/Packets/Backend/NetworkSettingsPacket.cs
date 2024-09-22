using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct NetworkSettingsPacket(int sendRate) : INetSerializable
	{
		public int SendRate = sendRate;

		public void Deserialize(NetDataReader reader)
		{
			SendRate = reader.GetInt();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(SendRate);
		}
	}
}
