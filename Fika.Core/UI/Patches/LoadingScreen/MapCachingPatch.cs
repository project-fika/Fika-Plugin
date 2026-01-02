using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using static EFT.TarkovApplication;

namespace Fika.Core.UI.Patches.LoadingScreen;

public class MapCachingPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Class1505),
            nameof(Class1505.method_2));
    }

    [PatchPrefix]
    public static void Postfix(float totalProgress)
    {
        var progress = 25f + (totalProgress * 25f);
        LoadingScreenUI.Instance.UpdateAndBroadcast(progress);
    }
}
