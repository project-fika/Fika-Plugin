using BepInEx.Logging;
using EFT.UI.Matchmaker;
using EFT.UI;
using EFT;
using Fika.Core.UI.Custom;
using LiteNetLib;
using SPT.Common.Http;
using System;
using UnityEngine;
using WebSocketSharp;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Comfort.Common;
using TMPro;
using UnityEngine.UI;
using Fika.Core.Coop.Utils;

namespace Fika.Core.Networking.Websocket
{
    public class DedicatedRaidWebSocketClient : MonoBehaviour
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.DedicatedWebSocket");

        public string Host { get; set; }
        public string Url { get; set; }
        public string SessionId { get; set; }
        public bool Connected
        {
            get
            {
                return _webSocket.ReadyState == WebSocketState.Open;
            }
        }

        private WebSocket _webSocket;

        public DedicatedRaidWebSocketClient()
        {
            Host = RequestHandler.Host.Replace("http", "ws");
            SessionId = RequestHandler.SessionId;
            Url = $"{Host}/fika/dedicatedraidservice/{SessionId}?";

            _webSocket = new WebSocket(Url)
            {
                WaitTime = TimeSpan.FromMinutes(1),
                EmitOnPing = true
            };

            _webSocket.OnOpen += WebSocket_OnOpen;
            _webSocket.OnError += WebSocket_OnError;
            _webSocket.OnMessage += WebSocket_OnMessage;
            //_webSocket.OnClose += WebSocket_OnClose;
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            logger.LogInfo($"websocket err: {e.Message}");
        }

        public void Connect()
        {
            _webSocket.Connect();
        }

        public void Close()
        {
            _webSocket.Close();
        }

        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            logger.LogInfo("Connected to FikaDedicatedRaidWebSocket as client");
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            JObject jsonObject = JObject.Parse(e.Data);

            if (!jsonObject.ContainsKey("type"))
            {
                return;
            }

            string type = jsonObject["type"].ToString();

            switch (type)
            {
                case "fikaDedicatedJoinMatch":
                    MatchMakerUI matchmakerUI = FindObjectOfType<MatchMakerUI>();

                    string matchId = jsonObject.Value<string>("matchId");
                    MatchMakerAcceptScreen matchMakerAcceptScreen = FindObjectOfType<MatchMakerAcceptScreen>();
                    TMP_Text matchmakerUiHostRaidText = matchmakerUI.RaidGroupHostButton.GetComponentInChildren<TMP_Text>();
                    if (matchMakerAcceptScreen == null)
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", "Failed to find MatchMakerAcceptScreen");

                        matchmakerUI.RaidGroupHostButton.interactable = true;
                        matchmakerUiHostRaidText.text = "HOST RAID";
                    }

                    if (matchId is not null)
                    {
                        TarkovApplication tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;

                        tarkovApplication.StartCoroutine(MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, matchId, null, () =>
                        {                           
                            // MatchmakerAcceptScreen -> next screen (accept)
                            matchMakerAcceptScreen.method_22();

                            Destroy(matchmakerUI.gameObject);
                            Destroy(matchmakerUI);
                        }));
                    }
                    else
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", "Received fikaJoinMatch WS event but there was no matchId");

                        matchmakerUI.RaidGroupHostButton.interactable = true;
                        matchmakerUiHostRaidText.text = "HOST RAID";
                    }

                    FikaPlugin.DedicatedRaidWebSocket.Close();

                    break;
            }
        }
    }
}
