using EFT.SynchronizableObjects;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
	public struct SpawnSyncObjectPacket(int id) : INetSerializable
	{
		public SynchronizableObjectType ObjectType;
		public int ObjectId = id;
		public bool IsStatic;
		public string GrenadeTemplate;
		public string GrenadeId;
		public string ProfileId;
		public Vector3 Position;
		public Vector3 ToPosition;
		public Quaternion Rotation;

		public void Deserialize(NetDataReader reader)
		{
			ObjectType = (SynchronizableObjectType)reader.GetByte();
			ObjectId = reader.GetInt();
			switch (ObjectType)
			{
				case SynchronizableObjectType.Tripwire:
					{
						IsStatic = reader.GetBool();
						GrenadeTemplate = reader.GetString();
						GrenadeId = reader.GetString();
						ProfileId = reader.GetString();
						Position = reader.GetVector3();
						ToPosition = reader.GetVector3();
						Rotation = reader.GetQuaternion();
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
						writer.Put(IsStatic);
						writer.Put(GrenadeTemplate);
						writer.Put(GrenadeId);
						writer.Put(ProfileId);
						writer.Put(Position);
						writer.Put(ToPosition);
						writer.Put(Rotation);
					}
					break;
				default:
					break;
			}
		}
	}
}
