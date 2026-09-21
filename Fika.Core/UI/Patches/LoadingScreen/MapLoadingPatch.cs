using System.Reflection;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using static EFT.TarkovApplication;

namespace Fika.Core.UI.Patches.LoadingScreen;

public class MapLoadingPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EFT.TarkovApplication.__c__DisplayClass222_0),
            nameof(EFT.TarkovApplication.__c__DisplayClass222_0._LoadMapAndData_b__4));
    }

    [PatchPrefix]
    public static void Postfix(float pr)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        var progress = 0f + (pr * (FikaBackendUtils.IsHeadless ? 50f : 25f)); // headless doesn't cache culling
        LoadingScreenUI.Instance.UpdateAndBroadcast(progress);
    }
}
