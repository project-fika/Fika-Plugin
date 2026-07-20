using System.Reflection;
using SPT.Reflection.Patching;
using EFT;

namespace Fika.Core.UI.Patches;

/// <summary>
/// This allows all game editions to edit the <see cref="RaidSettings"/>
/// </summary>
public class MainMenuShowOperation_method_55_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MainMenuShowOperation)
            .GetMethod(nameof(MainMenuShowOperation.method_55));
    }

    [PatchPrefix]
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
