using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Models.Presence;
using HarmonyLib;
using JsonType;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Custom;

public class MainMenuUIScript : MonoBehaviour
{
    public static bool Exist
    {
        get
        {
            return _instance != null;
        }
    }

    public static MainMenuUIScript Instance
    {
        get
        {
            return _instance;
        }
    }

    public bool JoinInProgress
    {
        get => _joinInProgress;
        set
        {
            lock (_joinInProgressLock)
            {
                _joinInProgress = value;
            }
        }
    }

    private static MainMenuUIScript _instance;

    private Coroutine _queryRoutine;
    private MainMenuUI _mainMenuUI;
    private GameObject _playerTemplate;
    private GameObject _userInterface;
    private List<GameObject> _players;
    private DateTime _lastRefresh;
    private DateTime _lastSet;
    private RectTransform _transformToScale;
    private GInterface225<RaidSettings> _backendSession;

    private bool _joinInProgress;
    private object _joinInProgressLock = new();

    private const int _minSecondsToWait = 2;

    private DateTime BackendTime
    {
        get
        {
            if (_backendSession == null)
            {
                return DateTime.MinValue;
            }
            return _backendSession.GetCurrentLocationTime;
        }
    }

    private DateTime StaticTime
    {
        get
        {
            return new DateTime(2016, 8, 4, 15, 28, 0);
        }
    }

    protected void Start()
    {
        _instance = this;
        _players = [];
        _lastRefresh = DateTime.Now;
        _lastSet = DateTime.Now;
        if (TarkovApplication.Exist(out var tarkovApplication) && tarkovApplication.Session != null)
        {
            _backendSession = tarkovApplication.Session;
        }
        CreateMainMenuUI();
    }

    protected void OnEnable()
    {
        if (!FikaPlugin.Instance.Settings.EnableOnlinePlayers.Value)
        {
            if (_userInterface != null)
            {
                _userInterface.SetActive(false);
            }
            return;
        }

        if (_userInterface != null)
        {
            _userInterface.SetActive(true);
        }

        if (_transformToScale != null)
        {
            var scale = FikaPlugin.Instance.Settings.OnlinePlayersScale.Value;
            _transformToScale.localScale = new(scale, scale, scale);
        }
        _queryRoutine = StartCoroutine(QueryPlayers());
    }

    protected void OnDisable()
    {
        if (_queryRoutine != null)
        {
            StopCoroutine(_queryRoutine);
        }
    }

    private void CreateMainMenuUI()
    {
        var mainMenuUIPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.MainMenuUI);
        var mainMenuUI = GameObject.Instantiate(mainMenuUIPrefab);
        _mainMenuUI = mainMenuUI.GetComponent<MainMenuUI>();
        _playerTemplate = _mainMenuUI.PlayerTemplate;
        var newParent = Singleton<CommonUI>.Instance.MenuScreen.gameObject.transform;
        mainMenuUI.transform.SetParent(newParent);
        gameObject.transform.SetParent(newParent);

        var mainMenuUITransform = _mainMenuUI.gameObject.transform;
        var objectToAttach = mainMenuUITransform.GetChild(0).GetChild(0).gameObject;
        _userInterface = objectToAttach;
        _transformToScale = objectToAttach.RectTransform();
        var scale = FikaPlugin.Instance.Settings.OnlinePlayersScale.Value;
        _transformToScale.localScale = new(scale, scale, scale);
        objectToAttach.AddComponent<UIDragComponent>().Init(_transformToScale, true);
        _mainMenuUI.RefreshButton.onClick.AddListener(ManualRefresh);

        gameObject.SetActive(false);
        this.WaitFrames(3, null);
        gameObject.SetActive(true);
    }

    private void ManualRefresh()
    {
        if ((DateTime.Now - _lastRefresh).TotalSeconds >= 5)
        {
            _lastRefresh = DateTime.Now;
            ClearAndQueryPlayers();
        }
    }

    private IEnumerator QueryPlayers()
    {
        WaitForEndOfFrame waitForEndOfFrame = new();
        WaitForSeconds waitForSeconds = new(10);
        while (true)
        {
            yield return waitForEndOfFrame;
            ClearAndQueryPlayers();
            yield return waitForSeconds;
        }
    }

    private void ClearAndQueryPlayers()
    {
        foreach (var item in _players)
        {
            GameObject.Destroy(item);
        }
        _players.Clear();

        var response = FikaRequestHandler.GetPlayerPresences();
        _mainMenuUI.UpdateLabel(response.Length);
        SetupPlayers(ref response);
    }

    private string ConvertToTime(EDateTime dateTime, bool staticTime)
    {
        if (staticTime)
        {
            var staticDate = StaticTime;
            return dateTime == EDateTime.CURR ? staticDate.ToString("HH:mm:ss") : staticDate.AddHours(-12).ToString("HH:mm:ss");
        }

        var backendTime = BackendTime;
        if (backendTime == DateTime.MinValue)
        {
            return "ERROR";
        }
        return dateTime == EDateTime.CURR ? backendTime.ToString("HH:mm:ss") : backendTime.AddHours(-12).ToString("HH:mm:ss");
    }

    private bool IsStaticTimeLocation(string location)
    {
        return location is "factory4_day" or "factory4_night" or "laboratory";
    }

    private void SetupPlayers(ref FikaPlayerPresence[] responses)
    {
        foreach (var presence in responses)
        {
            var newPlayer = GameObject.Instantiate(_playerTemplate, _playerTemplate.transform.parent);
            var mainMenuUIPlayer = newPlayer.GetComponent<MainMenuUIPlayer>();
            mainMenuUIPlayer.SetActivity(presence.Nickname, presence.Level, presence.Activity);
            if (presence.Activity is EFikaPlayerPresence.IN_RAID && presence.RaidInformation.HasValue)
            {
                var information = presence.RaidInformation.Value;
                FikaGlobals.LogInfo($"Got presense: {information.Started}, {information.MatchId}");
                var side = information.Side == ESideType.Pmc ? "RaidSidePmc".Localized() : "RaidSideScav".Localized();
                var time = ConvertToTime(information.Time, IsStaticTimeLocation(information.Location));
                var tooltip = newPlayer.AddComponent<HoverTooltipArea>();
                tooltip.enabled = true;
                tooltip.SetMessageText(string.Format(LocaleUtils.UI_MMUI_RAID_DETAILS.Localized(), side,
                    ColorizeText(EColor.BLUE, information.Location.Localized()),
                    BoldText(time)));

                if (!information.Started)
                {
                    if (JoinInProgress)
                    {
                        return;
                    }

                    JoinInProgress = true;

                    var buttonGo = mainMenuUIPlayer.JoinButton.transform.parent.gameObject;
                    buttonGo.SetActive(true);
                    var tooltip2 = buttonGo.AddComponent<HoverTooltipArea>();
                    tooltip2.enabled = true;
                    tooltip2.SetMessageText(LocaleUtils.UI_JOIN_RAID.Localized());
                    mainMenuUIPlayer.JoinButton.onClick.AddListener(async () =>
                    {
                        if (!TarkovApplication.Exist(out var tarkovApplication))
                        {
                            FikaGlobals.LogError("OnFikaStartRaid: Could not find TarkovApplication");
                            JoinInProgress = false;
                            return;
                        }

                        var session = tarkovApplication.Session;
                        if (session == null)
                        {
                            FikaGlobals.LogError("Session was null when starting the raid");
                            JoinInProgress = false;
                            return;
                        }

                        FikaBackendUtils.Profile = session.Profile;

                        var location = session.LocationSettings.locations.Values
                            .FirstOrDefault(l => l.Id == information.Location);

                        if (location == null)
                        {
                            FikaGlobals.LogError($"Failed to find location {information.Location}");
                            JoinInProgress = false;
                            return;
                        }

                        NotificationManagerClass.DisplayMessageNotification(LocaleUtils.CONNECTING_TO_SESSION.Localized(),
                            iconType: EFT.Communications.ENotificationIconType.EntryPoint);
                        using var pingingClient = await NetManagerUtils.CreatePingingClient();

                        if (pingingClient.Init(information.MatchId))
                        {
                            FikaGlobals.LogInfo("Attempting to connect to host session...");
                            var knockMessage = FikaBackendUtils.ServerGuid.ToString();
                            var success = await pingingClient.AttemptToPingHost(knockMessage, false);

                            if (!success)
                            {
                                Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                                LocaleUtils.UI_ERROR_CONNECTING.Localized(),
                                LocaleUtils.UI_UNABLE_TO_CONNECT.Localized(),
                                ErrorScreen.EButtonType.OkButton, 10f);
                                JoinInProgress = false;
                                return;
                            }
                        }
                        else
                        {
                            Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                                LocaleUtils.UI_ERROR_CONNECTING.Localized(),
                                LocaleUtils.UI_PINGER_START_FAIL.Localized(),
                                ErrorScreen.EButtonType.OkButton, 10f);
                            JoinInProgress = false;
                            return;
                        }

                        if (FikaBackendUtils.JoinMatch(FikaBackendUtils.Profile.Id, information.MatchId, out var result, out var errorMessage))
                        {
                            FikaBackendUtils.GroupId = result.ServerId;
                            FikaBackendUtils.ClientType = EClientType.Client;

                            AddPlayerRequest data = new(FikaBackendUtils.GroupId, FikaBackendUtils.Profile.Id, FikaBackendUtils.IsSpectator);
                            FikaRequestHandler.UpdateAddPlayer(data);

                            RaidSettings raidSettings = new()
                            {
                                Side = information.Side,
                                PlayersSpawnPlace = default,
                                MetabolismDisabled = default,
                                BotSettings = default,
                                WavesSettings = default,
                                TimeAndWeatherSettings = default,
                                SelectedDateTime = information.Time,
                                SelectedLocation = location,
                                isInTransition = false,
                                RaidMode = ERaidMode.Local,
                                IsPveOffline = true,
                                OnlinePveRaid = false
                            };

                            var tarkovAppTraverse = Traverse.Create(tarkovApplication);
                            tarkovAppTraverse.Field<RaidSettings>("_raidSettings").Value = raidSettings;
                            tarkovAppTraverse.Field<MainMenuControllerClass>("mainMenuControllerClass").Value.MatchmakerPlayerControllerClass.MatchingStartTime = EFTDateTimeClass.Now;

                            try
                            {
                                await tarkovApplication.method_41(raidSettings.TimeAndWeatherSettings);
                            }
                            catch (Exception ex)
                            {
                                Singleton<PreloaderUI>.Instance.ShowErrorScreen("ERROR JOINING",
                                    $"Exception caught during raid init: {ex.Message}", Application.Quit);
                                JoinInProgress = false;
                            }
                        }
                        else
                        {
                            Singleton<PreloaderUI>.Instance.ShowErrorScreen("ERROR JOINING", errorMessage, null);
                        }

                        JoinInProgress = false;
                    });
                }
            }
            newPlayer.SetActive(true);
            _players.Add(newPlayer);
        }
    }

    public void UpdatePresence(EFikaPlayerPresence presence)
    {
        // Prevent spamming going back and forth to the main menu causing server lag for no reason
        if ((DateTime.Now - _lastSet).TotalSeconds < _minSecondsToWait)
        {
            return;
        }

        _lastSet = DateTime.Now;
        FikaSetPresence data = new(presence);
        FikaRequestHandler.SetPresence(data);
    }
}
