using System.Collections.Generic;
using System.Reflection;
using CommonAssets.Scripts.Cutscenes;
using Fika.Core.Main.Components;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

public class CutscenesServerController_EndCutscenes_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CutscenesServerController), nameof(CutscenesServerController.EndCutscenes));
    }

    [PatchPrefix]
    public static void Prefix(CutscenesServerController __instance, Il2CppSystem.Collections.Generic.List<int> processIds,
        out List<int> __state)
    {
        __state = [];
        for (var i = 0; processIds != null && i < processIds.Count; i++)
        {
            if (__instance._cutsceneProcesses.ContainsKey(processIds[i]))
            {
                __state.Add(processIds[i]);
            }
        }

        ObservedPlayersAside.Begin();
    }

    [PatchFinalizer]
    public static void Finalizer()
    {
        ObservedPlayersAside.End();
    }

    [PatchPostfix]
    public static void Postfix(CutscenesServerController __instance, List<int> __state)
    {
        foreach (var processId in __state)
        {
            if (!__instance._cutsceneProcesses.ContainsKey(processId))
            {
                FikaCutsceneManager.Instance?.CutsceneEnded(processId, true);
            }
        }
    }
}
