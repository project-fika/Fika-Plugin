using Comfort.Common;
using EFT;
using EFT.UI.Matchmaker;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Fika.Core.Coop.Utils
{
    public enum EMatchmakerType
    {
        Single = 0,
        GroupPlayer = 1,
        GroupLeader = 2
    }

    public static class FikaBackendUtils
    {
        public static MatchMakerAcceptScreen MatchMakerAcceptScreenInstance;
        public static Profile Profile;
        public static string PMCName;
        public static EMatchmakerType MatchingType = EMatchmakerType.Single;
        public static bool IsServer => MatchingType == EMatchmakerType.GroupLeader;
        public static bool IsClient => MatchingType == EMatchmakerType.GroupPlayer;
        public static bool IsDedicated = false;
        public static bool IsSinglePlayer
        {
            get
            {
                return Singleton<FikaServer>.Instantiated
                    && Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0;
            }
        }
        public static bool IsDedicatedGame = false;
        public static PlayersRaidReadyPanel PlayersRaidReadyPanel;
        public static MatchMakerGroupPreview MatchMakerGroupPreview;
        public static int HostExpectedNumberOfPlayers = 1;
        public static WeatherClass[] Nodes = null;
        public static string RemoteIp;
        public static int RemotePort;
        public static int LocalPort = 0;
        public static bool IsHostNatPunch = false;
        public static string HostLocationId;
        public static bool RequestFikaWorld = false;
        private static string groupId;
        private static string raidCode;

        public static MatchmakerTimeHasCome.TimeHasComeScreenClass ScreenController;

        public static string GetGroupId()
        {
            return groupId;
        }

        public static void SetGroupId(string newId)
        {
            groupId = newId;
        }

        public static void SetRaidCode(string newCode)
        {
            raidCode = newCode;
        }

        public static string GetRaidCode()
        {
            return raidCode;
        }

        public static bool JoinMatch(string profileId, string serverId, out CreateMatch result, out string errorMessage)
        {
            result = new CreateMatch();
            errorMessage = $"No server matches the data provided or the server no longer exists";

            if (MatchMakerAcceptScreenInstance == null)
            {
                return false;
            }

            MatchJoinRequest body = new(serverId, profileId);
            result = FikaRequestHandler.RaidJoin(body);

            if (result.GameVersion != FikaPlugin.EFTVersionMajor)
            {
                errorMessage = $"You are attempting to use a different version of EFT than what the server is running.\nClient: {FikaPlugin.EFTVersionMajor}\nServer: {result.GameVersion}";
                return false;
            }

            Version detectedFikaVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (result.FikaVersion != detectedFikaVersion)
            {
                errorMessage = $"You are attempting to use a different version of Fika than what the server is running.\nClient: {detectedFikaVersion}\nServer: {result.FikaVersion}";
                return false;
            }

            SetRaidCode(result.RaidCode);

            return true;
        }

        public static async Task CreateMatch(string profileId, string hostUsername, RaidSettings raidSettings)
        {
            NotificationManagerClass.DisplayWarningNotification("Starting raid, please wait...");
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string raidCode = GenerateRaidCode(6);
            CreateMatch body = new(raidCode, profileId, hostUsername, timestamp, raidSettings,
                HostExpectedNumberOfPlayers, raidSettings.Side, raidSettings.SelectedDateTime);

            await FikaRequestHandler.RaidCreate(body);

            SetGroupId(profileId);
            MatchingType = EMatchmakerType.GroupLeader;

            SetRaidCode(raidCode);
        }

        public static string GenerateRaidCode(int length)
        {
            Random random = new();
            char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
            string raidCode = "";
            for (int i = 0; i < length; i++)
            {
                int charIndex = random.Next(chars.Length);
                raidCode += chars[charIndex];
            }

            return raidCode;
        }
    }
}
