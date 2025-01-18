﻿using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Coop.Utils;
using Fika.Core.UI.Custom;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using WebSocketSharp;

namespace Fika.Core.Networking.Websocket
{
    public class HeadlessRaidWebSocketClient
    {
        private static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.HeadlessWebSocket");

        public string Host { get; set; }
        public string Url { get; set; }
        public string SessionId { get; set; }
        public bool Connected
        {
            get
            {
                return webSocket.ReadyState == WebSocketState.Open;
            }
        }

        private WebSocket webSocket;

        public HeadlessRaidWebSocketClient()
        {
            Host = RequestHandler.Host.Replace("http", "ws");
            SessionId = RequestHandler.SessionId;
            Url = $"{Host}/fika/headlessraidservice/";

            webSocket = new WebSocket(Url)
            {
                WaitTime = TimeSpan.FromMinutes(1),
                EmitOnPing = true
            };

            webSocket.SetCredentials(SessionId, "", true);

            webSocket.OnOpen += WebSocket_OnOpen;
            webSocket.OnError += WebSocket_OnError;
            webSocket.OnMessage += (sender, args) =>
            {
                // Run the OnMessage event on main thread
                MainThreadDispatcher.RunOnMainThread(() =>
                {
                    WebSocket_OnMessage(sender, args);
                });
            };
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            logger.LogInfo($"WS error: {e.Message}");
        }

        public void Connect()
        {
            webSocket.Connect();
        }

        public void Close()
        {
            webSocket.Close();
        }

        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            logger.LogInfo("Connected to FikaHeadlessRaidWebSocket as client");
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

            // TODO: Convert to bytes and use an enum...
            string type = jsonObject.Value<string>("type");

            switch (type)
            {
                case "fikaHeadlessJoinMatch":
                    string matchId = jsonObject.Value<string>("matchId");

                    MatchMakerAcceptScreen matchMakerAcceptScreen = FikaBackendUtils.MatchMakerAcceptScreenInstance;

                    if (!string.IsNullOrEmpty(matchId))
                    {
                        TarkovApplication tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;
                        tarkovApplication.StartCoroutine(MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, matchId, null, (bool success) =>
                        {
                            if (success)
                            {
                                // Matchmaker next screen (accept)
                                matchMakerAcceptScreen.method_19().HandleExceptions();
                            }
                        }, false));
                    }
                    else
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Headless Error", "Received fikaJoinMatch WS event but there was no matchId");
                    }

                    FikaPlugin.HeadlessRaidWebSocket.Close();

                    break;
            }
        }
    }
}