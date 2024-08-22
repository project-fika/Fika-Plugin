using EFT.SynchronizableObjects;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct SyncObjectPacket(int id) : INetSerializable
	{
		public SynchronizableObjectType ObjectType;
		public int ObjectId = id;
		public bool Disarmed;
		public bool Triggered;

		public void Deserialize(NetDataReader reader)
		{
			ObjectType = (SynchronizableObjectType)reader.GetByte();
			ObjectId = reader.GetInt();
			switch (ObjectType)
			{
				case SynchronizableObjectType.Tripwire:
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
			writer.Put(ObjectId);
			switch (ObjectType)
			{
				case SynchronizableObjectType.Tripwire:
					{
						writer.Put(Disarmed);
						writer.Put(Triggered);
					}
					break;
				default:
					break;
			}
		}
	}
}
