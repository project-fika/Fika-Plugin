using System.Reflection;
using CommonAssets.Scripts.Cutscenes;
using EFT;
using Fika.Core.Main.Components;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Cutscenes;

public class CutscenesClientController_Dispose_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.Dispose));
    }

    [PatchPrefix]
    public static void Prefix()
    {
        FikaCutsceneManager.Instance?.Dispose();
    }
}
