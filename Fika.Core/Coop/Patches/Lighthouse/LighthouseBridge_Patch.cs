using Comfort.Common;
using EFT;
using Fika.Core.Coop.Lighthouse;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Lighthouse
{
	/// <summary>
	/// Based on <see href="https://dev.sp-tarkov.com/SPT/Modules/src/branch/master/project/SPT.SinglePlayer/Patches/Progression/LighthouseBridgePatch.cs"/>
	/// </summary>
	public class LighthouseBridge_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
		}

		[PatchPostfix]
		private static void PatchPostfix()
		{
			GameWorld gameWorld = Singleton<GameWorld>.Instance;

			if (gameWorld == null)
			{
				return;
			}

			if (gameWorld.MainPlayer.Location.ToLower() != "lighthouse" || gameWorld.MainPlayer.Side == EPlayerSide.Savage)
			{
				return;
			}

			gameWorld.GetOrAddComponent<FikaLighthouseProgressionClass>();
		}
	}
}
