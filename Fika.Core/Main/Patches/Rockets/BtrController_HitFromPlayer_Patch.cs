using System.Reflection;
using EFT;
using EFT.Vehicle;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.Rockets;

public class BtrController_HitFromPlayer_Patch : ModulePatch
{
    private static bool _reported;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(BtrController).GetMethod(nameof(BtrController.HitFromPlayer));
    }

    [PatchPrefix]
    public static bool Prefix(Player player)
    {
        if (FikaBackendUtils.IsTutorial)
        {
            return true;
        }

        if (!FikaBackendUtils.IsServer)
        {
            return false;
        }

        var aiData = player != null ? player.AIData : null;

        if (aiData != null && aiData.PlayerHostilityData != null)
        {
            return true;
        }

        if (aiData != null && aiData.IsAI)
        {
            return false;
        }

        if (!_reported)
        {
            _reported = true;
            FikaGlobals.LogWarning($"BTR hit from {player?.GetType().Name} {player?.Profile?.Nickname} ({player?.ProfileId}) skipped, AIData: {aiData != null}, hostility data: {aiData?.PlayerHostilityData != null}, IsAI: {player?.IsAI}");
        }

        return false;
    }
}
