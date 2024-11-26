using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class BufferZoneControllerClass_SetPlayerInZoneStatus_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BufferZoneControllerClass).GetMethod(nameof(BufferZoneControllerClass.SetPlayerInZoneStatus));
		}

		[PatchPostfix]
		public static void Postfix(string profileID, bool inZone)
		{
			if (FikaBackendUtils.IsClient)
			{
				return;
			}

			BufferZonePacket packet = new(EBufferZoneData.PlayerInZoneStatusChange)
			{
				ProfileId = profileID,
				Available = inZone
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
		}
	}
}
