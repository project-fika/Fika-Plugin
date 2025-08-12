using Dissonance;
using Fika.Core.Patching;
using HarmonyLib;
using System.Reflection;

namespace Fika.Core.Main.Patches.VOIP;

public class DissonanceComms_Start_Patch : FikaPatch
{
    public static bool IsReady;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(DissonanceComms), "Start");
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return IsReady;
    }
}
