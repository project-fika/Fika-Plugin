using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct SyncObjectPacket(int id) : INetSerializable
	{
		public SyncObjectType ObjectType;
		public int Id = id;
		public bool Disarmed;
		public bool Triggered;

		public void Deserialize(NetDataReader reader)
		{
			ObjectType = (SyncObjectType)reader.GetByte();
			Id = reader.GetInt();
			switch (ObjectType)
			{
				case SyncObjectType.Tripwire:
					{
						Disarmed = reader.GetBool();
						Triggered = reader.GetBool();
					}
					break;
				default:
					break;
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put((byte)ObjectType);
			writer.Put(Id);
			switch (ObjectType)
			{
				case SyncObjectType.Tripwire:
					{
						writer.Put(Disarmed);
						writer.Put(Triggered);
					}
					break;
				default:
					break;
			}
		}

		public enum SyncObjectType
		{
			Tripwire = 0
		}
	}
}
