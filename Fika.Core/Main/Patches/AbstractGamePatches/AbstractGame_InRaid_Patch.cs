using System.Reflection;
using EFT;
using Fika.Core.Main.GameMode;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.AbstractGamePatches;

/// <summary>
/// Used to support mods that rely on the <see cref="AbstractGame.InRaid"/> property, which normally casts to <see cref="LocalGame"/>
/// </summary>
internal class AbstractGame_InRaid_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(AbstractGame).GetProperty(nameof(AbstractGame.InRaid)).GetGetMethod();
    }

    [PatchPrefix]
    public static bool Prefix(AbstractGame __instance, ref bool __result)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        __result = __instance is CoopGame;
        return false;
    }
}
