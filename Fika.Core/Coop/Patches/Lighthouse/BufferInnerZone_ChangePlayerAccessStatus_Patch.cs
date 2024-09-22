using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class BufferInnerZone_ChangePlayerAccessStatus_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BufferInnerZone).GetMethod(nameof(BufferInnerZone.ChangePlayerAccessStatus));
		}

		[PatchPostfix]
		public static void Postfix(string profileID, bool status)
		{
			if (FikaBackendUtils.IsClient)
			{
				return;
			}

			BufferZonePacket packet = new(EBufferZoneData.PlayerAccessStatus)
			{
				ProfileId = profileID,
				Available = status
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
		}
	}
}
