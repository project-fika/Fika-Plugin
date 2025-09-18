using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.UI.Patches;

/// <summary>
/// This allows all game editions to edit the <see cref="EFT.RaidSettings"/>
/// </summary>
public class MainMenuControllerClass_method_54_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MainMenuControllerClass)
            .GetMethod(nameof(MainMenuControllerClass.method_54));
    }

    [PatchPrefix]
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
