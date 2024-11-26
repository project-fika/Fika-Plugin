using Comfort.Common;
using EFT.Vehicle;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	public class BTRView_SyncViewFromServer_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(BTRView).GetMethod(nameof(BTRView.SyncViewFromServer));
		}

		[PatchPrefix]
		public static void Prefix(ref BTRDataPacket packet)
		{
			if (FikaBackendUtils.IsClient)
			{
				return;
			}

			BTRPacket btrPacket = new()
			{
				Data = packet
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref btrPacket, DeliveryMethod.Unreliable);
		}
	}
}
