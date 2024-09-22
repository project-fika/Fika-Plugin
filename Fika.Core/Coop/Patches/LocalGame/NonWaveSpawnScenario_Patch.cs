using EFT;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	internal class NonWaveSpawnScenario_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => typeof(NonWavesSpawnScenario).GetMethod(nameof(NonWavesSpawnScenario.Run));

		[PatchPrefix]
		public static bool PatchPrefix(NonWavesSpawnScenario __instance)
		{
			bool result = FikaBackendUtils.IsServer;
			typeof(NonWavesSpawnScenario).GetProperty(nameof(NonWavesSpawnScenario.Enabled)).SetValue(__instance, result);
			return result;
		}
	}
}
