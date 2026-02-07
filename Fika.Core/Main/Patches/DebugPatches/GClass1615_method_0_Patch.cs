using System.Reflection;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.DebugPatches;

/// <summary>
/// Used to speed up debugging
/// </summary>
[DebugPatch]
public class GClass1640_method_0_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TransitControllerAbstractClass)
            .GetMethod(nameof(TransitControllerAbstractClass.method_0));
    }

    [PatchPrefix]
    public static void Prefix(ref LocationSettingsClass.Location.TransitParameters[] parameters)
    {
        foreach (var parameter in parameters)
        {
            parameter.activateAfterSec = 10;
        }
    }
}
