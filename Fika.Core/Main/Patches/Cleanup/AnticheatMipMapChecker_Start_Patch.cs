using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cleanup;

internal class AnticheatMipMapChecker_Start_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(AnticheatMipMapChecker),
            nameof(AnticheatMipMapChecker.Start));
    }

    [PatchPrefix]
    public static bool Prefix(AnticheatMipMapChecker __instance)
    {
        GameObject.Destroy(__instance.gameObject);
        return false;
    }
}
