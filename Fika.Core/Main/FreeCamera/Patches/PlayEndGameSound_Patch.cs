using System.Reflection;
using EFT.UI;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.FreeCamera.Patches;

public class PlayEndGameSound_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GUISounds)
            .GetMethod(nameof(GUISounds.PlayEndGameSound), [typeof(EEndGameSoundType)]);
    }

    [PatchPrefix]
    private static bool Prefix()
    {
        // Don't play end game sound if spectator mode
        if (FikaBackendUtils.IsSpectator)
        {
            return false;
        }

        return true;
    }
}
