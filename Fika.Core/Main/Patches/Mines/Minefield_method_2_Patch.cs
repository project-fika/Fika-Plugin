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
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Mines;

/// <summary>
/// This patch prevents a null exception when an <see cref="ObservedPlayer"/> is hit by a mine explosion
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
        if (player is ObservedPlayer)
        {
            if (FikaBackendUtils.IsServer)
            {
                var position = player.Position;
                foreach (var player2 in ___TargetedPlayers.Concat(___NotTargetedPlayers).ToList())
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

        var fikaPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(player.ProfileId);
        if (isCollateral && distance > collateralDamageRange)
        {
            return;
        }

        if (fikaPlayer != null)
        {
            float num2 = 1f - distance / collateralDamageRange;
            var enumerable = isCollateral ? player.PlayerBones.BodyPartColliders.Where(minefield.method_4)
                : player.PlayerBones.BodyPartColliders.Where(minefield.method_5);

            enumerable = enumerable.DistinctBy(FikaGlobals.GetBodyPartFromCollider).ToArray();
            enumerable = enumerable.Randomize();

            int num3 = isCollateral || first ? Random.Range(2, enumerable.Count()) : int.MaxValue;
            float num4 = isCollateral || first ? firstExplosionDamage : secondExplosionDamage;
            int num5 = 0;

            foreach (var bodyPartCollider in enumerable)
            {
                fikaPlayer.CommonPacket.Type = ECommonSubPacketType.Damage;
                fikaPlayer.CommonPacket.SubPacket = DamagePacket.FromValue(fikaPlayer.NetId, new()
                {
                    DamageType = EDamageType.Landmine,
                    Damage = num4 * num2,
                    ArmorDamage = 0.5f,
                    PenetrationPower = 30f,
                    Direction = default,
                    HitNormal = default
                }, bodyPartCollider.BodyPartType, bodyPartCollider.BodyPartColliderType);
                Singleton<IFikaNetworkManager>.Instance.SendNetReusable(ref fikaPlayer.CommonPacket, DeliveryMethod.ReliableOrdered, true);
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
