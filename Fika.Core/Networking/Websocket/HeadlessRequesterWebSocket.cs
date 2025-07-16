using BepInEx.Logging;
using Comfort.Common;
using Diz.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Websocket.Headless;
using Fika.Core.UI.Custom;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using WebSocketSharp;

namespace Fika.Core.Networking.Websocket
{
    public class HeadlessRequesterWebSocket
    {
        private static readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("Fika.HeadlessWebSocket");

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

        private readonly WebSocket _webSocket;

        public HeadlessRequesterWebSocket()
        {
            Host = RequestHandler.Host.Replace("http", "ws");
            SessionId = RequestHandler.SessionId;
            Url = $"{Host}/fika/headless/requester";

            _webSocket = new WebSocket(Url)
            {
                WaitTime = TimeSpan.FromMinutes(1),
                EmitOnPing = true
            };

            _webSocket.SetCredentials(SessionId, "", true);

            _webSocket.OnOpen += WebSocket_OnOpen;
            _webSocket.OnError += WebSocket_OnError;
            _webSocket.OnMessage += (sender, args) =>
            {
                // Run the OnMessage event on main thread
                AsyncWorker.RunInMainTread(() => WebSocket_OnMessage(sender, args));
            };
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            _logger.LogInfo($"WS error: {e.Message}");
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
            _logger.LogInfo("Connected to HeadlessRequesterWebSocket");
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
            if (!jsonObject.ContainsKey("Type"))
            {
                return;
            }

            EFikaHeadlessWSMessageType type = (EFikaHeadlessWSMessageType)Enum.Parse(typeof(EFikaHeadlessWSMessageType), jsonObject.Value<string>("Type"));

            switch (type)
            {
                case EFikaHeadlessWSMessageType.RequesterJoinRaid:

                    RequesterJoinRaid data = e.Data.ParseJsonTo<RequesterJoinRaid>();
                    MatchMakerAcceptScreen matchMakerAcceptScreen = FikaBackendUtils.MatchMakerAcceptScreenInstance;

                    if (!string.IsNullOrEmpty(data.MatchId))
                    {
                        TarkovApplication tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;
                        tarkovApplication.StartCoroutine(MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, data.MatchId, null, (bool success) =>
                        {
                            if (success)
                            {
                                // Matchmaker next screen (accept)
                                matchMakerAcceptScreen.method_22().HandleExceptions();
                            }
                        }, false));
                    }
                    else
                    {
                        PreloaderUI.Instance.ShowErrorScreen("Fika Headless Error", "Received RequesterJoinRaid WS event but there was no matchId");
                    }

                    FikaPlugin.HeadlessRequesterWebSocket.Close();

                    break;
            }
        }
    }
}
