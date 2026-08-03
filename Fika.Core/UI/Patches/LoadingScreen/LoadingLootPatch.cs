using EFT;
using System.Reflection;
using Fika.Core.Main.GameMode;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches.LoadingScreen;

public class LoadingLootPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CoopGame),
            nameof(CoopGame.CG_method_20));
    }

    [PatchPrefix]
    public static void Postfix(InitLevelProgress p)
    {
        var progress = p.Stage == InitLevelStage.LoadingBundles
            ? 50f + (p.Progress * 20f)
            : 70f + (p.Progress * 5f);
        LoadingScreenUI.Instance.UpdateAndBroadcast(progress);
    }
}
