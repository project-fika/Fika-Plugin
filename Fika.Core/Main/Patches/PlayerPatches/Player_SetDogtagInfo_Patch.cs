using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.PlayerPatches;

/// <summary>
/// This patch stops BSGs dogtag handling as it is poorly executed
/// </summary>
public class Player_SetDogtagInfo_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.SetDogtagInfo));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        return false;
    }
}
