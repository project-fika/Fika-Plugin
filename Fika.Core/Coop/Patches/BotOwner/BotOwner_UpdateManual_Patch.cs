using EFT;
using EFT.Game.Spawning;
using SPT.Reflection.Patching;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Patch used to stop the allocation of a <see cref="Stopwatch"/> every frame for all active AI
	/// </summary>
	public class BotOwner_UpdateManual_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BotOwner).GetMethod(nameof(BotOwner.UpdateManual));
		}

		[PatchPrefix]
		public static bool Prefix(BotOwner __instance, float ____nextGetGoalTime, ref float ____nextTimeCheckBorn)
		{
			if (__instance.BotState == EBotState.Active && __instance.GetPlayer.HealthController.IsAlive)
			{
				__instance.StandBy.Update();
				__instance.LookSensor.ManualUpdate();
				if (__instance.StandBy.StandByType != BotStandByType.paused)
				{
					if (____nextGetGoalTime < Time.time)
					{
						__instance.CalcGoal();
					}
					__instance.SuppressShoot.ManualUpdate();
					__instance.HeadData.ManualUpdate();
					__instance.ShootData.ManualUpdate();
					__instance.Tilt.ManualUpdate();
					__instance.NightVision.ManualUpdate();
					__instance.NearDoorData.Update();
					__instance.DogFight.ManualUpdate();
					__instance.FriendChecker.ManualUpdate();
					__instance.RecoilData.LosingRecoil();
					__instance.Mover.ManualUpdate();
					__instance.AimingData.PermanentUpdate();
					__instance.Medecine.ManualUpdate();
					__instance.Boss.ManualUpdate();
					__instance.BotTalk.ManualUpdate();
					__instance.WeaponManager.ManualUpdate();
					__instance.BotRequestController.Update();
					__instance.GrenadeToPortal.ManualUpdate();
					__instance.Tactic.UpdateChangeTactics();
					__instance.Memory.ManualUpdate(Time.deltaTime);
					__instance.Settings.UpdateManual();
					__instance.BotRequestController.TryToFind();
					__instance.ArtilleryDangerPlace.ManualUpdate();
					if (__instance.GetPlayer.UpdateQueue == EUpdateQueue.Update)
					{
						__instance.Mover.ManualFixedUpdate();
						__instance.Steering.ManualFixedUpdate();
					}
					__instance.UnityEditorRunChecker.ManualLateUpdate();
				}
				return false;
			}
			if (__instance.BotState == EBotState.PreActive && __instance.WeaponManager.IsReady)
			{
				if (NavMesh.SamplePosition(__instance.GetPlayer.Position, out _, 0.6f, -1))
				{
					__instance.method_10();
					return false;
				}
				if (____nextTimeCheckBorn < Time.time)
				{
					____nextTimeCheckBorn = Time.time + 1f;
					__instance.Transform.position = __instance.BotsGroup.BotZone.SpawnPoints.RandomElement<ISpawnPoint>().Position + Vector3.up * 0.5f;
					__instance.method_10();
				}
			}
			return false;
		}
	}
}
