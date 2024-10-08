using EFT;
using EFT.Communications;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Http;
using Fika.Core.UI.Models;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Coop.Patches
{
	/// <summary>
	/// Created by: Lacyway
	/// </summary>
	internal class TarkovApplication_LocalGamePreparer_Patch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_39));
		}

		[PatchPrefix]
		public static async void Prefix(TarkovApplication __instance, RaidSettings ____raidSettings)
		{
			Logger.LogDebug("TarkovApplication_LocalGamePreparer_Patch:Prefix");

			FikaBackendUtils.RequestFikaWorld = true;

			bool isServer = FikaBackendUtils.IsServer;
			if (!isServer)
			{
				if (!string.IsNullOrEmpty(FikaBackendUtils.HostLocationId))
				{
					if (____raidSettings.LocationId.ToLower() == "sandbox" && FikaBackendUtils.HostLocationId.ToLower() == "sandbox_high")
					{
						LocationSettingsClass.Location sandboxHigh = __instance.Session.LocationSettings.locations.Values.FirstOrDefault
							(IsSandboxHigh);
						____raidSettings.SelectedLocation = sandboxHigh;

						NotificationManagerClass.DisplayMessageNotification("Notification/HighLevelQueue".Localized(null),
							ENotificationDurationType.Default, ENotificationIconType.Default, null);
					}

					if (____raidSettings.LocationId.ToLower() == "sandbox_high" && FikaBackendUtils.HostLocationId.ToLower() == "sandbox")
					{
						LocationSettingsClass.Location sandbox = __instance.Session.LocationSettings.locations.Values.FirstOrDefault
							(IsSandbox);
						____raidSettings.SelectedLocation = sandbox;
					}
				}
			}

			if (!FikaBackendUtils.IsTransit)
			{
				NetManagerUtils.CreateNetManager(FikaBackendUtils.IsServer);
				if (isServer)
				{
					NetManagerUtils.StartPinger();
				}
				await NetManagerUtils.InitNetManager(isServer);

				if (isServer)
				{
					SetStatusModel status = new(FikaBackendUtils.GroupId, LobbyEntry.ELobbyStatus.COMPLETE);
					await FikaRequestHandler.UpdateSetStatus(status);
				}
			}
		}

		private static bool IsSandboxHigh(LocationSettingsClass.Location location)
		{
			return location.Id.ToLower() == "sandbox_high";
		}

		private static bool IsSandbox(LocationSettingsClass.Location location)
		{
			return location.Id.ToLower() == "sandbox";
		}
	}
}
