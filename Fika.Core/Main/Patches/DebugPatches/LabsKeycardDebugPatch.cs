using EFT;
using System.Reflection;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.DebugPatches;

/// <summary>
/// Removes the need for a Lab Keycard on debug builds
/// </summary>
[DebugPatch]
internal class LabsKeycardDebugPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MatchmakerOperation)
            .GetMethod(nameof(MatchmakerOperation.TryGetAccessToLocation));
    }

    [PatchPrefix]
    public static bool Prefix(ref string keyId, ref bool __result)
    {
        keyId = string.Empty;
        __result = true;
        return false;
    }
}
