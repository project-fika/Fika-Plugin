using System.Reflection;
using EFT.GameTriggers;
using Fika.Core.Main.Components;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

public class HandlerStartCutscene_Start_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(HandlerStartCutscene), "Start");
    }

    [PatchPostfix]
    public static void Postfix(HandlerStartCutscene __instance)
    {
        FikaStoryTriggers.Subscribe(__instance);
    }
}
