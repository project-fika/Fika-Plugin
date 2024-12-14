using Comfort.Common;
using EFT;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Models.Presence;
using Fika.Core.Utils;
using JsonType;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Custom
{
	public class MainMenuUIScript : MonoBehaviour
	{
		public static bool Exist
		{
			get
			{
				return instance != null;
			}
		}

		public static MainMenuUIScript Instance
		{
			get
			{
				return instance;
			}
		}

		private static MainMenuUIScript instance;

		private Coroutine queryRoutine;
		private MainMenuUI mainMenuUI;
		private GameObject playerTemplate;
		private GameObject userInterface;
		private List<GameObject> players;
		private DateTime lastRefresh;
		private DateTime lastSet;
		private int minSecondsToWait;
		private RectTransform transformToScale;
		private GInterface186<RaidSettings> backendSession;

		private DateTime BackendTime
		{
			get
			{
				if (backendSession == null)
				{
					return DateTime.MinValue;
				}
				return backendSession.GetCurrentLocationTime;
			}
		}

		protected void Start()
		{
			instance = this;
			minSecondsToWait = 2;
			players = [];
			lastRefresh = DateTime.Now;
			lastSet = DateTime.Now;
			if (TarkovApplication.Exist(out TarkovApplication tarkovApplication))
			{
				if (tarkovApplication.Session != null)
				{
					backendSession = tarkovApplication.Session;
				}
			}
			CreateMainMenuUI();
		}

		protected void OnEnable()
		{
			if (!FikaPlugin.EnableOnlinePlayers.Value)
			{
				if (userInterface != null)
				{
					userInterface.SetActive(false);
				}
				return;
			}

			if (userInterface != null)
			{
				userInterface.SetActive(true);
			}

			if (transformToScale != null)
			{
				float scale = FikaPlugin.OnlinePlayersScale.Value;
				transformToScale.localScale = new(scale, scale, scale);
			}
			queryRoutine = StartCoroutine(QueryPlayers());
		}

		protected void OnDisable()
		{
			if (queryRoutine != null)
			{
				StopCoroutine(queryRoutine);
			}
		}

		private void CreateMainMenuUI()
		{
			GameObject mainMenuUIPrefab = InternalBundleLoader.Instance.GetAssetBundle("mainmenuui").LoadAsset<GameObject>("MainMenuUI");
			GameObject mainMenuUI = GameObject.Instantiate(mainMenuUIPrefab);
			this.mainMenuUI = mainMenuUI.GetComponent<MainMenuUI>();
			playerTemplate = this.mainMenuUI.PlayerTemplate;
			playerTemplate.SetActive(false);
			Transform newParent = Singleton<CommonUI>.Instance.MenuScreen.gameObject.transform;
			mainMenuUI.transform.SetParent(newParent);
			gameObject.transform.SetParent(newParent);

			Transform mainMenuUITransform = this.mainMenuUI.gameObject.transform;
			GameObject objectToAttach = mainMenuUITransform.GetChild(0).GetChild(0).gameObject;
			userInterface = objectToAttach;
			transformToScale = objectToAttach.RectTransform();
			float scale = FikaPlugin.OnlinePlayersScale.Value;
			transformToScale.localScale = new(scale, scale, scale);
			objectToAttach.AddComponent<UIDragComponent>().Init(transformToScale, true);
			this.mainMenuUI.RefreshButton.onClick.AddListener(ManualRefresh);

			gameObject.SetActive(false);
			this.WaitFrames(3, null);
			gameObject.SetActive(true);
		}

		private void ManualRefresh()
		{
			if ((DateTime.Now - lastRefresh).TotalSeconds >= 5)
			{
				lastRefresh = DateTime.Now;
				ClearAndQueryPlayers();
			}
		}

		private IEnumerator QueryPlayers()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				ClearAndQueryPlayers();
				yield return new WaitForSeconds(10);
			}
		}

		private void ClearAndQueryPlayers()
		{
			foreach (GameObject item in players)
			{
				GameObject.Destroy(item);
			}
			players.Clear();

			FikaPlayerPresence[] response = FikaRequestHandler.GetPlayerPresences();
			mainMenuUI.UpdateLabel(response.Length);
			SetupPlayers(ref response);
		}

		private string ConvertToTime(EDateTime dateTime)
		{
			DateTime backendTime = BackendTime;
			if (backendTime == DateTime.MinValue)
			{
				return "ERROR";
			}
			return dateTime == EDateTime.CURR ? backendTime.ToString("HH:mm:ss") : backendTime.AddHours(-12).ToString("HH:mm:ss");
		}

		private void SetupPlayers(ref FikaPlayerPresence[] responses)
		{
			foreach (FikaPlayerPresence presence in responses)
			{
				GameObject newPlayer = GameObject.Instantiate(playerTemplate, playerTemplate.transform.parent);
				MainMenuUIPlayer mainMenuUIPlayer = newPlayer.GetComponent<MainMenuUIPlayer>();
				mainMenuUIPlayer.SetActivity(presence.Nickname, presence.Level, presence.Activity);
				if (presence.Activity is EFikaPlayerPresence.IN_RAID && presence.RaidInformation.HasValue)
				{
					RaidInformation information = presence.RaidInformation.Value;
					string side = information.Side == ESideType.Pmc ? "RaidSidePmc".Localized() : "RaidSideScav".Localized();
					string time = ConvertToTime(information.Time);
					HoverTooltipArea tooltip = newPlayer.AddComponent<HoverTooltipArea>();
					tooltip.enabled = true;
					tooltip.SetMessageText(string.Format(LocaleUtils.UI_MMUI_RAID_DETAILS.Localized(), side,
						ColorizeText(EColor.BLUE, information.Location.Localized()),
						BoldText(time)));
				}
				newPlayer.SetActive(true);
				players.Add(newPlayer);
			}
		}

		public void UpdatePresence(EFikaPlayerPresence presence)
		{
			// Prevent spamming going back and forth to the main menu causing server lag for no reason
			if ((DateTime.Now - lastSet).TotalSeconds < minSecondsToWait)
			{
				return;
			}

			lastSet = DateTime.Now;
			FikaSetPresence data = new(presence);
			FikaRequestHandler.SetPresence(data);
		}
	}
}
