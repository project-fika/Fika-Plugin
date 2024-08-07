using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Patches.Overrides;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.Networking.Models.Dedicated;
using Fika.Core.Networking.Websocket;
using Fika.Core.UI.Models;
using Fika.Core.Utils;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fika.Core.UI.Custom
{
    public class MatchMakerUIScript : MonoBehaviour
    {
        public MatchMakerUI fikaMatchMakerUi;
        public RaidSettings RaidSettings { get; set; }
        private LobbyEntry[] Matches { get; set; }
        private List<GameObject> MatchesListObjects { get; set; } = [];
        private bool stopQuery = false;

        public DefaultUIButton BackButton { get; internal set; }
        public DefaultUIButton AcceptButton { get; internal set; }
        public GameObject NewBackButton { get; internal set; }

        private string ProfileId => FikaBackendUtils.Profile.ProfileId;
        private float lastRefreshed;

        private bool _started;
        private Coroutine serverQueryRoutine;
        private float loadingTextTick = 0f;

        protected void OnEnable()
        {
            if (_started)
            {
                stopQuery = false;
                if (serverQueryRoutine == null)
                {
                    serverQueryRoutine = StartCoroutine(ServerQuery());
                }
            }
        }

        protected void OnDisable()
        {
            stopQuery = true;
            if (serverQueryRoutine != null)
            {
                StopCoroutine(serverQueryRoutine);
                serverQueryRoutine = null;
            }
        }

        protected void Start()
        {
            CreateMatchMakerUI();
            serverQueryRoutine = StartCoroutine(ServerQuery());
            _started = true;
        }

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DestroyThis();
            }

            if (stopQuery)
            {
                if (serverQueryRoutine != null)
                {
                    StopCoroutine(serverQueryRoutine);
                    serverQueryRoutine = null;
                }
            }

            if (fikaMatchMakerUi.LoadingScreen.activeSelf)
            {
                fikaMatchMakerUi.LoadingImage.transform.Rotate(0, 0, 3f);
                string text = fikaMatchMakerUi.LoadingText.text;
                TextMeshProUGUI tmpText = fikaMatchMakerUi.LoadingText;

                loadingTextTick++;

                if (loadingTextTick > 30)
                {
                    loadingTextTick = 0;

                    text += ".";
                    if (text == "....")
                    {
                        text = ".";
                    }
                    tmpText.text = text;
                }
            }
        }

        private void DestroyThis()
        {
            stopQuery = true;
            if (serverQueryRoutine != null)
            {
                StopCoroutine(serverQueryRoutine);
                serverQueryRoutine = null;
            }

            Destroy(gameObject);
            Destroy(fikaMatchMakerUi);
            Destroy(this);
        }

        protected void OnDestroy()
        {
            stopQuery = true;
            if (NewBackButton != null)
            {
                Destroy(NewBackButton);
            }
        }

        private void CreateMatchMakerUI()
        {
            GetDedicatedStatusResponse response = FikaRequestHandler.GetDedicatedStatus();

            GameObject matchMakerUiPrefab = InternalBundleLoader.Instance.GetAssetBundle("newmatchmakerui").LoadAsset<GameObject>("NewMatchMakerUI");
            GameObject uiGameObj = Instantiate(matchMakerUiPrefab);
            fikaMatchMakerUi = uiGameObj.GetComponent<MatchMakerUI>();
            fikaMatchMakerUi.transform.parent = transform;
            fikaMatchMakerUi.GetComponent<Canvas>().sortingOrder = 100; // Might wanna do this directly in the SDK later

            if (fikaMatchMakerUi.RaidGroupDefaultToClone.active)
            {
                fikaMatchMakerUi.RaidGroupDefaultToClone.SetActive(false);
            }

            if (fikaMatchMakerUi.PlayerAmountSelection.active)
            {
                fikaMatchMakerUi.PlayerAmountSelection.SetActive(false);
            }

            fikaMatchMakerUi.LoadingText.text = "";

            fikaMatchMakerUi.DedicatedToggle.isOn = false;
            fikaMatchMakerUi.DedicatedToggle.onValueChanged.AddListener((arg) =>
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuCheckBox);
            });

            if (!response.Available)
            {
                fikaMatchMakerUi.DedicatedToggle.interactable = false;
                TextMeshProUGUI dedicatedText = fikaMatchMakerUi.DedicatedToggle.gameObject.GetComponentInChildren<TextMeshProUGUI>();
                if (dedicatedText != null)
                {
                    dedicatedText.color = new(1f, 1f, 1f, 0.5f);
                }

                TooltipTextGetter dediTooltipTextGetter = new()
                {
                    TooltipText = $"No dedicated clients are available."
                };

                HoverTooltipArea dediTooltipArea = fikaMatchMakerUi.DedicatedToggle.GetOrAddComponent<HoverTooltipArea>();
                dediTooltipArea.enabled = true;
                dediTooltipArea.SetMessageText(new Func<string>(dediTooltipTextGetter.GetText));
            }

            TMP_Text matchmakerUiHostRaidText = fikaMatchMakerUi.RaidGroupHostButton.GetComponentInChildren<TMP_Text>();
            fikaMatchMakerUi.RaidGroupHostButton.onClick.AddListener(() =>
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
                if (!fikaMatchMakerUi.PlayerAmountSelection.active)
                {
                    fikaMatchMakerUi.PlayerAmountSelection.SetActive(true);
                }
                else
                {
                    fikaMatchMakerUi.PlayerAmountSelection.SetActive(false);
                }
            });

            fikaMatchMakerUi.CloseButton.onClick.AddListener(() =>
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
                if (fikaMatchMakerUi.PlayerAmountSelection.active)
                {
                    fikaMatchMakerUi.PlayerAmountSelection.SetActive(false);
                }
            });

            //fikaMatchMakerUi.DedicatedToggle.isOn = false;
            fikaMatchMakerUi.DedicatedToggle.onValueChanged.AddListener((arg) =>
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuCheckBox);
            });

            fikaMatchMakerUi.StartButton.onClick.AddListener(async () =>
            {
                ToggleLoading(true);

                TarkovApplication tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;
                ISession session = tarkovApplication.Session;

                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);

                if (!fikaMatchMakerUi.DedicatedToggle.isOn)
                {
                    if (FikaPlugin.ForceIP.Value != "")
                    {
                        // We need to handle DNS entries as well
                        string ip = FikaPlugin.ForceIP.Value;
                        try
                        {
                            IPAddress[] dnsAddress = Dns.GetHostAddresses(FikaPlugin.ForceIP.Value);
                            if (dnsAddress.Length > 0)
                            {
                                ip = dnsAddress[0].ToString();
                            }
                        }
                        catch
                        {

                        }

                        if (!IPAddress.TryParse(ip, out _))
                        {
                            Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("ERROR FORCING IP",
                                $"'{ip}' is not a valid IP address to connect to! Check your 'Force IP' setting.",
                                ErrorScreen.EButtonType.OkButton, 10f, null, null);

                            ToggleLoading(false);
                            return;
                        }
                    }

                    if (FikaPlugin.ForceBindIP.Value != "Disabled")
                    {
                        if (!IPAddress.TryParse(FikaPlugin.ForceBindIP.Value, out _))
                        {
                            Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("ERROR BINDING",
                                $"'{FikaPlugin.ForceBindIP.Value}' is not a valid IP address to bind to! Check your 'Force Bind IP' setting.",
                                ErrorScreen.EButtonType.OkButton, 10f, null, null);

                            ToggleLoading(false);
                            return;
                        }
                    }

                    FikaBackendUtils.HostExpectedNumberOfPlayers = int.Parse(fikaMatchMakerUi.PlayerAmountText.text);
                    await FikaBackendUtils.CreateMatch(FikaBackendUtils.Profile.ProfileId, FikaBackendUtils.PMCName, RaidSettings);
                    AcceptButton.OnClick.Invoke();
                    DestroyThis();
                }
                else
                {
                    ToggleLoading(true);

                    FikaPlugin.DedicatedRaidWebSocket ??= new DedicatedRaidWebSocketClient();

                    if (!FikaPlugin.DedicatedRaidWebSocket.Connected)
                    {
                        FikaPlugin.DedicatedRaidWebSocket.Connect();
                    }

                    RaidSettings raidSettings = Traverse.Create(tarkovApplication).Field<RaidSettings>("_raidSettings").Value;

                    StartDedicatedRequest request = new()
                    {
                        ExpectedNumPlayers = int.Parse(fikaMatchMakerUi.PlayerAmountText.text),
                        Time = raidSettings.SelectedDateTime,
                        LocationId = raidSettings.SelectedLocation._Id,
                        SpawnPlace = raidSettings.PlayersSpawnPlace,
                        MetabolismDisabled = raidSettings.MetabolismDisabled,
                        BotSettings = raidSettings.BotSettings,
                        Side = raidSettings.Side,
                        TimeAndWeatherSettings = raidSettings.TimeAndWeatherSettings,
                        WavesSettings = raidSettings.WavesSettings,
                        CustomWeather = OfflineRaidSettingsMenuPatch_Override.UseCustomWeather
                    };

                    StartDedicatedResponse response = await FikaRequestHandler.StartDedicated(request);

                    if (!string.IsNullOrEmpty(response.Error))
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", response.Error);

                        ToggleLoading(false);
                    }
                    else
                    {
                        NotificationManagerClass.DisplaySingletonWarningNotification("Starting raid on dedicated client... please wait.");
                    }
                }
            });

            fikaMatchMakerUi.RefreshButton.onClick.AddListener(ManualRefresh);

            TooltipTextGetter tooltipTextGetter = new()
            {
                TooltipText = "Refresh list of active raids"
            };

            HoverTooltipArea tooltipArea = fikaMatchMakerUi.RefreshButton.GetOrAddComponent<HoverTooltipArea>();
            tooltipArea.enabled = true;
            tooltipArea.SetMessageText(new Func<string>(tooltipTextGetter.GetText));

            AcceptButton.gameObject.SetActive(false);
            AcceptButton.enabled = false;
            AcceptButton.Interactable = false;

            NewBackButton = Instantiate(BackButton.gameObject, BackButton.transform.parent);
            UnityEngine.Events.UnityEvent newEvent = new();
            newEvent.AddListener(() =>
            {
                DestroyThis();
                BackButton.OnClick.Invoke();
            });
            DefaultUIButton newButtonComponent = NewBackButton.GetComponent<DefaultUIButton>();
            Traverse.Create(newButtonComponent).Field("OnClick").SetValue(newEvent);

            if (!NewBackButton.active)
            {
                NewBackButton.SetActive(true);
            }

            BackButton.gameObject.SetActive(false);
        }

        private void ToggleLoading(bool enabled)
        {
            fikaMatchMakerUi.RaidGroupHostButton.interactable = !enabled;
            fikaMatchMakerUi.StartButton.interactable = !enabled;
            fikaMatchMakerUi.ServerBrowserPanel.SetActive(!enabled);

            fikaMatchMakerUi.LoadingScreen.SetActive(enabled);

            if (enabled)
            {
                if (serverQueryRoutine != null)
                {
                    StopCoroutine(serverQueryRoutine);
                    serverQueryRoutine = null;
                }
            }
            else if (!enabled)
            {
                serverQueryRoutine = StartCoroutine(ServerQuery());
            }
        }

        private void AutoRefresh()
        {
            Matches = FikaRequestHandler.LocationRaids(RaidSettings);

            lastRefreshed = Time.time;

            RefreshUI();
        }

        private void ManualRefresh()
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
            Matches = FikaRequestHandler.LocationRaids(RaidSettings);

            lastRefreshed = Time.time;

            RefreshUI();
        }

        public static IEnumerator JoinMatch(string profileId, string serverId, Button button, Action successCallback)
        {
            if (button != null)
            {
                button.enabled = false;
            }

            NotificationManagerClass.DisplayMessageNotification("Connecting to session...", iconType: EFT.Communications.ENotificationIconType.EntryPoint);

            NetManagerUtils.CreatePingingClient();

            FikaPingingClient pingingClient = Singleton<FikaPingingClient>.Instance;

            if (pingingClient.Init(serverId))
            {
                int attempts = 0;
                bool success;

                FikaPlugin.Instance.FikaLogger.LogInfo("Attempting to connect to host session...");

                do
                {
                    attempts++;

                    pingingClient.PingEndPoint("fika.hello");
                    pingingClient.NetClient.PollEvents();
                    success = pingingClient.Received;

                    yield return new WaitForSeconds(0.1f);
                } while (!success && attempts < 50);

                if (!success)
                {
                    Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                    "ERROR CONNECTING",
                    "Unable to connect to the server. Make sure that all ports are open and that all settings are configured correctly.",
                    ErrorScreen.EButtonType.OkButton, 10f, null, null);

                    FikaPlugin.Instance.FikaLogger.LogError("Unable to connect to the session!");

                    if (button != null)
                    {
                        button.enabled = true;
                    }
                    yield break;
                }
            }
            else
            {
                Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                    "ERROR CONNECTING",
                    "Could not start the FikaPinger!",
                    ErrorScreen.EButtonType.OkButton, 10f, null, null);
                yield break;
            }

            if (FikaBackendUtils.JoinMatch(profileId, serverId, out CreateMatch result, out string errorMessage))
            {
                FikaBackendUtils.SetGroupId(result.ServerId);
                FikaBackendUtils.MatchingType = EMatchmakerType.GroupPlayer;
                FikaBackendUtils.HostExpectedNumberOfPlayers = result.ExpectedNumberOfPlayers;

                AddPlayerRequest data = new(FikaBackendUtils.GetGroupId(), profileId);
                FikaRequestHandler.UpdateAddPlayer(data);

                if (FikaBackendUtils.IsHostNatPunch)
                {
                    pingingClient.StartKeepAliveRoutine();
                }
                else
                {
                    NetManagerUtils.DestroyPingingClient();
                }

                //DestroyThis();

                //AcceptButton.OnClick.Invoke();

                successCallback?.Invoke();
            }
            else
            {
                NetManagerUtils.DestroyPingingClient();

                Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("ERROR JOINING", errorMessage, ErrorScreen.EButtonType.OkButton, 15, null, null);
            }
        }

        private void RefreshUI()
        {
            if (Matches == null)
            {
                // not initialized
                return;
            }

            if (MatchesListObjects != null)
            {
                // cleanup old objects
                foreach (GameObject match in MatchesListObjects)
                {
                    Destroy(match);
                }
            }

            // create lobby listings
            for (int i = 0; i < Matches.Length; ++i)
            {
                LobbyEntry entry = Matches[i];

                // server object
                GameObject server = Instantiate(fikaMatchMakerUi.RaidGroupDefaultToClone, fikaMatchMakerUi.RaidGroupDefaultToClone.transform.parent);
                server.SetActive(true);
                MatchesListObjects.Add(server);

                server.name = entry.ServerId;

                // player label
                GameObject playerLabel = GameObject.Find("PlayerLabel");
                playerLabel.name = "PlayerLabel" + i;
                playerLabel.GetComponentInChildren<TextMeshProUGUI>().text = entry.HostUsername;

                // players count label
                GameObject playerCountLabel = GameObject.Find("PlayerCountLabel");
                playerCountLabel.name = "PlayerCountLabel" + i;
                playerCountLabel.GetComponentInChildren<TextMeshProUGUI>().text = entry.PlayerCount.ToString();

                // player join button
                GameObject joinButton = GameObject.Find("JoinButton");
                joinButton.name = "JoinButton" + i;
                Button button = joinButton.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    if (fikaMatchMakerUi.PlayerAmountSelection.active)
                    {
                        fikaMatchMakerUi.PlayerAmountSelection.SetActive(false);
                    }

                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
                    //StartCoroutine(JoinMatch(ProfileId, server.name, button));
                    FikaBackendUtils.HostLocationId = entry.Location;
                    StartCoroutine(JoinMatch(ProfileId, server.name, button, () =>
                    {
                        DestroyThis();
                        AcceptButton.OnClick.Invoke();
                    }));
                });

                TooltipTextGetter tooltipTextGetter;
                HoverTooltipArea tooltipArea;
                Image image = server.GetComponent<Image>();

                if (RaidSettings.LocationId != entry.Location && !(RaidSettings.LocationId.ToLower().StartsWith("sandbox") && entry.Location.ToLower().StartsWith("sandbox")))
                {
                    tooltipTextGetter = new()
                    {
                        TooltipText = $"Cannot join a raid that is on another map.\nRaid map: {ColorUtils.ColorizeText(Colors.BLUE, entry.Location.Localized())}"
                    };

                    button.enabled = false;
                    if (image != null)
                    {
                        image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
                    }

                    tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
                    tooltipArea.enabled = true;
                    tooltipArea.SetMessageText(new Func<string>(tooltipTextGetter.GetText));

                    continue;
                }

                if (RaidSettings.SelectedDateTime != entry.Time)
                {
                    tooltipTextGetter = new()
                    {
                        TooltipText = $"Cannot join a raid that is on another time."
                    };

                    button.enabled = false;
                    if (image != null)
                    {
                        image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
                    }

                    tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
                    tooltipArea.enabled = true;
                    tooltipArea.SetMessageText(new Func<string>(tooltipTextGetter.GetText));

                    continue;
                }

                if (RaidSettings.Side != entry.Side)
                {
                    string errorText = "Cannot join a raid that is on another map.";
                    if (RaidSettings.Side == ESideType.Pmc)
                    {
                        errorText = "You cannot join a scav raid as a PMC.";
                    }
                    else if (RaidSettings.Side == ESideType.Savage)
                    {
                        errorText = "You cannot join a PMC raid as a scav.";
                    }

                    tooltipTextGetter = new()
                    {
                        TooltipText = errorText
                    };

                    button.enabled = false;
                    if (image != null)
                    {
                        image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
                    }

                    tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
                    tooltipArea.enabled = true;
                    tooltipArea.SetMessageText(new Func<string>(tooltipTextGetter.GetText));

                    continue;
                }

                switch (entry.Status)
                {
                    case LobbyEntry.ELobbyStatus.LOADING:
                        /*{
                            tooltipTextGetter = new()
                            {
                                TooltipText = "Host is still loading."
                            };

                            button.enabled = false;
                            if (image != null)
                            {
                                image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
                            }

                            tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
                            tooltipArea.enabled = true;
                            tooltipArea.SetMessageText(new Func<string>(tooltipTextGetter.GetText));
                        }
                        break;*/
                    case LobbyEntry.ELobbyStatus.IN_GAME:
                        /*tooltipTextGetter = new()
                        {
                            TooltipText = "Raid is already in progress."
                        };

                        button.enabled = false;
                        if (image != null)
                        {
                            image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
                        }

                        tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
                        tooltipArea.enabled = true;
                        tooltipArea.SetMessageText(new Func<string>(tooltipTextGetter.GetText));
                        break;*/
                    case LobbyEntry.ELobbyStatus.COMPLETE:
                        tooltipTextGetter = new()
                        {
                            TooltipText = "Click to join raid."
                        };

                        tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
                        tooltipArea.enabled = true;
                        tooltipArea.SetMessageText(new Func<string>(tooltipTextGetter.GetText));
                        break;
                    default:
                        break;
                }
            }
        }

        public class TooltipTextGetter
        {
            public string GetText()
            {
                return TooltipText;
            }
            public string TooltipText;
        }

        public IEnumerator ServerQuery()
        {
            while (!stopQuery)
            {
                AutoRefresh();

                while (Time.time < lastRefreshed + FikaPlugin.AutoRefreshRate.Value)
                {
                    yield return null;
                }
            }
        }
    }
}
