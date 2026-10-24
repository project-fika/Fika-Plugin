using System.Reflection;
using CommonAssets.Scripts.Cutscenes;
using Fika.Core.Main.Components;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

public class CutscenesServerController_PlayerViewCutsceneComplete_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CutscenesServerController),
            nameof(CutscenesServerController.PlayerViewCutsceneComplete));
    }

    [PatchPrefix]
    public static void Prefix(CutscenesServerController __instance, int cutsceneProcessId, out bool __state)
    {
        __state = __instance._cutsceneProcesses.ContainsKey(cutsceneProcessId);
        ObservedPlayersAside.Begin();
    }

    [PatchFinalizer]
    public static void Finalizer()
    {
        ObservedPlayersAside.End();
    }

    [PatchPostfix]
    public static void Postfix(CutscenesServerController __instance, int cutsceneProcessId, bool __state)
    {
        if (__state && !__instance._cutsceneProcesses.ContainsKey(cutsceneProcessId))
        {
            FikaCutsceneManager.Instance?.CutsceneEnded(cutsceneProcessId, false);
        }
    }
}
