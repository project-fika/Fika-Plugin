using System.Reflection;
using Dissonance;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.VOIP;

public class DissonanceComms_Start_Patch : ModulePatch
{
    public static bool IsReady;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools
            .Method(typeof(DissonanceComms), "Start");
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return IsReady;
    }
}
