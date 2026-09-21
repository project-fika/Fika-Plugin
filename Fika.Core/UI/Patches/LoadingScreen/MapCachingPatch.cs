using System.Reflection;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using static EFT.TarkovApplication;
using Fika.Core.Main.Utils;

namespace Fika.Core.UI.Patches.LoadingScreen;

public class MapCachingPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EFT.TarkovApplication.__c__DisplayClass222_0),
            nameof(EFT.TarkovApplication.__c__DisplayClass222_0._LoadMapAndData_b__0));
    }

    [PatchPrefix]
    public static void Postfix(float totalProgress)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        var progress = 25f + (totalProgress * 25f);
        LoadingScreenUI.Instance.UpdateAndBroadcast(progress);
    }
}
