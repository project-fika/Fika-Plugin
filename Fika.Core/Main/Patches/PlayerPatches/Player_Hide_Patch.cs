using EFT;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches;

public class Player_Hide_Patch : FikaPatch
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
