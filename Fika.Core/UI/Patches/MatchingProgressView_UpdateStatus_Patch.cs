using System.Reflection;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Patches.LocalGame;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

public class MatchingProgressView_UpdateStatus_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchingProgressView).GetMethod(nameof(MatchingProgressView.UpdateStatus));
    }

    [PatchPostfix]
    public static void Postfix(MatchingProgressView __instance)
    {
        var text = ScreenUpdater.StatusText;
        if (text != null && __instance._stageTitle != null)
        {
            __instance._stageTitle.SetText(text);
        }
    }
}
