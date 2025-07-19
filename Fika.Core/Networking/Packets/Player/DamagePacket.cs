using EFT;
using EFT.Ballistics;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking.Packets.Player
{
    public struct DamagePacket : INetSerializable
    {
        public int NetId;
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
        public MongoID? ProfileId;
        public MongoID? WeaponId;

        public string SourceId;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();

            Damage = reader.GetFloat();
            Absorbed = reader.GetFloat();
            PenetrationPower = reader.GetFloat();
            ArmorDamage = reader.GetFloat();

            Direction = reader.GetVector3();
            Point = reader.GetVector3();
            HitNormal = reader.GetVector3();

            FragmentIndex = reader.GetInt();

            DamageType = reader.GetEnum<EDamageType>();
            BodyPartType = reader.GetEnum<EBodyPart>();
            ColliderType = reader.GetEnum<EBodyPartColliderType>();
            ArmorPlateCollider = reader.GetEnum<EArmorPlateCollider>();
            Material = reader.GetEnum<MaterialType>();

            BlockedBy = reader.GetNullableMongoID();
            DeflectedBy = reader.GetNullableMongoID();
            ProfileId = reader.GetNullableMongoID();
            WeaponId = reader.GetNullableMongoID();

            SourceId = reader.GetString();
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

            writer.PutEnum(DamageType);
            writer.PutEnum(BodyPartType);
            writer.PutEnum(ColliderType);
            writer.PutEnum(ArmorPlateCollider);
            writer.PutEnum(Material);

            writer.PutNullableMongoID(BlockedBy);
            writer.PutNullableMongoID(DeflectedBy);
            writer.PutNullableMongoID(ProfileId);
            writer.PutNullableMongoID(WeaponId);

            writer.Put(SourceId);
        }
    }
}
