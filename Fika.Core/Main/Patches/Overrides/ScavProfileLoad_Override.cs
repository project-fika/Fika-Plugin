using System.Reflection;
using EFT;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Overrides;

internal class ScavProfileLoad_Override : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.OnGameEnd));
    }

    [PatchPrefix]
    private static void PatchPrefix(EFT.TarkovApplication __instance, ref string profileId, Profile savageProfile)
    {
        if (!__instance._raidSettings.IsPmc)
        {
            profileId = savageProfile.Id;
        }
    }
}