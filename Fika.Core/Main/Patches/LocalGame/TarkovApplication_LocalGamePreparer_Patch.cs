using JsonType;
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

public sealed class TarkovApplication_LocalGamePreparer_Patch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.LocalGameMatching));
    }

    [PatchPrefix]
    public static async void Prefix(TarkovApplication __instance, RaidSettings ____raidSettings)
    {
        Logger.LogDebug("TarkovApplication_LocalGamePreparer_Patch:Prefix");

        FikaBackendUtils.RequestFikaWorld = true;

        var isServer = FikaBackendUtils.IsServer;
        if (!isServer && !string.IsNullOrEmpty(FikaBackendUtils.HostLocationId))
        {
            if (string.Equals(____raidSettings.LocationId, "sandbox", StringComparison.OrdinalIgnoreCase)
                && string.Equals(FikaBackendUtils.HostLocationId, "sandbox_high", StringComparison.OrdinalIgnoreCase))
            {
                ____raidSettings.SelectedLocation = __instance.Session.LocationSettings.locations.Values
                    .FirstOrDefault(IsSandboxHigh);

                NotificationManager.DisplayMessageNotification("Notification/HighLevelQueue".Localized(null),
                    ENotificationDurationType.Default, ENotificationIconType.Default, null);
            }

            if (string.Equals(____raidSettings.LocationId, "sandbox_high", StringComparison.OrdinalIgnoreCase)
                && string.Equals(FikaBackendUtils.HostLocationId, "sandbox", StringComparison.OrdinalIgnoreCase))
            {
                ____raidSettings.SelectedLocation = __instance.Session.LocationSettings.locations.Values
                    .FirstOrDefault(IsSandbox);
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
                var status = new SetStatusModel(FikaBackendUtils.GroupId, LobbyEntry.ELobbyStatus.COMPLETE);
                await FikaRequestHandler.UpdateSetStatus(status);
            }
        }
    }

    private static bool IsSandboxHigh(LocationSettings.Location location)
    {
        return string.Equals(location.Id, "sandbox_high", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSandbox(LocationSettings.Location location)
    {
        return string.Equals(location.Id, "sandbox", StringComparison.OrdinalIgnoreCase);
    }
}
