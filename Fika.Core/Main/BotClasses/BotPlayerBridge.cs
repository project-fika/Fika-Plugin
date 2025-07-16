using Comfort.Common;
using EFT;
using EFT.Ballistics;
using Fika.Core.Main.Players;
using UnityEngine;

namespace Fika.Core.Main.ObservedClasses
{
    public class BotPlayerBridge(CoopBot bot) : BodyPartCollider.IPlayerBridge
    {
        private readonly CoopBot _bot = bot;

        public IPlayer iPlayer
        {
            get
            {
                return _bot;
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
                return _bot.UsedSimplifiedSkeleton;
            }
        }

        public void ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType bodyPartCollider, float absorbed)
        {
            _bot.ApplyDamageInfo(damageInfo, bodyPartType, bodyPartCollider, absorbed);
        }

        public ShotInfoClass ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPart, EBodyPartColliderType bodyPartCollider, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            if (damageInfo.DamageType is EDamageType.Explosion or EDamageType.Landmine or EDamageType.Sniper)
            {
                return _bot.ApplyShot(damageInfo, bodyPart, bodyPartCollider, armorPlateCollider, shotId);
            }

            if (damageInfo.Player != null && (damageInfo.Player.iPlayer.IsYourPlayer || damageInfo.Player.IsAI))
            {
                return _bot.ApplyShot(damageInfo, bodyPart, bodyPartCollider, armorPlateCollider, shotId);
            }

            _bot.ShotReactions(damageInfo, bodyPart);
            return new()
            {
                PoV = EPointOfView.ThirdPerson,
                Penetrated = damageInfo.Penetrated,
                Material = MaterialType.Body
            };
        }

        public bool CheckArmorHitByDirection(BodyPartCollider bodypart, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
        {
            return _bot.CheckArmorHitByDirection(bodypart);
        }

        public bool IsShotDeflectedByHeavyArmor(EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, int shotSeed)
        {
            return _bot.IsShotDeflectedByHeavyArmor(colliderType, armorPlateCollider, shotSeed);
        }

        public bool SetShotStatus(BodyPartCollider bodypart, EftBulletClass shot, Vector3 hitpoint, Vector3 shotNormal, Vector3 shotDirection)
        {
            return _bot.SetShotStatus(bodypart, shot, hitpoint, shotNormal, shotDirection);
        }

        public bool TryGetArmorResistData(BodyPartCollider bodyPart, float penetrationPower, out ArmorResistanceStruct armorResistanceData)
        {
            return _bot.TryGetArmorResistData(bodyPart, penetrationPower, out armorResistanceData);
        }
    }
}
