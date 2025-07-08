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

        private static MainMenuUIScript _instance;

        private Coroutine _queryRoutine;
        private MainMenuUI _mainMenuUI;
        private GameObject _playerTemplate;
        private GameObject _userInterface;
        private List<GameObject> _players;
        private DateTime _lastRefresh;
        private DateTime _lastSet;
        private int _minSecondsToWait;
        private RectTransform _transformToScale;
        private GInterface207<RaidSettings> _backendSession;

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
            _minSecondsToWait = 2;
            _players = [];
            _lastRefresh = DateTime.Now;
            _lastSet = DateTime.Now;
            if (TarkovApplication.Exist(out TarkovApplication tarkovApplication))
            {
                if (tarkovApplication.Session != null)
                {
                    _backendSession = tarkovApplication.Session;
                }
            }
            CreateMainMenuUI();
        }

        protected void OnEnable()
        {
            if (!FikaPlugin.EnableOnlinePlayers.Value)
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
                float scale = FikaPlugin.OnlinePlayersScale.Value;
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
            GameObject mainMenuUIPrefab = InternalBundleLoader.Instance.GetFikaAsset(InternalBundleLoader.EFikaAsset.MainMenuUI);
            GameObject mainMenuUI = GameObject.Instantiate(mainMenuUIPrefab);
            _mainMenuUI = mainMenuUI.GetComponent<MainMenuUI>();
            _playerTemplate = _mainMenuUI.PlayerTemplate;
            Transform newParent = Singleton<CommonUI>.Instance.MenuScreen.gameObject.transform;
            mainMenuUI.transform.SetParent(newParent);
            gameObject.transform.SetParent(newParent);

            Transform mainMenuUITransform = _mainMenuUI.gameObject.transform;
            GameObject objectToAttach = mainMenuUITransform.GetChild(0).GetChild(0).gameObject;
            _userInterface = objectToAttach;
            _transformToScale = objectToAttach.RectTransform();
            float scale = FikaPlugin.OnlinePlayersScale.Value;
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
            foreach (GameObject item in _players)
            {
                GameObject.Destroy(item);
            }
            _players.Clear();

            FikaPlayerPresence[] response = FikaRequestHandler.GetPlayerPresences();
            _mainMenuUI.UpdateLabel(response.Length);
            SetupPlayers(ref response);
        }

        private string ConvertToTime(EDateTime dateTime, bool staticTime)
        {
            if (staticTime)
            {
                DateTime staticDate = StaticTime;
                return dateTime == EDateTime.CURR ? staticDate.ToString("HH:mm:ss") : staticDate.AddHours(-12).ToString("HH:mm:ss");
            }

            DateTime backendTime = BackendTime;
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
            foreach (FikaPlayerPresence presence in responses)
            {
                GameObject newPlayer = GameObject.Instantiate(_playerTemplate, _playerTemplate.transform.parent);
                MainMenuUIPlayer mainMenuUIPlayer = newPlayer.GetComponent<MainMenuUIPlayer>();
                mainMenuUIPlayer.SetActivity(presence.Nickname, presence.Level, presence.Activity);
                if (presence.Activity is EFikaPlayerPresence.IN_RAID && presence.RaidInformation.HasValue)
                {
                    RaidInformation information = presence.RaidInformation.Value;
                    string side = information.Side == ESideType.Pmc ? "RaidSidePmc".Localized() : "RaidSideScav".Localized();
                    string time = ConvertToTime(information.Time, IsStaticTimeLocation(information.Location));
                    HoverTooltipArea tooltip = newPlayer.AddComponent<HoverTooltipArea>();
                    tooltip.enabled = true;
                    tooltip.SetMessageText(string.Format(LocaleUtils.UI_MMUI_RAID_DETAILS.Localized(), side,
                        ColorizeText(EColor.BLUE, information.Location.Localized()),
                        BoldText(time)));
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
}
