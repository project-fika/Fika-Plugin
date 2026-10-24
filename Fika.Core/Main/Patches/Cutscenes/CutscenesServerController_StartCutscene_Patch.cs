using System.Reflection;
using CommonAssets.Scripts.Cutscenes;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

/// <summary>
/// The scene triggers that start a story cutscene run on every machine, and the process id is random per call,
/// so only the host is allowed to originate one. Clients wait for the host's packet and replay it.
/// </summary>
public class CutscenesServerController_StartCutscene_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CutscenesServerController),
            nameof(CutscenesServerController.StartCutscene));
    }

    [PatchPrefix]
    public static bool Prefix(ref CutsceneProcess __result, out bool __state)
    {
        __state = false;
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        var manager = FikaCutsceneManager.Instance;
        if (manager != null && FikaBackendUtils.IsClient && !manager.ReplayingHostStart)
        {
            __result = null;
            return false;
        }

        __state = true;
        ObservedPlayersAside.Begin();
        return true;
    }

    [PatchFinalizer]
    public static void Finalizer(bool __state)
    {
        if (__state)
        {
            ObservedPlayersAside.End();
        }
    }

    [PatchPostfix]
    public static void Postfix(string cutsceneId, bool fixedTime, bool forceMode, float initTime,
        CutsceneProcess __result)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return;
        }

        if (!FikaBackendUtils.IsServer)
        {
            return;
        }

        FikaCutsceneManager.Instance?.HostStartedCutscene(cutsceneId, __result, fixedTime, forceMode, initTime);
    }
}
