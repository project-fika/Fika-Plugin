using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
    /// <summary>
    /// This patch prevents a null exception when an <see cref="ObservedCoopPlayer"/> is hit by a mine explosion
    /// </summary>
    internal class Minefield_method_2_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Minefield).GetMethod(nameof(Minefield.method_2));
        }

        [PatchPrefix]
        public static bool Prefix(IPlayer player, bool first, List<IPlayer> ___TargetedPlayers,
            List<IPlayer> ___NotTargetedPlayers, float ____collateralContusionRange, float ____collateralDamageRange,
            float ____firstExplosionDamage, float ____secondExplosionDamage, Minefield __instance)
        {
            if (player is ObservedCoopPlayer)
            {
                if (MatchmakerAcceptPatches.IsServer)
                {
                    Vector3 position = player.Position;
                    foreach (IPlayer player2 in ___TargetedPlayers.Concat(___NotTargetedPlayers).ToList())
                    {
                        DoReplicatedMineDamage(player2, Vector3.Distance(position, player2.Position), first,
                            player != player2, ____collateralContusionRange, ____collateralDamageRange,
                            ____firstExplosionDamage, ____secondExplosionDamage, __instance);
                    }
                }
                return false;
            }
            return true;
        }

        private static void DoReplicatedMineDamage(IPlayer player, float distance, bool first, bool isCollateral,
            float collateralContusionRange, float collateralDamageRange, float firstExplosionDamage,
            float secondExplosionDamage, Minefield minefield)
        {
            if (isCollateral && distance > collateralContusionRange)
            {
                return;
            }

            CoopPlayer coopPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(player.ProfileId);
            if (isCollateral && distance > collateralDamageRange)
            {
                return;
            }

            if (coopPlayer != null)
            {
                float num2 = 1f - distance / collateralDamageRange;
                IEnumerable<BodyPartCollider> enumerable = isCollateral ? player.PlayerBones.BodyPartColliders.Where(new Func<BodyPartCollider, bool>(minefield.method_4))
                    : player.PlayerBones.BodyPartColliders.Where(new Func<BodyPartCollider, bool>(minefield.method_5));

                enumerable = enumerable.DistinctBy(new Func<BodyPartCollider, EBodyPart>(Minefield.Class2286.class2286_0.method_0)).ToArray();
                enumerable = enumerable.Randomize();

                int num3 = ((isCollateral || first) ? UnityEngine.Random.Range(2, enumerable.Count()) : int.MaxValue);
                float num4 = (isCollateral || first) ? firstExplosionDamage : secondExplosionDamage;
                int num5 = 0;

                foreach (BodyPartCollider bodyPartCollider in enumerable)
                {
                    coopPlayer.PacketSender.DamagePackets.Enqueue(new()
                    {
                        DamageInfo = new()
                        {
                            DamageType = EDamageType.Landmine,
                            Damage = num4 * num2,
                            ArmorDamage = 0.5f,
                            PenetrationPower = 30f,
                            Direction = Vector3.zero,
                            HitNormal = Vector3.zero,
                            ColliderType = bodyPartCollider.BodyPartColliderType,
                            BodyPartType = bodyPartCollider.BodyPartType
                        }
                    });
                    if (++num5 >= num3)
                    {
                        break;
                    }
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"DoReplicatedMineDamage: Could not find player with ProfileId: {player.ProfileId}, Nickname: {player.Profile.Nickname}!");
            }
        }
    }
}
