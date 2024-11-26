using EFT;
using EFT.Vehicle;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
	class BTRSide_Patches
	{
		public static List<ValueTuple<BTRSide, Player, int>> Passengers;

		public static void Enable()
		{
			new BTRSide_AddPassenger_Patch().Enable();
			new BTRSide_RemovePassenger_Patch().Enable();
			new BTRSide_method_9_Patch().Enable();
			Passengers = [];
		}

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

				for (int i = 0; i < Passengers.Count; i++)
				{
					(BTRSide, Player, int) tuple = Passengers[i];
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
				for (int i = 0; i < Passengers.Count; i++)
				{
					(BTRSide, Player, int) tuple = Passengers[i];
					if (tuple.Item1 == __instance)
					{
						switch (tuple.Item2.BtrState)
						{
							case EPlayerBtrState.GoIn:
							case EPlayerBtrState.GoOut:
								{
									tuple.Item2.Teleport(____startPoint.position);
									(Vector3 start, Vector3 target) = __instance.GoInPoints();
									__instance.ApplyPlayerRotation(tuple.Item2.MovementContext, start, target);
									break;
								}
							case EPlayerBtrState.Inside:
								{
									tuple.Item2.Teleport(__instance.method_11(tuple.Item3));
									(Vector3 start, Vector3 target) = __instance.GoInPoints();
									__instance.ApplyPlayerRotation(tuple.Item2.MovementContext, start, target);
									float num = Mathf.Lerp(0f, 0.05f, Mathf.InverseLerp(0f, __instance.BtrView.MoveSpeed, __instance.BtrView.CurrentSpeed));
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
}
