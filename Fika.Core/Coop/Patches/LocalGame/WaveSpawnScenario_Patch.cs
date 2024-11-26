using EFT;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	internal class WaveSpawnScenario_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => typeof(WavesSpawnScenario).GetMethod(nameof(WavesSpawnScenario.Run));

		[PatchPrefix]
		public static bool PatchPrefix(WavesSpawnScenario __instance)
		{
			bool result = FikaBackendUtils.IsServer;
			typeof(WavesSpawnScenario).GetProperty(nameof(WavesSpawnScenario.Enabled)).SetValue(__instance, result);
			return result;
		}
	}
}
