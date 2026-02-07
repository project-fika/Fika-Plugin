using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Vehicle;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

public class BTRSide_Patches
{
    public static List<ValueTuple<BTRSide, Player, int>> Passengers = [];

    public class BTRSide_AddPassenger_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BTRSide), nameof(BTRSide.AddPassenger), [typeof(Player), typeof(int)]);
        }

        [PatchPrefix]
        public static bool Prefix(BTRSide __instance, Player player, int placeId, Transform ____startPoint)
        {
            if (player.IsYourPlayer)
            {
                return true;
            }

            player.Transform.Original.parent = ____startPoint;
            __instance.method_8(player);
            Passengers.Add(new ValueTuple<BTRSide, Player, int>(__instance, player, placeId));
            __instance.method_9();
            return false;
        }
    }

    public class BTRSide_RemovePassenger_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BTRSide), nameof(BTRSide.RemovePassenger), [typeof(Player)]);
        }

        [PatchPrefix]
        public static bool Prefix(BTRSide __instance, Player player)
        {
            if (player.IsYourPlayer)
            {
                return true;
            }

            for (var i = 0; i < Passengers.Count; i++)
            {
                var tuple = Passengers[i];
                if (tuple.Item2 == player)
                {
                    Passengers.Remove(tuple);
                }
            }

            player.Transform.Original.parent = null;
            return false;
        }
    }

    public class BTRSide_method_9_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BTRSide).GetMethod(nameof(BTRSide.method_9));
        }

        [PatchPrefix]
        public static void Prefix(BTRSide __instance, Transform ____startPoint)
        {
            for (var i = 0; i < Passengers.Count; i++)
            {
                var tuple = Passengers[i];
                if (tuple.Item1 == __instance)
                {
                    switch (tuple.Item2.BtrState)
                    {
                        case EPlayerBtrState.GoIn:
                        case EPlayerBtrState.GoOut:
                            {
                                tuple.Item2.Teleport(____startPoint.position);
                                (var start, var target) = __instance.GoInPoints();
                                __instance.ApplyPlayerRotation(tuple.Item2.MovementContext, start, target);
                                break;
                            }
                        case EPlayerBtrState.Inside:
                            {
                                tuple.Item2.Teleport(__instance.method_11(tuple.Item3));
                                (var start, var target) = __instance.GoInPoints();
                                __instance.ApplyPlayerRotation(tuple.Item2.MovementContext, start, target);
                                var num = Mathf.Lerp(0f, 0.05f, Mathf.InverseLerp(0f, __instance.BtrView.MoveSpeed, __instance.BtrView.CurrentSpeed));
                                if (num > Mathf.Epsilon)
                                {
                                    tuple.Item2.ProceduralWeaponAnimation.ForceReact.HardShake(num);
                                }
                            }
                            break;
                        default:
                            return;
                    }
                }
            }
        }
    }
}
