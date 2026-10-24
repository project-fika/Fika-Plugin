using System.Reflection;
using EFT.Vehicle;
using Fika.Core.Main.Utils;
using SPTushonka.Reflection.Patching;

namespace Fika.Core.Main.Patches.BTR;

/// <summary>
/// Makes sure that the BTRShooter is updated on clients so that it moves with the BTR
/// </summary>
public class BTRTurretView_AttachBot_Postfix : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BTRTurretView)
            .GetMethod(nameof(BTRTurretView.AttachBot));
    }

    [PatchPostfix]
    public static void Postfix(BTRTurretView __instance)
    {
        if (!FikaBackendUtils.IsClient)
        {
            return;
        }

        var offlineBot = __instance._offlineBot;
        if (offlineBot != null && offlineBot.Item2 && offlineBot.Item1 != null)
        {
            offlineBot.Item1.Transform.Original.position = __instance.BotRoot.position;
        }
    }
}
