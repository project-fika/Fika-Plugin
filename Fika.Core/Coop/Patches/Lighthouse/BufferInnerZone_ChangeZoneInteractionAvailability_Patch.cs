using Comfort.Common;
using EFT.BufferZone;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class BufferInnerZone_ChangeZoneInteractionAvailability_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BufferInnerZone).GetMethod(nameof(BufferInnerZone.ChangeZoneInteractionAvailability));
		}

		[PatchPostfix]
		public static void Postfix(bool isAvailable, EBufferZoneData changesDataType)
		{
			if (FikaBackendUtils.IsClient)
			{
				return;
			}

			BufferZonePacket packet = new(changesDataType)
			{
				Available = isAvailable
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
		}
	}
}
