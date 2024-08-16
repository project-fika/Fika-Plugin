using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches.Overrides
{
	internal class ScavProfileLoad_Override : ModulePatch
	{
		protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_50));

		[PatchPrefix]
		private static void PatchPrefix(ref string profileId, Profile savageProfile, RaidSettings ____raidSettings)
		{
			if (!____raidSettings.IsPmc)
			{
				profileId = savageProfile.Id;
			}
		}
	}
}