using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Patches
{
	public class GameWorld_ThrowItem_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(GameWorld).GetMethods().First(x => x.Name == nameof(GameWorld.ThrowItem) && x.GetParameters().Length == 9);
		}

		[PatchPostfix]
		public static void Prefix(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, bool syncable, LootItem __result)
		{
			if (syncable && FikaBackendUtils.IsServer)
			{
				LootSyncPacket packet = new()
				{
					Data = new()
					{
						Id = __result.GetNetId(),
						Position = position,
						Rotation = rotation,
						Velocity = velocity,
						AngularVelocity = angularVelocity,
						Done = false
					}
				};
				Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
			}
		}
	}
}
