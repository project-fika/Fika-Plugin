using Comfort.Common;
using EFT;
using EFT.Vehicle;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.GameWorld;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Patches.BTR
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
			if(FikaBackendUtils.IsClient)
			{
				return;
			}

			Logger.LogInfo("BTRView SyncViewFromServer");

			BTRPacket btrPacket = new()
			{
				Data = packet
			};

			Singleton<FikaServer>.Instance.SendDataToAll(ref btrPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
		}
	}
}
