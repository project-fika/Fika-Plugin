using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Coop.Patches;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Http;
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
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Custom
{
	public class MatchMakerUIScript : MonoBehaviour
	{
		public DefaultUIButton AcceptButton
		{
			get
			{
				return acceptButton;
			}
		}

		private MatchMakerUI fikaMatchMakerUi;
		private LobbyEntry[] matches;
		private readonly List<GameObject> matchesListObjects = [];
		private bool stopQuery = false;
		private GameObject newBackButton;
		private string profileId;
		private float lastRefreshed;
		private bool _started;
		private Coroutine serverQueryRoutine;
		private float loadingTextTick = 0f;

		internal RaidSettings raidSettings;
		internal DefaultUIButton backButton;
		internal DefaultUIButton acceptButton;

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
			DestroyThis();
		}

		protected void Start()
		{
			profileId = FikaBackendUtils.Profile.ProfileId;
			CreateMatchMakerUI();
			serverQueryRoutine = StartCoroutine(ServerQuery());
			_started = true;
		}

		protected void Update()
		{
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
				string text = fikaMatchMakerUi.LoadingAnimationText.text;
				TextMeshProUGUI tmpText = fikaMatchMakerUi.LoadingAnimationText;

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

			Destroy(fikaMatchMakerUi);
			Destroy(this);
		}

		protected void OnDestroy()
		{
			stopQuery = true;
			if (newBackButton != null)
			{
				Destroy(newBackButton);
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

			// Ensure the IsSpectator field is reset every time the matchmaker UI is created
			FikaBackendUtils.IsSpectator = false;

			fikaMatchMakerUi.SpectatorToggle.isOn = false;
			fikaMatchMakerUi.SpectatorToggle.onValueChanged.AddListener((arg) =>
			{
				FikaBackendUtils.IsSpectator = !FikaBackendUtils.IsSpectator;
				Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuCheckBox);
			});

			fikaMatchMakerUi.LoadingAnimationText.text = "";

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

				HoverTooltipArea dediTooltipArea = fikaMatchMakerUi.DedicatedToggle.GetOrAddComponent<HoverTooltipArea>();
				dediTooltipArea.enabled = true;
				dediTooltipArea.SetMessageText(LocaleUtils.UI_NO_DEDICATED_CLIENTS.Localized());
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
							Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(LocaleUtils.UI_ERROR_FORCE_IP_HEADER.Localized(),
								string.Format(LocaleUtils.UI_ERROR_FORCE_IP.Localized(), ip),
								ErrorScreen.EButtonType.OkButton, 10f, null, null);

							ToggleLoading(false);
							return;
						}
					}

					if (FikaPlugin.ForceBindIP.Value != "Disabled")
					{
						if (!IPAddress.TryParse(FikaPlugin.ForceBindIP.Value, out _))
						{
							Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(LocaleUtils.UI_ERROR_BIND_IP_HEADER.Localized(),
								string.Format(LocaleUtils.UI_ERROR_BIND_IP.Localized(), FikaPlugin.ForceBindIP.Value),
								ErrorScreen.EButtonType.OkButton, 10f, null, null);

							ToggleLoading(false);
							return;
						}
					}

					FikaBackendUtils.HostExpectedNumberOfPlayers = int.Parse(fikaMatchMakerUi.PlayerAmountText.text);
					await FikaBackendUtils.CreateMatch(FikaBackendUtils.Profile.ProfileId, FikaBackendUtils.PMCName, raidSettings);
					acceptButton.OnClick.Invoke();
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
						PreloaderUI.Instance.ShowErrorScreen(LocaleUtils.UI_DEDICATED_ERROR.Localized(), response.Error);

						ToggleLoading(false);
					}
					else
					{
						NotificationManagerClass.DisplaySingletonWarningNotification(LocaleUtils.STARTING_RAID_ON_DEDICATED.Localized());
					}
				}
			});

			fikaMatchMakerUi.RefreshButton.onClick.AddListener(ManualRefresh);

			HoverTooltipArea tooltipArea = fikaMatchMakerUi.RefreshButton.GetOrAddComponent<HoverTooltipArea>();
			tooltipArea.enabled = true;
			tooltipArea.SetMessageText(LocaleUtils.UI_REFRESH_RAIDS.Localized());

			acceptButton.gameObject.SetActive(false);
			acceptButton.enabled = false;
			acceptButton.Interactable = false;

			newBackButton = Instantiate(backButton.gameObject, backButton.transform.parent);
			UnityEngine.Events.UnityEvent newEvent = new();
			newEvent.AddListener(() =>
			{
				backButton.OnClick.Invoke();
			});
			DefaultUIButton newButtonComponent = newBackButton.GetComponent<DefaultUIButton>();
			Traverse.Create(newButtonComponent).Field("OnClick").SetValue(newEvent);

			if (!newBackButton.active)
			{
				newBackButton.SetActive(true);
			}

			backButton.gameObject.SetActive(false);
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
				return;
			}

			serverQueryRoutine = StartCoroutine(ServerQuery());
		}

		private void AutoRefresh()
		{
			matches = FikaRequestHandler.LocationRaids(raidSettings);

			lastRefreshed = Time.time;

			RefreshUI();
		}

		private void ManualRefresh()
		{
			Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
			matches = FikaRequestHandler.LocationRaids(raidSettings);

			lastRefreshed = Time.time;

			RefreshUI();
		}

		public static IEnumerator JoinMatch(string profileId, string serverId, Button button, Action<bool> callback, bool reconnect)
		{
			if (button != null)
			{
				button.enabled = false;
			}

			FikaBackendUtils.IsReconnect = reconnect;

			NotificationManagerClass.DisplayMessageNotification(LocaleUtils.CONNECTING_TO_SESSION.Localized(), iconType: EFT.Communications.ENotificationIconType.EntryPoint);

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
					LocaleUtils.UI_ERROR_CONNECTING.Localized(),
					LocaleUtils.UI_UNABLE_TO_CONNECT.Localized(),
					ErrorScreen.EButtonType.OkButton, 10f, null, null);

					FikaPlugin.Instance.FikaLogger.LogError("Unable to connect to the session!");

					if (button != null)
					{
						button.enabled = true;
					}
					callback.Invoke(false);
					yield break;
				}
			}
			else
			{
				Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen(
					LocaleUtils.UI_ERROR_CONNECTING.Localized(),
					LocaleUtils.UI_PINGER_START_FAIL.Localized(),
					ErrorScreen.EButtonType.OkButton, 10f, null, null);
				callback.Invoke(false);
				yield break;
			}

			if (FikaBackendUtils.JoinMatch(profileId, serverId, out CreateMatch result, out string errorMessage))
			{
				FikaBackendUtils.GroupId = result.ServerId;
				FikaBackendUtils.MatchingType = EMatchmakerType.GroupPlayer;
				FikaBackendUtils.HostExpectedNumberOfPlayers = result.ExpectedNumberOfPlayers;

				AddPlayerRequest data = new(FikaBackendUtils.GroupId, profileId, FikaBackendUtils.IsSpectator);
				FikaRequestHandler.UpdateAddPlayer(data);

				if (FikaBackendUtils.IsHostNatPunch)
				{
					pingingClient.StartKeepAliveRoutine();
				}
				else
				{
					NetManagerUtils.DestroyPingingClient();
				}

				callback?.Invoke(true);
			}
			else
			{
				NetManagerUtils.DestroyPingingClient();

				Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("ERROR JOINING", errorMessage,
					ErrorScreen.EButtonType.OkButton, 15, null, null);

				callback?.Invoke(false);
			}
		}

		private void RefreshUI()
		{
			if (matches == null)
			{
				// not initialized
				return;
			}

			if (matchesListObjects != null)
			{
				// cleanup old objects
				foreach (GameObject match in matchesListObjects)
				{
					Destroy(match);
				}
			}

			// create lobby listings
			for (int i = 0; i < matches.Length; ++i)
			{
				LobbyEntry entry = matches[i];

				if (entry.ServerId == profileId)
				{
					continue;
				}

				// server object
				GameObject server = Instantiate(fikaMatchMakerUi.RaidGroupDefaultToClone, fikaMatchMakerUi.RaidGroupDefaultToClone.transform.parent);
				server.SetActive(true);
				matchesListObjects.Add(server);

				server.name = entry.ServerId;

				bool localPlayerInRaid = false;
				bool localPlayerDead = false;
				foreach (KeyValuePair<string, bool> player in entry.Players)
				{
					if (player.Key == profileId)
					{
						localPlayerInRaid = true;
						localPlayerDead = player.Value;
					}
				}

				bool isDedicated = entry.HostUsername.StartsWith("dedicated_");

				// player label
				GameObject playerLabel = GameObject.Find("PlayerLabel");
				playerLabel.name = "PlayerLabel" + i;
				string sessionName = isDedicated ? "Dedicated" : entry.HostUsername;
				playerLabel.GetComponentInChildren<TextMeshProUGUI>().text = sessionName;

				// players count label
				GameObject playerCountLabel = GameObject.Find("PlayerCountLabel");
				playerCountLabel.name = "PlayerCountLabel" + i;
				int playerCount = isDedicated ? entry.PlayerCount - 1 : entry.PlayerCount;
				playerCountLabel.GetComponentInChildren<TextMeshProUGUI>().text = playerCount.ToString();

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
					FikaBackendUtils.HostLocationId = entry.Location;
					ToggleLoading(true);
					StartCoroutine(JoinMatch(profileId, server.name, button, (bool success) =>
					{
						if (success)
						{
							acceptButton.OnClick.Invoke();
							return;
						}
						ToggleLoading(false);
					}, localPlayerInRaid));
				});

				HoverTooltipArea tooltipArea;
				Image image = server.GetComponent<Image>();

				if (raidSettings.LocationId != entry.Location && !(raidSettings.LocationId.ToLower().StartsWith("sandbox") && entry.Location.ToLower().StartsWith("sandbox")))
				{
					button.enabled = false;
					if (image != null)
					{
						image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
					}

					tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
					tooltipArea.enabled = true;
					tooltipArea.SetMessageText(string.Format(LocaleUtils.UI_CANNOT_JOIN_RAID_OTHER_MAP.Localized(),
						ColorizeText(EColor.BLUE, entry.Location.Localized())));

					continue;
				}

				if (raidSettings.SelectedDateTime != entry.Time)
				{
					button.enabled = false;
					if (image != null)
					{
						image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
					}

					tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
					tooltipArea.enabled = true;
					tooltipArea.SetMessageText(LocaleUtils.UI_CANNOT_JOIN_RAID_OTHER_TIME.Localized());

					continue;
				}

				if (raidSettings.Side != entry.Side)
				{
					string errorText = "ERROR";
					if (raidSettings.Side == ESideType.Pmc)
					{
						errorText = LocaleUtils.UI_CANNOT_JOIN_RAID_SCAV_AS_PMC.Localized();
					}
					else if (raidSettings.Side == ESideType.Savage)
					{
						errorText = LocaleUtils.UI_CANNOT_JOIN_RAID_PMC_AS_SCAV.Localized();
					}

					button.enabled = false;
					if (image != null)
					{
						image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
					}

					tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
					tooltipArea.enabled = true;
					tooltipArea.SetMessageText(errorText);

					continue;
				}

				switch (entry.Status)
				{
					case LobbyEntry.ELobbyStatus.LOADING:
						{
							button.enabled = false;
							if (image != null)
							{
								image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
							}

							tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
							tooltipArea.enabled = true;
							tooltipArea.SetMessageText(LocaleUtils.UI_HOST_STILL_LOADING.Localized());
						}
						break;
					case LobbyEntry.ELobbyStatus.IN_GAME:
						if (!localPlayerInRaid)
						{
							button.enabled = false;
							if (image != null)
							{
								image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
							}

							tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
							tooltipArea.enabled = true;
							tooltipArea.SetMessageText(LocaleUtils.UI_RAID_IN_PROGRESS.Localized());
						}
						else
						{
							if (!localPlayerDead)
							{
								tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
								tooltipArea.enabled = true;
								tooltipArea.SetMessageText(LocaleUtils.UI_REJOIN_RAID.Localized());
							}
							else
							{
								button.enabled = false;
								if (image != null)
								{
									image.color = new(0.5f, image.color.g / 2, image.color.b / 2, 0.75f);
								}

								tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
								tooltipArea.enabled = true;
								tooltipArea.SetMessageText(LocaleUtils.UI_CANNOT_REJOIN_RAID_DIED.Localized());
							}
						}
						break;
					case LobbyEntry.ELobbyStatus.COMPLETE:
						tooltipArea = joinButton.GetOrAddComponent<HoverTooltipArea>();
						tooltipArea.enabled = true;
						tooltipArea.SetMessageText(LocaleUtils.UI_JOIN_RAID.Localized());
						break;
					default:
						break;
				}
			}
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
