using EFT;
using EFT.Ballistics;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
    public struct DamagePacket : INetSerializable
    {
        public ushort NetId;
        public float Damage;
        public float Absorbed;
        public float PenetrationPower;
        public float ArmorDamage;

        public Vector3 Direction;
        public Vector3 Point;
        public Vector3 HitNormal;

        public int FragmentIndex;

        public EDamageType DamageType;
        public EBodyPart BodyPartType;
        public EBodyPartColliderType ColliderType;
        public EArmorPlateCollider ArmorPlateCollider;
        public MaterialType Material;

        public MongoID? BlockedBy;
        public MongoID? DeflectedBy;
        public MongoID? SourceId;
        public MongoID? ProfileId;
        public MongoID? WeaponId;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetUShort();

            Damage = reader.GetFloat();
            Absorbed = reader.GetFloat();
            PenetrationPower = reader.GetFloat();
            ArmorDamage = reader.GetFloat();

            Direction = reader.GetVector3();
            Point = reader.GetVector3();
            HitNormal = reader.GetVector3();

            FragmentIndex = reader.GetInt();

            DamageType = (EDamageType)reader.GetInt();
            BodyPartType = (EBodyPart)reader.GetByte();
            ColliderType = (EBodyPartColliderType)reader.GetByte();
            ArmorPlateCollider = (EArmorPlateCollider)reader.GetByte();
            Material = (MaterialType)reader.GetByte();

            BlockedBy = reader.GetMongoID();
            DeflectedBy = reader.GetMongoID();
            SourceId = reader.GetMongoID();
            ProfileId = reader.GetMongoID();
            WeaponId = reader.GetMongoID();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);

            writer.Put(Damage);
            writer.Put(Absorbed);
            writer.Put(PenetrationPower);
            writer.Put(ArmorDamage);

            writer.PutVector3(Direction);
            writer.PutVector3(Point);
            writer.PutVector3(HitNormal);

            writer.Put(FragmentIndex);

            writer.Put((int)DamageType);
            writer.Put((byte)BodyPartType);
            writer.Put((byte)ColliderType);
            writer.Put((byte)ArmorPlateCollider);
            writer.Put((byte)Material);

            writer.PutMongoID(BlockedBy);
            writer.PutMongoID(DeflectedBy);
            writer.PutMongoID(SourceId);
            writer.PutMongoID(ProfileId);
            writer.PutMongoID(WeaponId);
        }
    }
}
