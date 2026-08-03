using System.Reflection;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;

namespace Fika.Core.UI.Patches;

/// <summary>
/// This allows all game editions to edit the <see cref="EFT.RaidSettings"/>
/// </summary>
public class MatchmakerPlayersController_GetCoopBlockReason_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchmakerPlayersController).GetMethod(nameof(MatchmakerPlayersController.GetCoopBlockReason));
    }

    [PatchPrefix]
    public static bool Prefix(ref ECoopBlock reason)
    {
        reason = ECoopBlock.NoBlock;
        return false;
    }
}
