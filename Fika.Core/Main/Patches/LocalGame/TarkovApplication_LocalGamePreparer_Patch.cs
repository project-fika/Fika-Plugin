using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Communications;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.UI.Models;
using SPT.Reflection.Patching;

namespace Fika.Core.Main.Patches.LocalGame;

/// <summary>
/// Created by: Lacyway
/// </summary>
internal class TarkovApplication_LocalGamePreparer_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_41));
    }

    [PatchPrefix]
    public static async void Prefix(TarkovApplication __instance, RaidSettings ____raidSettings)
    {
        Logger.LogDebug("TarkovApplication_LocalGamePreparer_Patch:Prefix");

        FikaBackendUtils.RequestFikaWorld = true;

        var isServer = FikaBackendUtils.IsServer;
        if (!isServer)
        {
            if (!string.IsNullOrEmpty(FikaBackendUtils.HostLocationId))
            {
                if (string.Equals(____raidSettings.LocationId, "sandbox", System.StringComparison.OrdinalIgnoreCase)
                    && string.Equals(FikaBackendUtils.HostLocationId, "sandbox_high", System.StringComparison.OrdinalIgnoreCase))
                {
                    ____raidSettings.SelectedLocation = __instance.Session.LocationSettings.locations.Values
                        .FirstOrDefault(IsSandboxHigh);

                    NotificationManagerClass.DisplayMessageNotification("Notification/HighLevelQueue".Localized(null),
                        ENotificationDurationType.Default, ENotificationIconType.Default, null);
                }

                if (string.Equals(____raidSettings.LocationId, "sandbox_high", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(FikaBackendUtils.HostLocationId, "sandbox", StringComparison.OrdinalIgnoreCase))
                {
                    ____raidSettings.SelectedLocation = __instance.Session.LocationSettings.locations.Values
                        .FirstOrDefault(IsSandbox);
                }
            }
        }

        if (!FikaBackendUtils.IsTransit)
        {
#if DEBUG
            FikaGlobals.LogInfo("Creating net manager");
#endif
            NetManagerUtils.CreateNetManager(isServer);
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
        return string.Equals(location.Id, "sandbox_high", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSandbox(LocationSettingsClass.Location location)
    {
        return string.Equals(location.Id, "sandbox", StringComparison.OrdinalIgnoreCase);
    }
}
