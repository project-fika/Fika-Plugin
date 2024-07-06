using BepInEx.Logging;
using EFT.UI.Matchmaker;
using EFT.UI;
using EFT;
using Fika.Core.UI.Custom;
using LiteNetLib;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Comfort.Common;

namespace Fika.Core.Networking
{
    public class FikaDedicatedWebSocket
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

        public FikaDedicatedWebSocket()
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
            //_webSocket.OnError += WebSocket_OnError;
            _webSocket.OnMessage += WebSocket_OnMessage;
            //_webSocket.OnClose += WebSocket_OnClose;
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
            logger.LogInfo("Connected to FikaNatPunchRelayService as server");
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

            if(!jsonObject.ContainsKey("type"))
            {
                return;
            }

            string type = jsonObject["type"].ToString();

            switch (type)
            {
                case "fikaDedicatedJoinMatch":

                    ConsoleScreen.Log("received fikaJoinMatch");
                    string matchId = jsonObject.Value<string>("matchId");
                    MatchMakerAcceptScreen matchMakerAcceptScreen = GameObject.FindObjectOfType<MatchMakerAcceptScreen>();
                    if (matchMakerAcceptScreen == null)
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", "Failed to find MatchMakerAcceptScreen", () =>
                        {
                            var acceptScreen = GameObject.FindObjectOfType<MatchMakerAcceptScreen>();
                            var controller = Traverse.Create(acceptScreen).Field<MatchMakerAcceptScreen.GClass3177>("ScreenController").Value;
                            controller.CloseScreen();
                        });

                        return;
                    }

                    if (matchId is not null)
                    {
                        //Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.QuestCompleted);
                        TarkovApplication tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;
                        tarkovApplication.StartCoroutine(MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, matchId, null, () =>
                        {
                            Traverse.Create(matchMakerAcceptScreen).Field<DefaultUIButton>("_acceptButton").Value.OnClick.Invoke();
                        }));
                    }
                    else
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Dedicated Error", "Received fikaJoinMatch WS event but there was no matchId", () =>
                        {
                            var acceptScreen = GameObject.FindObjectOfType<MatchMakerAcceptScreen>();
                            var controller = Traverse.Create(acceptScreen).Field<MatchMakerAcceptScreen.GClass3177>("ScreenController").Value;
                            controller.CloseScreen();
                        });
                    }

                    break;
            }
        }
    }
}
