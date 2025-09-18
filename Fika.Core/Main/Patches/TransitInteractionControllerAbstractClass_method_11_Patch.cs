using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches;

public class TransitInteractionControllerAbstractClass_method_11_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TransitInteractionControllerAbstractClass)
            .GetMethod(nameof(TransitInteractionControllerAbstractClass.method_11));
    }

    [PatchPrefix]
    public static void Prefix(Player player)
    {
        if (FikaBackendUtils.IsClient && player.IsYourPlayer && player is FikaPlayer fikaPlayer)
        {
            fikaPlayer.InventoryController.GetTraderServicesDataFromServer(FikaGlobals.TransitTraderId);
        }
    }
}
