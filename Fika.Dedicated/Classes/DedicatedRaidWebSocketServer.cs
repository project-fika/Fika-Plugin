using BepInEx.Logging;
using EFT.UI.Matchmaker;
using EFT.UI;
using EFT;
using Fika.Core.UI.Custom;
using SPT.Common.Http;
using System;
using UnityEngine;
using WebSocketSharp;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Comfort.Common;
using Fika.Core.Models;
using Fika.Dedicated;

namespace Fika.Core.Networking
{
    public class DedicatedRaidWebSocketServer
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

        public DedicatedRaidWebSocketServer()
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
            _webSocket.OnMessage += WebSocket_OnMessage;
        }

        public void Connect()
        {
            logger.LogInfo($"WS Connect()");
            logger.LogInfo($"Attempting to connect to {Url}...");
            _webSocket.Connect();
        }

        public void Close()
        {
            _webSocket.Close();
        }


        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            logger.LogInfo("Connected to FikaDedicatedRaidWebSocket as server");
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
                case "fikaDedicatedStartRaid":
                    StartDedicatedRequest request = jsonObject.ToObject<StartDedicatedRequest>();
                    FikaDedicatedPlugin.Instance.OnFikaStartRaid(request);
                    break;
            }
        }
    }
}
