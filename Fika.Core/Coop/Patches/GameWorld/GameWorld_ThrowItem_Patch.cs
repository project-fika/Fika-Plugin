using EFT;
using EFT.Interactive;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class GameWorld_ThrowItem_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GameWorld).GetMethods().First(x => x.Name == nameof(GameWorld.ThrowItem) && x.GetParameters().Length == 3);
		}

		[PatchPostfix]
		public static void Postfix(LootItem __result, IPlayer player)
		{
			if (__result is ObservedLootItem observedLootItem)
			{
				if (player.IsYourPlayer || player.IsAI)
				{
					ItemPositionSyncer.Create(observedLootItem.gameObject, FikaBackendUtils.IsServer, observedLootItem);
					return;
				}

				Traverse.Create(observedLootItem).Field<bool>("bool_3").Value = true;
			}
		}
	}
}
