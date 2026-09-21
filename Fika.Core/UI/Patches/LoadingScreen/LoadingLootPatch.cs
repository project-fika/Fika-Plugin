using EFT;
using System.Reflection;
using Fika.Core.Main.GameMode;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.UI.Patches.LoadingScreen;

public class LoadingLootPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BaseLocalGame<EftGamePlayerOwner>),
            nameof(BaseLocalGame<EftGamePlayerOwner>._SpawnLoot_b__85_4));
    }

    [PatchPrefix]
    public static void Postfix(InitLevelProgress p)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        var progress = p.Stage == InitLevelStage.LoadingBundles
            ? 50f + (p.Progress * 20f)
            : 70f + (p.Progress * 5f);
        LoadingScreenUI.Instance.UpdateAndBroadcast(progress);
    }
}
