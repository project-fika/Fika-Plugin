using EFT;
using EFT.UI.Matchmaker;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Coop.Matchmaker
{
    public enum EMatchmakerType
    {
        Single = 0,
        GroupPlayer = 1,
        GroupLeader = 2
    }

    public static class MatchmakerAcceptPatches
    {
        #region Fields/Properties
        public static MatchMakerAcceptScreen MatchMakerAcceptScreenInstance { get; set; }
        public static Profile Profile { get; set; }
        public static string PMCName { get; set; }
        public static EMatchmakerType MatchingType { get; set; } = EMatchmakerType.Single;
        public static bool IsServer => MatchingType == EMatchmakerType.GroupLeader;
        public static bool IsClient => MatchingType == EMatchmakerType.GroupPlayer;
        public static bool IsSinglePlayer => MatchingType == EMatchmakerType.Single;
        public static PlayersRaidReadyPanel PlayersRaidReadyPanel { get; set; }
        public static MatchMakerGroupPreview MatchMakerGroupPreview { get; set; }
        public static int HostExpectedNumberOfPlayers { get; set; } = 1;
        public static WeatherClass[] Nodes { get; set; } = null;
        private static string groupId;
        private static long timestamp;
        #endregion

        #region Static Fields

        public static object MatchmakerScreenController
        {
            get
            {
                object screenController = typeof(MatchMakerAcceptScreen).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(x => x.Name == "ScreenController")
                    .FirstOrDefault().GetValue(MatchMakerAcceptScreenInstance);
                if (screenController != null)
                {
                    return screenController;
                }
                return null;
            }
        }

        public static GameObject EnvironmentUIRoot { get; internal set; }
        public static MatchmakerTimeHasCome.GClass3163 GClass3163 { get; internal set; }
        #endregion

        public static string GetGroupId()
        {
            return groupId;
        }

        public static void SetGroupId(string newId)
        {
            groupId = newId;
        }

        public static long GetTimestamp()
        {
            return timestamp;
        }

        public static void SetTimestamp(long ts)
        {
            timestamp = ts;
        }

        public static bool JoinMatch(string profileId, string serverId, out CreateMatch result, out string errorMessage)
        {
            result = new CreateMatch();
            errorMessage = $"No server matches the data provided or the server no longer exists";

            if (MatchMakerAcceptScreenInstance == null)
            {
                return false;
            }

            var body = new MatchJoinRequest(serverId, profileId);
            result = FikaRequestHandler.RaidJoin(body);

            if (result.GameVersion != FikaPlugin.EFTVersionMajor)
            {
                errorMessage = $"You are attempting to use a different version of EFT {FikaPlugin.EFTVersionMajor} than what the server is running {result.GameVersion}";
                return false;
            }

            var detectedFikaVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (result.FikaVersion != detectedFikaVersion)
            {
                errorMessage = $"You are attempting to use a different version of Fika {detectedFikaVersion} than what the server is running {result.FikaVersion}";
                return false;
            }

            return true;
        }

        public static void CreateMatch(string profileId, string hostUsername, RaidSettings raidSettings)
        {
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var body = new CreateMatch(profileId, hostUsername, timestamp, raidSettings, HostExpectedNumberOfPlayers, raidSettings.Side, raidSettings.SelectedDateTime);

            FikaRequestHandler.RaidCreate(body);

            SetGroupId(profileId);
            SetTimestamp(timestamp);
            MatchingType = EMatchmakerType.GroupLeader;
        }
    }
}
