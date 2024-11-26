using EFT.BufferZone;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct BufferZonePacket(EBufferZoneData status) : INetSerializable
	{
		public EBufferZoneData Status = status;
		public bool Available;
		public string ProfileId;

		public void Deserialize(NetDataReader reader)
		{
			Status = (EBufferZoneData)reader.GetByte();
			switch (Status)
			{
				case EBufferZoneData.Availability:
				case EBufferZoneData.DisableByZryachiyDead:
				case EBufferZoneData.DisableByPlayerDead:
					{
						Available = reader.GetBool();
					}
					break;
				case EBufferZoneData.PlayerAccessStatus:
					{
						Available = reader.GetBool();
						ProfileId = reader.GetString();
					}
					break;
				case EBufferZoneData.PlayerInZoneStatusChange:
					{
						Available = reader.GetBool();
						ProfileId = reader.GetString();
					}
					break;
				default:
					break;
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put((byte)Status);
			switch (Status)
			{
				case EBufferZoneData.Availability:
				case EBufferZoneData.DisableByZryachiyDead:
				case EBufferZoneData.DisableByPlayerDead:
					{
						writer.Put(Available);
					}
					break;
				case EBufferZoneData.PlayerAccessStatus:
					{
						writer.Put(Available);
						writer.Put(ProfileId);
					}
					break;
				case EBufferZoneData.PlayerInZoneStatusChange:
					{
						writer.Put(Available);
						writer.Put(ProfileId);
					}
					break;
				default:
					break;
			}
		}
	}
}
