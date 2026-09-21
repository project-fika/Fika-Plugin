using EFT.RocketLauncher;
using System.Reflection;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Rockets;

/// <summary>
/// Do not run method unless server to avoid double damage
/// </summary>
public class BackblastModel_Initialize_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BackblastModel)
            .GetMethod(nameof(BackblastModel.Initialize));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        return FikaBackendUtils.IsServer;
    }
}
