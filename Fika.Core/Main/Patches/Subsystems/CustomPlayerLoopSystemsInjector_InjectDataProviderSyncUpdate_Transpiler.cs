using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using CustomPlayerLoopSystem;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Subsystems;

/// <summary>
/// Prevents logic that is never used from being injected into the <see cref="UnityEngine.LowLevel.PlayerLoop"/>
/// </summary>
internal class CustomPlayerLoopSystemsInjector_InjectDataProviderSyncUpdate_Transpiler : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(CustomPlayerLoopSystemsInjector).GetMethod(nameof(CustomPlayerLoopSystemsInjector.InjectDataProviderSyncUpdate));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        return false;
    }
}
