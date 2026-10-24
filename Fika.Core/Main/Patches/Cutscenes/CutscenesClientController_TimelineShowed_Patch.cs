using System.Reflection;
using CommonAssets.Scripts.Cutscenes;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

public class CutscenesClientController_TimelineShowed_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CutscenesClientController), "TimelineShowed");
    }

    [PatchPrefix]
    public static void Prefix(CutscenesClientController __instance, ref bool skip)
    {
        var manager = FikaCutsceneManager.Instance;
        if (FikaBackendUtils.IsTutorial || manager == null)
        {
            return;
        }

        if (!skip && !__instance._currentCutsceneViewerOnly)
        {
            manager.LocalViewComplete(__instance._cutsceneProcessId);
        }

        skip = true;
    }
}
