using EFT.Airdrop;
using EFT.InventoryLogic;
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
		public Vector3 ToPosition;

		public EAirdropType AirdropType;
		public Item AirdropItem;
		public string ContainerId;
		public int NetId;

		public Vector3 Position;
		public Quaternion Rotation;

		public void Deserialize(NetDataReader reader)
		{
			ObjectType = (SynchronizableObjectType)reader.GetByte();
			ObjectId = reader.GetInt();
			IsStatic = reader.GetBool();
			switch (ObjectType)
			{
				case SynchronizableObjectType.Tripwire:
					{
						GrenadeTemplate = reader.GetString();
						GrenadeId = reader.GetString();
						ProfileId = reader.GetString();
						Position = reader.GetVector3();
						ToPosition = reader.GetVector3();
						Rotation = reader.GetQuaternion();
					}
					break;
				case SynchronizableObjectType.AirPlane:
					{
						Position = reader.GetVector3();
						Rotation = reader.GetQuaternion();
					}
					break;
				case SynchronizableObjectType.AirDrop:
					{
						AirdropType = (EAirdropType)reader.GetByte();
						AirdropItem = reader.GetAirdropItem();
						ContainerId = reader.GetString();
						NetId = reader.GetInt();
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
			writer.Put(IsStatic);
			switch (ObjectType)
			{
				case SynchronizableObjectType.Tripwire:
					{

						writer.Put(GrenadeTemplate);
						writer.Put(GrenadeId);
						writer.Put(ProfileId);
						writer.Put(Position);
						writer.Put(ToPosition);
						writer.Put(Rotation);
					}
					break;
				case SynchronizableObjectType.AirPlane:
					{

						writer.Put(Position);
						writer.Put(Rotation);
					}
					break;
				case SynchronizableObjectType.AirDrop:
					{
						writer.Put((byte)AirdropType);
						writer.PutItem(AirdropItem);
						writer.Put(ContainerId);
						writer.Put(NetId);
					}
					break;
				default:
					break;
			}
		}
	}
}
