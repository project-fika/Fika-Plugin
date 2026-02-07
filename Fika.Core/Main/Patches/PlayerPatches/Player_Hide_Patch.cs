using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.PlayerPatches;

public class Player_Hide_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LocalPlayer)
            .GetMethod(nameof(LocalPlayer.Hide));
    }

    [PatchPrefix]
    public static bool Prefix(LocalPlayerCullingHandlerClass ___localPlayerCullingHandlerClass)
    {
        ___localPlayerCullingHandlerClass.Hide();
        return false;
    }
}
