using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using static EFT.TarkovApplication;

namespace Fika.Core.UI.Patches.LoadingScreen;

public class MapLoadingPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Class1505),
            nameof(Class1505.method_1));
    }

    [PatchPrefix]
    public static void Postfix(float pr)
    {
        var progress = 0f + (pr * 25f);
        LoadingScreenUI.Instance.UpdateAndBroadcast(progress);
    }
}
