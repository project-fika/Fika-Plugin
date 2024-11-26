using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// This patch aims to alleviate problems with bots refilling mags too quickly causing a desync on <see cref="Item.Id"/>s by blocking firing for 0.7s
	/// </summary>
	public class BotReload_method_1_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BotReload).GetMethod(nameof(BotReload.method_1));
		}

		[PatchPrefix]
		public static void Prefix(BotOwner ___botOwner_0)
		{
			___botOwner_0.ShootData.BlockFor(0.7f);
		}
	}
}
