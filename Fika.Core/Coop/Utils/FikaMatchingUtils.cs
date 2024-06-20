using System;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;

namespace Fika.Core.Coop.Utils;

public static class FikaMatchingUtils
{
    public static DefaultUIButton BackButton { get; internal set; }
    public static DefaultUIButton AcceptButton { get; internal set; }
    public static RaidSettings RaidSettings { get; set; }
    
    public static async Task<bool> JoinMatch(string profileId, string joinServerId)
        {
            FikaPlugin.Instance.FikaLogger.LogInfo($"Joining match {profileId} {joinServerId}");
            
            FikaPingingClient pingingClient = new();
            if (pingingClient.Init(joinServerId))
            {
                int attempts = 0;
                bool success;

                FikaPlugin.Instance.FikaLogger.LogInfo("Attempting to connect to host session...");

                do
                {
                    attempts++;

                    pingingClient.PingEndPoint();
                    pingingClient.NetClient.PollEvents();
                    success = pingingClient.Received;

                    await Task.Delay(100);
                } while (!success && attempts < 50);

                if (!success)
                {
                    Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                        "ERROR CONNECTING",
                        "Unable to connect to the server. Make sure that all ports are open and that all settings are configured correctly.",
                        ErrorScreen.EButtonType.OkButton, 10f, null, null);

                    FikaPlugin.Instance.FikaLogger.LogError("Unable to connect to the session!");

                    return false;
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("Pinging failed??");
            }
           
            pingingClient.NetClient?.Stop();
            pingingClient = null;

            string errorMessage = null;

            // TODO, check only if they're the leader/not in a group
            // if (!MatchMakerAcceptScreenInstance) return false;

            FikaPlugin.Instance.FikaLogger.LogInfo($"Match join req");
            var body = new MatchJoinRequest(joinServerId, profileId);
            var result = FikaRequestHandler.RaidJoin(body);
            var detectedFikaVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (result.GameVersion != FikaPlugin.EFTVersionMajor)
            {
                errorMessage = $"You are attempting to use a different version of EFT than what the server is running.\nClient: {FikaPlugin.EFTVersionMajor}\nServer: {result.GameVersion}";
            }
            // else if (result.FikaVersion != detectedFikaVersion)
            // {
            //     errorMessage = $"You are attempting to use a different version of Fika than what the server is running.\nClient: {detectedFikaVersion}\nServer: {result.FikaVersion}";
            // }

            if (errorMessage != null)
            {
                FikaPlugin.Instance.FikaLogger.LogError(errorMessage);
                Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("ERROR JOINING", errorMessage, ErrorScreen.EButtonType.OkButton, 15, null, null);
                // return false;
            }

            FikaBackendUtils.SetServerId(result.ServerId);
            DetermineMatchingType(profileId, result.ServerId);

            AcceptButton.enabled = true;
            AcceptButton.Interactable = true;
            AcceptButton.OnClick.Invoke();

            return true;
        }
        
    public static void CreateMatch(string profileId, string hostUsername, RaidSettings raidSettings, bool dummy = false)
    {
        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateMatch(profileId, hostUsername, timestamp, raidSettings, raidSettings.Side, raidSettings.SelectedDateTime, dummy);

        FikaRequestHandler.RaidCreate(body);

        FikaBackendUtils.SetServerId(profileId);
        FikaBackendUtils.MatchingType = EMatchingType.GroupLeader;
    }
    
    public static void CreateTempMatch(string profileId, string hostUsername, RaidSettings raidSettings)
    {
        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        var body = new CreateMatch(profileId, hostUsername, timestamp, raidSettings, raidSettings.Side, raidSettings.SelectedDateTime, true);
        FikaRequestHandler.RaidCreate(body);
    }
    
    private static void DetermineMatchingType(string profileId, string _serverId)
    {
        if (FikaGroupUtils.GroupController != null)
        {
            FikaBackendUtils.MatchingType = FikaGroupUtils.GroupController.GetMatchingType(FikaBackendUtils.Profile.AccountId);
        }
        else if (_serverId == profileId)
        {
            FikaBackendUtils.MatchingType = EMatchingType.GroupLeader;
        }
        else if (!FikaGroupUtils.InGroup || FikaGroupUtils.IsGroupLeader)
        {
            FikaBackendUtils.MatchingType = EMatchingType.GroupLeader;
        }
        else if (FikaGroupUtils.InGroup && !FikaGroupUtils.IsGroupLeader)
        {
            FikaBackendUtils.MatchingType = EMatchingType.GroupPlayer;
        }
        else
        {
            FikaBackendUtils.MatchingType = EMatchingType.Single;
        }
    }
}