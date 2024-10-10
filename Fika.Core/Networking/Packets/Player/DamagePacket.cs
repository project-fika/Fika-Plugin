using EFT;
using EFT.Ballistics;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
	public struct DamagePacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public EDamageType DamageType;
		public float Damage;
		public EBodyPart BodyPartType;
		public EBodyPartColliderType ColliderType;
		public EArmorPlateCollider ArmorPlateCollider;
		public float Absorbed;
		public Vector3 Direction;
		public Vector3 Point;
		public Vector3 HitNormal;
		public float PenetrationPower;
		public MongoID? BlockedBy;
		public MongoID? DeflectedBy;
		public string SourceId;
		public int FragmentIndex;
		public float ArmorDamage;
		public string ProfileId;
		public MaterialType Material;
		public string WeaponId;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			DamageType = (EDamageType)reader.GetInt();
			Damage = reader.GetFloat();
			BodyPartType = (EBodyPart)reader.GetByte();
			ColliderType = (EBodyPartColliderType)reader.GetByte();
			ArmorPlateCollider = (EArmorPlateCollider)reader.GetByte();
			Direction = reader.GetVector3();
			Point = reader.GetVector3();
			HitNormal = reader.GetVector3();
			PenetrationPower = reader.GetFloat();
			BlockedBy = reader.GetMongoID();
			DeflectedBy = reader.GetMongoID();
			SourceId = reader.GetString();
			FragmentIndex = reader.GetInt();
			ArmorDamage = reader.GetFloat();
			ProfileId = reader.GetString();
			Material = (MaterialType)reader.GetByte();
			WeaponId = reader.GetString();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put((int)DamageType);
			writer.Put(Damage);
			writer.Put((byte)BodyPartType);
			writer.Put((byte)ColliderType);
			writer.Put((byte)ArmorPlateCollider);
			writer.Put(Direction);
			writer.Put(Point);
			writer.Put(HitNormal);
			writer.Put(PenetrationPower);
			writer.PutMongoID(BlockedBy);
			writer.PutMongoID(DeflectedBy);
			writer.Put(SourceId);
			writer.Put(FragmentIndex);
			writer.Put(ArmorDamage);
			writer.Put(ProfileId);
			writer.Put((byte)Material);
			writer.Put(WeaponId);
		}
	}
}
