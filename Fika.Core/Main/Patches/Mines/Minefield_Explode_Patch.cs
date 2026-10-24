using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Mines;

/// <summary>
/// This patch prevents a null exception when an <see cref="ObservedPlayer"/> is hit by a mine explosion
/// </summary>
internal class Minefield_Explode_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Minefield).GetMethod(nameof(Minefield.Explode));
    }

    [PatchPrefix]
    public static bool Prefix(IPlayer player, bool first, Minefield __instance)
    {
        if (player is ObservedPlayer)
        {
            if (FikaBackendUtils.IsServer)
            {
                var position = player.Position;
                foreach (var player2 in __instance.TargetedPlayers.Concat(__instance.NotTargetedPlayers).ToList())
                {
                    DoReplicatedMineDamage(player2, Vector3.Distance(position, player2.Position), first,
                        player != player2, __instance._collateralContusionRange, __instance._collateralDamageRange,
                        __instance._firstExplosionDamage, __instance._secondExplosionDamage, __instance);
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

        var fikaPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(player.ProfileId);
        if (isCollateral && distance > collateralDamageRange)
        {
            return;
        }

        if (fikaPlayer != null)
        {
            var num2 = 1f - distance / collateralDamageRange;
            var enumerable = isCollateral ? player.PlayerBones.BodyPartColliders.Where(x => minefield._collateralDamageTarget.Contains(x.BodyPartType))
                : player.PlayerBones.BodyPartColliders.Where(x => minefield._explosionTarget.Contains(x.BodyPartType));

            enumerable = enumerable.DistinctBy(FikaGlobals.GetBodyPartFromCollider).ToArray();
            enumerable = enumerable.OrderBy(_ => Random.value).ToArray();

            var num3 = isCollateral || first ? Random.Range(2, enumerable.Count()) : int.MaxValue;
            var num4 = isCollateral || first ? firstExplosionDamage : secondExplosionDamage;
            var num5 = 0;

            foreach (var bodyPartCollider in enumerable)
            {
                var packet = new DamagePacket(fikaPlayer.NetId, new()
                {
                    DamageType = EDamageType.Landmine,
                    Damage = num4 * num2,
                    ArmorDamage = 0.5f,
                    PenetrationPower = 30f,
                    Direction = default,
                    HitNormal = default
                }, bodyPartCollider.BodyPartType, bodyPartCollider.BodyPartColliderType);
                FikaGlobals.NetworkManager.SendPlayerPacket(in packet, fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
                if (++num5 >= num3)
                {
                    break;
                }
            }
        }
        else
        {
            FikaGlobals.LogError($"DoReplicatedMineDamage: Could not find player with ProfileId: {player.ProfileId}, Nickname: {player.Profile.Nickname}!");
        }
    }
}
