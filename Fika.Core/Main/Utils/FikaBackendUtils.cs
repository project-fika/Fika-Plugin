using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Patches.VOIP;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Fika.Core.Main.Utils;

public enum EClientType
{
    None = 0,
    Client = 1,
    Host = 2
}

public static class FikaBackendUtils
{
    internal static MatchMakerAcceptScreen MatchMakerAcceptScreenInstance;
    /// <summary>
    /// The local player PMC <see cref="EFT.Profile"/>
    /// </summary>
    public static Profile Profile
    {
        get
        {
            _profile ??= FikaGlobals.GetProfile(false);
            return _profile;
        }

        internal set
        {
            _profile = value;
        }
    }
    /// <summary>
    /// The name of the local player PMC
    /// </summary>
    public static string PMCName { get; internal set; }
    public static bool IsScav { get; internal set; }
    public static EClientType ClientType { get; internal set; } = EClientType.None;
    public static bool IsHeadless { get; set; }
    public static bool IsReconnect { get; internal set; }
    public static bool IsHeadlessGame { get; set; }
    public static bool IsHeadlessRequester { get; set; }
    public static bool IsTransit { get; set; }
    public static bool IsSpectator { get; internal set; }
    public static bool IsHostNatPunch { get; internal set; }
    public static string RemoteIp { get; internal set; }
    public static int RemotePort { get; internal set; }
    public static int LocalPort { get; internal set; } = 0;
    public static string HostLocationId { get; internal set; }
    public static IPAddress VPNIP { get; internal set; }
    public static RaidSettings CachedRaidSettings { get; set; }
    public static GClass3968<GroupPlayerViewModelClass> GroupPlayers { get; set; } = [];

    internal static bool RequestFikaWorld;
    internal static Vector3 ReconnectPosition = Vector3.zero;
    internal static PlayersRaidReadyPanel PlayersRaidReadyPanel;
    internal static MatchMakerGroupPreview MatchMakerGroupPreview;

    private static Profile _profile;

    public static void CleanUpVariables()
    {
        if (!IsTransit)
        {
            IsSpectator = false;
            IsHeadlessRequester = false;
        }

        RequestFikaWorld = false;
        IsReconnect = false;
        ReconnectPosition = Vector3.zero;
        GroupPlayers?.Clear();
        DissonanceComms_Start_Patch.IsReady = false;
    }

    public static bool IsServer
    {
        get
        {
            return ClientType == EClientType.Host;
        }
    }
    public static bool IsClient
    {
        get
        {
            return ClientType == EClientType.Client;
        }
    }
    public static bool IsSinglePlayer
    {
        get
        {
            return Singleton<FikaServer>.Instantiated
                && Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0;
        }
    }
    public static string GroupId { get; internal set; }
    public static string RaidCode { get; internal set; }
    public static RaidTransitionInfoClass TransitData
    {
        get
        {
            if (_transitData == null)
            {
                return new()
                {
                    transitionType = ELocationTransition.None,
                    transitionCount = 0,
                    transitionRaidId = FikaGlobals.DefaultTransitId,
                    visitedLocations = []
                };
            }

            return _transitData;
        }
        set
        {
            _transitData = value;
        }
    }

    private static RaidTransitionInfoClass _transitData;

    public static void ResetTransitData()
    {
        TransitData = null;
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
            errorMessage = string.Format(LocaleUtils.UI_ERROR_HOST_EFT_MISMATCH.Localized(), FikaPlugin.EFTVersionMajor, result.GameVersion);
            return false;
        }

        if (result.Crc32 != FikaPlugin.Crc32)
        {
            errorMessage = string.Format(LocaleUtils.UI_ERROR_HOST_FIKA_MISMATCH.Localized(), FikaPlugin.Crc32, result.Crc32);
            return false;
        }

        RaidCode = result.RaidCode;

        return true;
    }

    public static async Task CreateMatch(string profileId, string hostUsername, RaidSettings raidSettings)
    {
        NotificationManagerClass.DisplayWarningNotification(LocaleUtils.STARTING_RAID.Localized());
        long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        string raidCode = GenerateRaidCode(6);
        CreateMatch body = new(raidCode, profileId, hostUsername, IsSpectator, timestamp, raidSettings, FikaPlugin.Crc32,
            raidSettings.Side, raidSettings.SelectedDateTime);

        await FikaRequestHandler.RaidCreate(body);

        GroupId = profileId;
        ClientType = EClientType.Host;

        RaidCode = raidCode;
    }

    internal static string GenerateRaidCode(int length)
    {
        System.Random random = new();
        char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        string raidCode = "";
        for (int i = 0; i < length; i++)
        {
            int charIndex = random.Next(chars.Length);
            raidCode += chars[charIndex];
        }

        return raidCode;
    }

    internal static void AddPartyMembers(Dictionary<Profile, bool> profiles)
    {
        if (IsHeadless)
        {
            return;
        }

        if (Profile == null)
        {
            FikaPlugin.Instance.FikaLogger.LogError("AddPartyMembers: Own profile was null!");
            return;
        }

        GroupPlayers.Clear();
        foreach ((Profile profile, bool isLeader) in profiles)
        {
            InfoClass info = profile.Info;
            GroupPlayerDataClass infoSet = new()
            {
                AccountId = profile.AccountId,
                Id = profile.Id,
                IsLeader = isLeader,
                Info = new()
                {
                    Level = info.Level,
                    MemberCategory = info.MemberCategory,
                    SelectedMemberCategory = info.SelectedMemberCategory,
                    Nickname = profile.GetCorrectedNickname(),
                    Side = info.Side,
                    GameVersion = info.GameVersion,
                    HasCoopExtension = info.HasCoopExtension
                }
            };
            GroupPlayerViewModelClass visualProfile = new(infoSet)
            {
                PlayerVisualRepresentation = profile.GetVisualEquipmentState(false)
            };

            GroupPlayers.Add(visualProfile);
        }

        if (TarkovApplication.Exist(out TarkovApplication app))
        {
            MatchmakerPlayerControllerClass controller = app.MatchmakerPlayerControllerClass;
            if (controller != null)
            {
                MenuUI menuUi = Singleton<MenuUI>.Instance;
                if (menuUi != null)
                {
                    PartyInfoPanel panel = Traverse.Create(menuUi.MatchmakerTimeHasCome).Field<PartyInfoPanel>("_partyInfoPanel").Value;
                    panel.Close();
                    panel.Show(GroupPlayers, Profile, false);
                    return;
                }
                FikaPlugin.Instance.FikaLogger.LogWarning("AddPartyMembers: MenuUI was null!");
                return;
            }
            FikaPlugin.Instance.FikaLogger.LogWarning("AddPartyMembers: MatchmakerPlayerControllerClass was null!");
        }
        FikaPlugin.Instance.FikaLogger.LogWarning("AddPartyMembers: TarkovApplication was null!");
    }
}
