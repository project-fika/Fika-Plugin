using EFT.Airdrop;
using EFT.SynchronizableObjects;
using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct SyncObjectPacket(int id) : INetSerializable
	{
		public SynchronizableObjectType ObjectType;
		public int ObjectId = id;
		public AirplaneDataPacketStruct Data;

		public void Deserialize(NetDataReader reader)
		{
			ObjectType = (SynchronizableObjectType)reader.GetByte();
			ObjectId = reader.GetInt();
			switch (ObjectType)
			{
				case SynchronizableObjectType.Tripwire:
					{
						Data = new()
						{
							ObjectId = ObjectId,
							ObjectType = ObjectType,
							PacketData = new()
							{
								TripwireDataPacket = new()
								{
									State = (ETripwireState)reader.GetByte()
								}
							},
							Position = reader.GetVector3(),
							Rotation = reader.GetVector3(),
							IsActive = reader.GetBool()
						};
					}
					break;
				case SynchronizableObjectType.AirPlane:
					{
						Data = new()
						{
							ObjectId = ObjectId,
							ObjectType = ObjectType,
							Position = reader.GetVector3(),
							Rotation = reader.GetVector3(),
							PacketData = new()
							{
								AirplaneDataPacket = new()
								{
									AirplanePercent = reader.GetInt()
								}
							},
							Outdated = reader.GetBool(),
							IsStatic = reader.GetBool(),
							Develop = false
						};
					}
					break;
				case SynchronizableObjectType.AirDrop:
					{
						Data = new()
						{
							ObjectId = ObjectId,
							ObjectType = ObjectType,
							Position = reader.GetVector3(),
							Rotation = reader.GetVector3(),
							Outdated = reader.GetBool(),
							IsStatic = reader.GetBool(),
							PacketData = new()
							{
								AirdropDataPacket = new()
								{
									AirdropType = (EAirdropType)reader.GetByte(),
									FallingStage = (EAirdropFallingStage)reader.GetByte(),
									SignalFire = reader.GetBool(),
									UniqueId = reader.GetInt()
								}
							}
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
						writer.Put((byte)Data.PacketData.TripwireDataPacket.State);
						writer.Put(Data.Position);
						writer.Put(Data.Rotation);
						writer.Put(Data.IsActive);
					}
					break;
				case SynchronizableObjectType.AirPlane:
					{
						writer.Put(Data.Position);
						writer.Put(Data.Rotation);
						writer.Put(Data.PacketData.AirplaneDataPacket.AirplanePercent);
						writer.Put(Data.Outdated);
						writer.Put(Data.IsStatic);
					}
					break;
				case SynchronizableObjectType.AirDrop:
					{
						writer.Put(Data.Position);
						writer.Put(Data.Rotation);
						writer.Put(Data.Outdated);
						writer.Put(Data.IsStatic);
						writer.Put((byte)Data.PacketData.AirdropDataPacket.AirdropType);
						writer.Put((byte)Data.PacketData.AirdropDataPacket.FallingStage);
						writer.Put(Data.PacketData.AirdropDataPacket.SignalFire);
						writer.Put(Data.PacketData.AirdropDataPacket.UniqueId);
					}
					break;
				default:
					break;
			}
		}
	}
}
