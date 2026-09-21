using System.Reflection;
using EFT;
using SPTushonka.Reflection.Patching;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.Patches.PlayerPatches;

public class Player_Hide_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocalPlayer)
            .GetMethod(nameof(LocalPlayer.Hide));
    }

    [PatchPrefix]
    public static bool Prefix(EFT.LocalPlayer __instance)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        __instance.botPlayerCulling.Hide();
        return false;
    }
}
