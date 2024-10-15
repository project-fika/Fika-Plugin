using Comfort.Common;
using EFT.UI;
using Fika.Core.Bundles;
using Fika.Core.Networking.Http;
using Fika.Core.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Fika.Core.UI.FikaUIUtils;

namespace Fika.Core.UI.Custom
{
	public class MainMenuUIScript : MonoBehaviour
	{
		private Coroutine queryRoutine;
		private MainMenuUI mainMenuUI;
		private GameObject playerTemplate;
		private List<GameObject> players;
		private DateTime lastRefresh;

		private void Start()
		{
			players = [];
			lastRefresh = DateTime.Now;
			CreateMainMenuUI();
		}

		private void OnEnable()
		{
			queryRoutine = StartCoroutine(QueryPlayers());
		}

		private void OnDisable()
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

			this.mainMenuUI.RefreshButton.onClick.AddListener(ManualRefresh);
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

		private void SetupPlayers(ref FikaPlayerPresence[] responses)
		{
			foreach (FikaPlayerPresence presence in responses)
			{
				GameObject newPlayer = GameObject.Instantiate(playerTemplate, playerTemplate.transform.parent);
				MainMenuUIPlayer mainMenuUIPlayer = newPlayer.GetComponent<MainMenuUIPlayer>();
				mainMenuUIPlayer.SetStatus(presence.Nickname, presence.Level, presence.InRaid);
				if (presence.InRaid && presence.RaidInformation.HasValue)
				{
					RaidInformation information = presence.RaidInformation.Value;
					string side = information.Side == EFT.ESideType.Pmc ? "PMC" : "Scav";
					HoverTooltipArea tooltip = newPlayer.AddComponent<HoverTooltipArea>();
					tooltip.enabled = true;
					tooltip.SetMessageText($"Playing as a {side} on {ColorUtils.ColorizeText(Colors.BLUE, information.Location.Localized())}");
				}
				newPlayer.SetActive(true);
				players.Add(newPlayer);
			}
		}
	}
}
