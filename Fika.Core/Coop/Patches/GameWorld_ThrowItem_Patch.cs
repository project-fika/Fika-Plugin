using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Patch used to ensure items are always thrown in the correct direction if the dedicated client is hosting
	/// </summary>
	public class GameWorld_ThrowItem_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GameWorld).GetMethods().First(x => x.Name == nameof(GameWorld.ThrowItem) && x.GetParameters().Length == 3);
		}

		[PatchPrefix]
		public static bool Prefix(GameWorld __instance, Item item, IPlayer player, Vector3? direction, ref LootItem __result)
		{
			Vector3 vector = direction ?? (Quaternion.Euler(Mathf.Clamp(player.Rotation.y, -90f, 45f), player.Rotation.x, 0f) * new Vector3(0f, 1f, 1f));
			vector *= 2f;
			Vector3 vector2 = player.PlayerColliderPointOnCenterAxis(0.65f) + player.Velocity * Time.deltaTime;
			Quaternion quaternion = player.Transform.rotation * Quaternion.Euler(90f, 0f, 0f);
			Vector3 vector3 = new(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 2f * Mathf.Sign((float)Random.Range(-1, 2)));
			__result = __instance.ThrowItem(item, player, vector2, quaternion, vector + player.Velocity / 2f, vector3, true, true, EFTHardSettings.Instance.ThrowLootMakeVisibleDelay);
			return false;
		}
	}
}
