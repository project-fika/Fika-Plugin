using EFT;
using EFT.Ballistics;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct DamagePacket
{
    public DamagePacket(int netId, DamageInfo damageInfo, EBodyPart bodyPartType,
        EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider = default, MaterialType materialType = default, float absorbed = default)
    {
        NetId = netId;
        Damage = damageInfo.Damage;
        Absorbed = absorbed;
        PenetrationPower = damageInfo.PenetrationPower;
        ArmorDamage = damageInfo.ArmorDamage;

        Direction = damageInfo.Direction;
        Point = damageInfo.HitPoint;
        HitNormal = damageInfo.HitNormal;

        DamageType = damageInfo.DamageType;
        BodyPartType = bodyPartType;
        ColliderType = colliderType;
        ArmorPlateCollider = armorPlateCollider;
        Material = materialType;

        BlockedBy = damageInfo.BlockedBy;
        DeflectedBy = damageInfo.DeflectedBy;
        if (damageInfo.Player != null)
        {
            ProfileId = damageInfo.Player.iPlayer.ProfileId;
        }
        if (damageInfo.Weapon != null)
        {
            WeaponId = damageInfo.Weapon.Id;
        }
        if (!string.IsNullOrWhiteSpace(damageInfo.SourceId))
        {
            SourceId = damageInfo.SourceId;
        }
    }

    public DamagePacket(NetDataReader reader)
    {
        NetId = reader.GetInt();

        Damage = reader.GetPackedFloat(0f, 1000f);
        Absorbed = reader.GetPackedFloat(0f, 1000f);
        PenetrationPower = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
        ArmorDamage = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);

        Direction = reader.GetUnmanaged<Vector3>();
        Point = reader.GetUnmanaged<Vector3>();
        HitNormal = reader.GetUnmanaged<Vector3>();

        DamageType = reader.GetEnum<EDamageType>();
        BodyPartType = reader.GetEnum<EBodyPart>();
        ColliderType = reader.GetEnum<EBodyPartColliderType>();
        ArmorPlateCollider = reader.GetEnum<EArmorPlateCollider>();
        Material = reader.GetEnum<MaterialType>();

        if (reader.GetBool())
        {
            BlockedBy = reader.GetMongoID();
        }
        if (reader.GetBool())
        {
            DeflectedBy = reader.GetMongoID();
        }
        if (reader.GetBool())
        {
            ProfileId = reader.GetMongoID();
        }
        if (reader.GetBool())
        {
            WeaponId = reader.GetMongoID();
        }
        if (reader.GetBool())
        {
            SourceId = reader.GetMongoID();
        }
    }

    public readonly int NetId;
    public readonly float Damage;
    public readonly float Absorbed;
    public readonly float PenetrationPower;
    public readonly float ArmorDamage;

    public readonly Vector3 Direction;
    public readonly Vector3 Point;
    public readonly Vector3 HitNormal;

    public readonly EDamageType DamageType;
    public readonly EBodyPart BodyPartType;
    public readonly EBodyPartColliderType ColliderType;
    public readonly EArmorPlateCollider ArmorPlateCollider;
    public readonly MaterialType Material;

    public readonly MongoID? BlockedBy;
    public readonly MongoID? DeflectedBy;
    public readonly MongoID? ProfileId;
    public readonly MongoID? WeaponId;
    public readonly MongoID? SourceId;

    public readonly void Execute(FikaPlayer player = null)
    {
        if (player.IsAI || player.IsYourPlayer)
        {
            player.HandleDamagePacket(this);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);

        writer.PutPackedFloat(Damage, 0f, 1000f);
        writer.PutPackedFloat(Absorbed, 0f, 1000f);
        writer.PutPackedFloat(PenetrationPower, 0f, 200f, EFloatCompression.High);
        writer.PutPackedFloat(ArmorDamage, 0f, 200f, EFloatCompression.High);

        writer.PutUnmanaged(Direction);
        writer.PutUnmanaged(Point);
        writer.PutUnmanaged(HitNormal);

        writer.PutEnum(DamageType);
        writer.PutEnum(BodyPartType);
        writer.PutEnum(ColliderType);
        writer.PutEnum(ArmorPlateCollider);
        writer.PutEnum(Material);

        writer.Put(BlockedBy.HasValue);
        if (BlockedBy.HasValue)
        {
            writer.PutMongoID(BlockedBy.Value);
        }
        writer.Put(DeflectedBy.HasValue);
        if (DeflectedBy.HasValue)
        {
            writer.PutMongoID(DeflectedBy.Value);
        }
        writer.Put(ProfileId.HasValue);
        if (ProfileId.HasValue)
        {
            writer.PutMongoID(ProfileId.Value);
        }
        writer.Put(WeaponId.HasValue);
        if (WeaponId.HasValue)
        {
            writer.PutMongoID(WeaponId.Value);
        }
        writer.Put(SourceId.HasValue);
        if (SourceId.HasValue)
        {
            writer.PutMongoID(SourceId.Value);
        }
    }
}
