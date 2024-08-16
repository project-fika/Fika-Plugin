using Fika.Core.Coop.Utils;
using HarmonyLib;
using SPT.Custom.Airdrops;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Airdrop
{
	public class AirdropBox_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(AirdropBox), "AddNavMeshObstacle");

		[PatchPrefix]
		public static bool PatchPrefix(AirdropBox __instance)
		{
			//Allow method to go through
			if (FikaBackendUtils.IsServer)
			{
				return true;
			}

			//Stop running method.
			return false;
		}
	}
}
