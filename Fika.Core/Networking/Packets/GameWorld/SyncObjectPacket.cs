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

		public AirplaneDataPacketStruct Data;

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
				case SynchronizableObjectType.AirPlane:
					{
						Data = new()
						{
							ObjectId = ObjectId,
							ObjectType = ObjectType,
							Position = reader.GetVector3(),
							Rotation = reader.GetQuaternion().eulerAngles,
							PacketData = new()
							{
								AirplaneDataPacket = new()
								{
									AirplanePercent = reader.GetInt()
								}
							},
							Outdated = reader.GetBool(),
							IsStatic = reader.GetBool()
						};
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
					case SynchronizableObjectType.AirPlane:
					{
						writer.Put(Data.Position)
					}
					break;
				default:
					break;
			}
		}
	}
}
