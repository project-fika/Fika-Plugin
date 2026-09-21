using System.Reflection;
using Dissonance;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.VOIP;

public class DissonanceComms_Start_Patch : ModulePatch
{
    public static bool IsReady;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(DissonanceComms)
            .GetMethod(nameof(DissonanceComms.Initialize));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return IsReady;
    }
}
