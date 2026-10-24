using System.Reflection;
using CommonAssets.Scripts.Cutscenes;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

public class CutscenesClientController_EnableNoCutsceneObjectsViews_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(CutscenesClientController),
            nameof(CutscenesClientController.EnableNoCutsceneObjectsViews));
    }

    [PatchPostfix]
    public static void Postfix(CutscenesClientController __instance)
    {
        ObservedPlayersAside.ViewsEnabled(__instance);
    }
}
