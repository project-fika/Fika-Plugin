using EFT;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Overrides;

internal class ScavProfileLoad_Override : FikaPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_52));
    }

    [PatchPrefix]
    private static void PatchPrefix(ref string profileId, Profile savageProfile, RaidSettings ____raidSettings)
    {
        if (!____raidSettings.IsPmc)
        {
            profileId = savageProfile.Id;
        }
    }
}