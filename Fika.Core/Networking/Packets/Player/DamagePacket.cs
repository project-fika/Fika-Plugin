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
		public Vector3 Direction = Vector3.zero;
		public Vector3 Point = Vector3.zero;
		public Vector3 HitNormal = Vector3.zero;
		public float PenetrationPower = 0f;
		public string BlockedBy;
		public string DeflectedBy;
		public string SourceId;
		public string AmmoId;
		public int FragmentIndex;
		public float ArmorDamage = 0f;
		public string ProfileId;
		public MaterialType Material = 0;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			DamageType = (EDamageType)reader.GetInt();
			Damage = reader.GetFloat();
			BodyPartType = (EBodyPart)reader.GetInt();
			ColliderType = (EBodyPartColliderType)reader.GetInt();
			ArmorPlateCollider = (EArmorPlateCollider)reader.GetInt();
			Absorbed = reader.GetFloat();
			Direction = reader.GetVector3();
			Point = reader.GetVector3();
			HitNormal = reader.GetVector3();
			PenetrationPower = reader.GetFloat();
			BlockedBy = reader.GetString();
			DeflectedBy = reader.GetString();
			SourceId = reader.GetString();
			AmmoId = reader.GetString();
			FragmentIndex = reader.GetInt();
			ArmorDamage = reader.GetFloat();
			ProfileId = reader.GetString();
			Material = (MaterialType)reader.GetInt();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put((int)DamageType);
			writer.Put(Damage);
			writer.Put((int)BodyPartType);
			writer.Put((int)ColliderType);
			writer.Put((int)ArmorPlateCollider);
			writer.Put(Absorbed);
			writer.Put(Direction);
			writer.Put(Point);
			writer.Put(HitNormal);
			writer.Put(PenetrationPower);
			writer.Put(BlockedBy);
			writer.Put(DeflectedBy);
			writer.Put(SourceId);
			writer.Put(AmmoId);
			writer.Put(FragmentIndex);
			writer.Put(ArmorDamage);
			writer.Put(ProfileId);
			writer.Put((int)Material);
		}
	}
}
