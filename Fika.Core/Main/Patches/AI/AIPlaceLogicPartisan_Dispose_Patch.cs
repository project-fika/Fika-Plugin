using System;
using System.Reflection;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.AI;

/// <summary>
/// Fixes a live bug where there is no null check on the <see cref="AIPlaceInfo"/> during <see cref="AIPlaceLogicPartisan.Dispose"/>, causing a<see cref="NullReferenceException"/> if it is null
/// </summary>
public class AIPlaceLogicPartisan_Dispose_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(AIPlaceLogicPartisan)
            .GetMethod(nameof(AIPlaceLogicPartisan.Dispose));
    }

    [PatchPrefix]
    public static bool Prefix(AIPlaceLogicPartisan __instance)
    {
        if (__instance._aiPlaceInfo == null)
        {
            return false;
        }

        return true;
    }
}
