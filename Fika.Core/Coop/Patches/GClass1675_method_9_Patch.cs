using EFT;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
    public class GClass1675_method_9_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1675).GetMethod(nameof(GClass1675.method_9));
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
