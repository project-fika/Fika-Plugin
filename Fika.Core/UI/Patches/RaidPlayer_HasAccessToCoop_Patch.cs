using System.Reflection;
using EFT.UI.Matchmaker;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// This allows all game editions to edit the <see cref="EFT.RaidSettings"/>
/// </summary>
public class RaidPlayer_HasAccessToCoop_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RaidPlayer)
            .GetProperty(nameof(RaidPlayer.HasAccessToCoop))
            .GetGetMethod();
    }

    [PatchPrefix]
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
