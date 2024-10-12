using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class ObservedLootItem_OnRigidBodyStopped_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(ObservedLootItem).GetMethod(nameof(ObservedLootItem.OnRigidbodyStopped));
		}

		[PatchPrefix]
		public static void Prefix(ObservedLootItem __instance)
		{
			if (FikaBackendUtils.IsServer)
			{
				LootSyncPacket packet = new()
				{
					Data = new()
					{
						Id = __instance.GetNetId(),
						Position = __instance.transform.position,
						Rotation = __instance.transform.rotation,
						Done = true
					}
				};
				Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
			}
		}
	}
}
