using JsonType;
using System.Reflection;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.DebugPatches;

/// <summary>
/// Used to speed up debugging
/// </summary>
[DebugPatch]
public class TransitController_InitTransitPoints_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(EFT.TransitController)
            .GetMethod(nameof(EFT.TransitController.InitTransitPoints));
    }

    [PatchPrefix]
    public static void Prefix(ref LocationSettings.Location.TransitParameters[] parameters)
    {
        foreach (var parameter in parameters)
        {
            parameter.activateAfterSec = 10;
        }
    }
}
