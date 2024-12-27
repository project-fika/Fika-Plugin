using EFT;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GClass1668_method_9_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GClass1668).GetMethod(nameof(GClass1668.method_9));
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
