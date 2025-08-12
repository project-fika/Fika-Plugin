using Comfort.Common;
using EFT;
using EFT.Ballistics;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses.PlayerBridge;

public class ObservedClientBridge(ObservedPlayer observedPlayer) : BodyPartCollider.IPlayerBridge
{
    private readonly ObservedPlayer _observedPlayer = observedPlayer;

    public IPlayer iPlayer
    {
        get
        {
            return _observedPlayer;
        }
    }

    public float WorldTime
    {
        get
        {
            return Singleton<AbstractGame>.Instance.LastServerTimeStamp;
        }
    }

    public bool UsingSimplifiedSkeleton
    {
        get
        {
            return _observedPlayer.UsedSimplifiedSkeleton;
        }
    }

    public void ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType bodyPartCollider, float absorbed)
    {
        if (damageInfo.DamageType.IsEnvironmental() || damageInfo.DamageType is EDamageType.Landmine or EDamageType.Artillery)
        {
            return;
        }

        if (damageInfo.Player != null && damageInfo.Player.iPlayer.IsYourPlayer)
        {
            _observedPlayer.ApplyDamageInfo(damageInfo, bodyPartType, bodyPartCollider, absorbed);
        }
    }

    public ShotInfoClass ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPart, EBodyPartColliderType bodyPartCollider, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
    {
        if (damageInfo.Player != null && damageInfo.Player.iPlayer.IsYourPlayer)
        {
            return _observedPlayer.ApplyClientShot(damageInfo, bodyPart, bodyPartCollider, armorPlateCollider, shotId);
        }

        _observedPlayer.ShotReactions(damageInfo, bodyPart);
        _observedPlayer.ApplyHitDebuff(damageInfo.Damage, 0f, bodyPart, damageInfo.DamageType);
        return new()
        {
            PoV = EPointOfView.ThirdPerson,
            Penetrated = damageInfo.Penetrated,
            Material = MaterialType.Body
        };
    }

    public bool CheckArmorHitByDirection(BodyPartCollider bodypart, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
    {
        return _observedPlayer.CheckArmorHitByDirection(bodypart);
    }

    public bool IsShotDeflectedByHeavyArmor(EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, int shotSeed)
    {
        return _observedPlayer.IsShotDeflectedByHeavyArmor(colliderType, armorPlateCollider, shotSeed);
    }

    public bool SetShotStatus(BodyPartCollider bodypart, EftBulletClass shot, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
    {
        return _observedPlayer.SetShotStatus(bodypart, shot, hitpoint, shotNormal, shotDirection);
    }

    public bool TryGetArmorResistData(BodyPartCollider bodyPart, float penetrationPower, out ArmorResistanceStruct armorResistanceData)
    {
        return _observedPlayer.TryGetArmorResistData(bodyPart, penetrationPower, out armorResistanceData);
    }
}
