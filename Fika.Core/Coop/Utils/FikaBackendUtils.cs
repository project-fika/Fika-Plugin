using EFT;
using EFT.UI.Matchmaker;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using System;
using System.Reflection;
using Fika.Core.UI.Custom;

namespace Fika.Core.Coop.Utils
{
    public static class FikaBackendUtils
    {
        public static MatchMakerAcceptScreen MatchMakerAcceptScreenInstance;
        public static IProfileDataContainer Profile;
        public static string PmcName;
        public static EMatchingType MatchingType = EMatchingType.Single;
        public static bool IsServer => Profile.ProfileId == _serverId;
        public static bool IsClient => MatchingType is EMatchingType.GroupPlayer or EMatchingType.GroupLeader;
        public static bool IsSinglePlayer => MatchingType == EMatchingType.Single;
        
        public static PlayersRaidReadyPanel PlayersRaidReadyPanel;
        public static MatchMakerGroupPreview MatchMakerGroupPreview;
        public static int HostExpectedNumberOfPlayers = 1;
        public static WeatherClass[] Nodes = null;
        public static string RemoteIp;
        public static int RemotePort;
        public static int LocalPort = 0;
        public static bool IsHostNatPunch = false;
        private static string _serverId;
        private static string _raidCode;

        public static MatchmakerTimeHasCome.GClass3187 ScreenController;
        public static RaidSettings RaidSettings { get; set; }
        public static MatchMakerUIScript MatchMakerUIScript;

        public static string GetServerId()
        {
            return _serverId;
        }

        public static void SetServerId(string newId)
        {
            _serverId = newId;
        }

        public static void SetRaidCode(string newCode)
        {
            _raidCode = newCode;
        }

        public static string GetRaidCode()
        {
            return _raidCode;
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

        public static void CreateMatch(string profileId, string hostUsername, RaidSettings raidSettings)
        {
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string raidCode = GenerateRaidCode(6);
            CreateMatch body = new(raidCode, profileId, hostUsername, timestamp, raidSettings,
                HostExpectedNumberOfPlayers, raidSettings.Side, raidSettings.SelectedDateTime);

            FikaRequestHandler.RaidCreate(body);

            SetServerId(profileId);
            MatchingType = EMatchingType.GroupLeader;

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
