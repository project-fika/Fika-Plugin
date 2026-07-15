using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Patches.VOIP;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using HarmonyLib;
using Newtonsoft.Json;

namespace Fika.Core.Main.Utils;

public enum EClientType
{
    None = 0,
    Client = 1,
    Host = 2
}

public static class FikaBackendUtils
{
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
    public static string PMCName => Profile.Nickname;
    /// <summary>
    /// If the current raid is a scav raid
    /// </summary>
    public static bool IsScav { get; internal set; }
    public static EClientType ClientType { get; internal set; } = EClientType.None;
    /// <summary>
    /// If this client is a headless client
    /// </summary>
    /// <remarks>
    /// Headless clients are always raid hosts (<see cref="IsServer"/> is always <see langword="true"/>) <br/>
    /// This check should mainly be used to prevent GPU/animation logic, etc.
    /// </remarks>
    public static bool IsHeadless { get; set; }
    /// <summary>
    /// If current session is a reconnect session
    /// </summary>
    public static bool IsReconnect { get; internal set; }
    /// <summary>
    /// If the raid host is a headless client
    /// </summary>
    public static bool IsHeadlessGame { get; set; }
    /// <summary>
    /// If this client requested the headless session
    /// </summary>
    public static bool IsHeadlessRequester { get; set; }
    /// <summary>
    /// If the current raid is a transit
    /// </summary>
    public static bool IsTransit { get; set; }
    /// <summary>
    /// If the current session is spectator only
    /// </summary>
    public static bool IsSpectator { get; internal set; }
    /// <summary>
    /// If the host is using NAT punching
    /// </summary>
    public static bool IsHostNatPunch { get; internal set; }
    public static IPEndPoint RemoteEndPoint { get; internal set; }
    public static ushort LocalPort { get; internal set; }
    public static string HostLocationId { get; internal set; }
    public static RaidSettings CachedRaidSettings { get; set; }
    public static GClass1628<GroupPlayerViewModelClass> GroupPlayers { get; set; } = [];
    public static FikaCustomRaidSettings CustomRaidSettings { get; set; } = new();

    internal static bool RequestFikaWorld;
    internal static Vector3 ReconnectPosition;
    internal static Vector2 ReconnectRotation;

    internal static MatchMakerAcceptScreen MatchMakerAcceptScreenInstance { get; set; }
    internal static PlayersRaidReadyPanel PlayersRaidReadyPanel { get; set; }
    internal static MatchMakerGroupPreview MatchMakerGroupPreview { get; set; }

    private static Profile _profile;

    public static void CleanUpVariables()
    {
        if (!IsTransit)
        {
            IsSpectator = false;
            IsHeadlessRequester = false;
            IsHeadlessGame = false;
        }

        MatchMakerAcceptScreenInstance = null;
        PlayersRaidReadyPanel = null;
        MatchMakerGroupPreview = null;

        RequestFikaWorld = false;
        IsReconnect = false;
        ReconnectPosition = Vector3.zero;
        ReconnectRotation = Vector2.zero;
        GroupPlayers?.Clear();
        DissonanceComms_Start_Patch.IsReady = false;
    }

    /// <summary>
    /// If this client is hosting the raid
    /// </summary>
    /// <remarks>A headless client is always server</remarks>
    public static bool IsServer
    {
        get
        {
            return ClientType == EClientType.Host;
        }
    }
    /// <summary>
    /// If this client joined a raid
    /// </summary>
    public static bool IsClient
    {
        get
        {
            return ClientType == EClientType.Client;
        }
    }
    /// <summary>
    /// If this client is hosting the raid and no clients are connectec
    /// </summary>
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
    /// <summary>
    /// The <see cref="Guid"/> of the current raid session
    /// </summary>
    public static Guid ServerGuid { get; internal set; }
    public static RaidTransitionInfoClass TransitData
    {
        get
        {
            return _transitData ?? new()
            {
                transitionType = ELocationTransition.None,
                transitionCount = 0,
                transitionRaidId = FikaGlobals.DefaultTransitId,
                visitedLocations = []
            };
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
        errorMessage = "No server matches the data provided or the server no longer exists";

        /*if (MatchMakerAcceptScreenInstance == null)
        {
            FikaGlobals.LogError("Could not find MatchMakerAcceptScreen");
            return false;
        }*/

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
        var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        var raidCode = GenerateRaidCode(6);
        var serverGuid = Guid.NewGuid();
        CreateMatch body = new(raidCode, profileId, serverGuid, hostUsername, IsSpectator, timestamp, raidSettings, FikaPlugin.Crc32,
            raidSettings.Side, raidSettings.SelectedDateTime, CustomRaidSettings);

        await FikaRequestHandler.RaidCreate(body);

        GroupId = profileId;
        ClientType = EClientType.Host;

        RaidCode = raidCode;
        ServerGuid = serverGuid;
    }

    internal static string GenerateRaidCode(int length)
    {
        System.Random random = new();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        var raidCode = "";
        for (var i = 0; i < length; i++)
        {
            var charIndex = random.Next(chars.Length);
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
            FikaGlobals.LogError("AddPartyMembers: Own profile was null!");
            return;
        }

        GroupPlayers.Clear();
        foreach ((var profile, var isLeader) in profiles)
        {
            var info = profile.Info;
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

        if (TarkovApplication.Exist(out var app))
        {
            var controller = app.MatchmakerPlayerControllerClass;
            if (controller != null)
            {
                var menuUi = Singleton<MenuUI>.Instance;
                if (menuUi != null)
                {
                    var panel = Traverse.Create(menuUi.MatchmakerTimeHasCome).Field<PartyInfoPanel>("_partyInfoPanel").Value;
                    panel.Close();
                    panel.Show(GroupPlayers, Profile, false);
                    return;
                }
                FikaGlobals.LogWarning("AddPartyMembers: MenuUI was null!");
                return;
            }
            FikaGlobals.LogWarning("AddPartyMembers: MatchmakerPlayerControllerClass was null!");
        }
        FikaGlobals.LogWarning("AddPartyMembers: TarkovApplication was null!");
    }
}

public class FikaCustomRaidSettings
{
    [JsonProperty("useCustomWeather")]
    public bool UseCustomWeather { get; set; }

    [JsonProperty("disableOverload")]
    public bool DisableOverload { get; set; }

    [JsonProperty("disableLegStamina")]
    public bool DisableLegStamina { get; set; }

    [JsonProperty("disableArmStamina")]
    public bool DisableArmStamina { get; set; }

    public override string ToString()
    {
        return $"UseCustomWeather: {UseCustomWeather}, DisableOverload: {DisableOverload}, DisableLegStamina: {DisableLegStamina}, DisableArmStamina: {DisableArmStamina}";
    }

    public void Reset()
    {
        UseCustomWeather = default;
        DisableOverload = default;
        DisableLegStamina = default;
        DisableArmStamina = default;
    }
}