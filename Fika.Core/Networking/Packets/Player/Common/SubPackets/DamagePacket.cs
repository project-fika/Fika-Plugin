using EFT;
using EFT.Ballistics;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class DamagePacket : IPoolSubPacket
{
    private DamagePacket() { }

    public static DamagePacket CreateInstance()
    {
        return new();
    }

    public int NetId;
    public float Damage;
    public float Absorbed;
    public float PenetrationPower;
    public float ArmorDamage;

    public Vector3 Direction;
    public Vector3 Point;
    public Vector3 HitNormal;

    public EDamageType DamageType;
    public EBodyPart BodyPartType;
    public EBodyPartColliderType ColliderType;
    public EArmorPlateCollider ArmorPlateCollider;
    public MaterialType Material;

    public MongoID? BlockedBy;
    public MongoID? DeflectedBy;
    public MongoID? ProfileId;
    public MongoID? WeaponId;
    public MongoID? SourceId;

    public static DamagePacket FromValue(int netId, DamageInfoStruct damageInfo, EBodyPart bodyPartType,
        EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider = default, MaterialType materialType = default, float absorbed = default)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<DamagePacket>(ECommonSubPacketType.Damage);

        packet.NetId = netId;
        packet.Damage = damageInfo.Damage;
        packet.Absorbed = absorbed;
        packet.PenetrationPower = damageInfo.PenetrationPower;
        packet.ArmorDamage = damageInfo.ArmorDamage;

        packet.Direction = damageInfo.Direction;
        packet.Point = damageInfo.HitPoint;
        packet.HitNormal = damageInfo.HitNormal;

        packet.DamageType = damageInfo.DamageType;
        packet.BodyPartType = bodyPartType;
        packet.ColliderType = colliderType;
        packet.ArmorPlateCollider = armorPlateCollider;
        packet.Material = materialType;

        packet.BlockedBy = damageInfo.BlockedBy;
        packet.DeflectedBy = damageInfo.DeflectedBy;
        if (damageInfo.Player != null)
        {
            packet.ProfileId = damageInfo.Player.iPlayer.ProfileId;
        }
        if (damageInfo.Weapon != null)
        {
            packet.WeaponId = damageInfo.Weapon.Id;
        }
        if (!string.IsNullOrEmpty(damageInfo.SourceId))
        {
            packet.SourceId = damageInfo.SourceId;
        }

        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (player.IsAI || player.IsYourPlayer)
        {
            player.HandleDamagePacket(this);
        }
    }

    public void Deserialize(NetDataReader reader)
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

    public void Dispose()
    {
        NetId = default;
        Damage = default;
        Absorbed = default;
        PenetrationPower = default;
        ArmorDamage = default;

        Direction = default;
        Point = default;
        HitNormal = default;

        DamageType = default;
        BodyPartType = default;
        ColliderType = default;
        ArmorPlateCollider = default;
        Material = default;

        BlockedBy = null;
        DeflectedBy = null;
        ProfileId = null;
        WeaponId = null;
        SourceId = null;
    }
}
