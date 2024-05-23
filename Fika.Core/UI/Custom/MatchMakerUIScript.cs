using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.UI.Models;
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
    internal class MatchMakerUIScript : MonoBehaviour
    {
        private MatchMakerUI fikaMatchMakerUi;
        public RaidSettings RaidSettings { get; set; }
        private LobbyEntry[] Matches { get; set; }
        private List<GameObject> MatchesListObjects { get; set; } = [];
        private bool StopQuery = false;

        public DefaultUIButton BackButton { get; internal set; }
        public DefaultUIButton AcceptButton { get; internal set; }
        public GameObject NewBackButton { get; internal set; }

        private string ProfileId => MatchmakerAcceptPatches.Profile.ProfileId;

        private float _lastRefreshed;

        protected void Start()
        {
            CreateMatchMakerUI();

            StartCoroutine(ServerQuery());
        }

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DestroyThis();
            }

            if (StopQuery)
            {
                StopCoroutine(ServerQuery());
            }
        }

        private void DestroyThis()
        {
            StopQuery = true;
            StopCoroutine(ServerQuery());

            Destroy(gameObject);
            Destroy(fikaMatchMakerUi);
            Destroy(this);
        }

        protected void OnDestroy()
        {
            StopQuery = true;
            if (NewBackButton != null)
                Destroy(NewBackButton);
        }

        private void CreateMatchMakerUI()
        {
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

            fikaMatchMakerUi.StartButton.onClick.AddListener(() =>
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
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
                        Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                    "ERROR FORCING IP",
                    $"'{ip}' is not a valid IP address to connect to! Check your 'Force IP' setting.",
                    ErrorScreen.EButtonType.OkButton, 10f, null, null);
                        return;
                    }
                }
                if (FikaPlugin.ForceBindIP.Value != "Disabled")
                {
                    if (!IPAddress.TryParse(FikaPlugin.ForceBindIP.Value, out _))
                    {
                        Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
                    "ERROR BINDING",
                    $"'{FikaPlugin.ForceBindIP.Value}' is not a valid IP address to bind to! Check your 'Force Bind IP' setting.",
                    ErrorScreen.EButtonType.OkButton, 10f, null, null);
                        return;
                    }
                }
                MatchmakerAcceptPatches.HostExpectedNumberOfPlayers = int.Parse(fikaMatchMakerUi.PlayerAmountText.text);
                MatchmakerAcceptPatches.CreateMatch(MatchmakerAcceptPatches.Profile.ProfileId, MatchmakerAcceptPatches.PMCName, RaidSettings);
                AcceptButton.OnClick.Invoke();
                DestroyThis();
            });

            fikaMatchMakerUi.RefreshButton.onClick.AddListener(ManualRefresh);

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
                NewBackButton.SetActive(true);

            BackButton.gameObject.SetActive(false);
        }

        private void ManualRefresh()
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
            Matches = FikaRequestHandler.LocationRaids(RaidSettings);

            _lastRefreshed = Time.time;

            RefreshUI();
        }

        private IEnumerator JoinMatch(string profileId, string serverId, Button button)
        {
            if (button != null)
            {
                button.enabled = false;
            }

            NotificationManagerClass.DisplayMessageNotification("Connecting to session...", iconType: EFT.Communications.ENotificationIconType.EntryPoint);

            FikaPingingClient pingingClient = new(serverId);
            if (pingingClient.Init())
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
                ConsoleScreen.Log("ERROR");
            }

            pingingClient.NetClient?.Stop();
            pingingClient = null;

            if (MatchmakerAcceptPatches.JoinMatch(profileId, serverId, out CreateMatch result, out string errorMessage))
            {
                MatchmakerAcceptPatches.SetGroupId(result.ServerId);
                MatchmakerAcceptPatches.SetTimestamp(result.Timestamp);
                MatchmakerAcceptPatches.MatchingType = EMatchmakerType.GroupPlayer;
                MatchmakerAcceptPatches.HostExpectedNumberOfPlayers = result.ExpectedNumberOfPlayers;

                DestroyThis();

                AcceptButton.OnClick.Invoke();
            }
            else
            {
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
                    StartCoroutine(JoinMatch(ProfileId, server.name, button));
                });

                TooltipTextGetter tooltipTextGetter;
                HoverTooltipArea tooltipArea;
                Image image = server.GetComponent<Image>();

                if (RaidSettings.LocationId != entry.Location)
                {
                    tooltipTextGetter = new()
                    {
                        TooltipText = "Cannot join a raid that is on another map."
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
                        TooltipText = "Cannot join a raid that is on another time."
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
                        {
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
                        break;
                    case LobbyEntry.ELobbyStatus.IN_GAME:
                        tooltipTextGetter = new()
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
                        break;
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
            while (!StopQuery)
            {
                ManualRefresh();

                while (Time.time < _lastRefreshed + FikaPlugin.AutoRefreshRate.Value)
                {
                    yield return null;
                }
            }
        }
    }
}
