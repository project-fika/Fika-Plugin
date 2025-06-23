using EFT;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass1737_method_11_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1737).GetMethod(nameof(GClass1737.method_11));
        }

        [PatchPrefix]
        public static void Prefix(Player player)
        {
            if (FikaBackendUtils.IsClient && player.IsYourPlayer && player is CoopPlayer coopPlayer)
            {
                coopPlayer.InventoryController.GetTraderServicesDataFromServer(FikaGlobals.TransitTraderId);
            }
        }
    }
}
