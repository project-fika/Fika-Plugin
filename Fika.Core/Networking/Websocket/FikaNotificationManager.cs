using BepInEx.Logging;
using Comfort.Common;
using Diz.Utils;
using EFT.UI;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking.Websocket.Notifications;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Fika.Core.Networking.Websocket
{
    internal class FikaNotificationManager
    {
        private static readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("FikaNotificationManager");
        public static FikaNotificationManager Instance;
        public static bool Exists
        {
            get
            {
                return Instance != null;
            }
        }
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

        public bool reconnecting;

        private WebSocket _webSocket;

        public FikaNotificationManager()
        {
            Instance = this;
            Host = RequestHandler.Host.Replace("http", "ws");
            SessionId = RequestHandler.SessionId;
            Url = $"{Host}/fika/notification/";

            _webSocket = new WebSocket(Url)
            {
                WaitTime = TimeSpan.FromMinutes(1),
                EmitOnPing = true
            };

            _webSocket.SetCredentials(SessionId, "", true);

            _webSocket.OnError += WebSocket_OnError;
            _webSocket.OnMessage += WebSocket_OnMessage;
            _webSocket.OnClose += (sender, e) =>
            {
                if (reconnecting)
                {
                    return;
                }

                Task.Run(ReconnectWebSocket);
            };

            Connect();
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

            EFikaNotifications type = (EFikaNotifications)Enum.Parse(typeof(EFikaNotifications), jsonObject.Value<string>("type"));

#if DEBUG
            _logger.LogDebug($"Received type: {type}");
#endif
            NotificationAbstractClass notification = null;
            switch (type)
            {
                case EFikaNotifications.StartedRaid:
                    notification = e.Data.ParseJsonTo<StartRaidNotification>([]);

                    if (FikaGlobals.IsInRaid)
                    {
                        return;
                    }
                    HandleNotification(notification);
                    break;
                case EFikaNotifications.SentItem:
                    notification = e.Data.ParseJsonTo<ReceivedSentItemNotification>([]);
                    HandleNotification(notification);
                    break;
                case EFikaNotifications.PushNotification:
                    notification = e.Data.ParseJsonTo<PushNotification>([]);
                    HandleNotification(notification);
                    break;
                case EFikaNotifications.KeepAlive:
                    break;
                case EFikaNotifications.OpenAdminSettings:
                    notification = e.Data.ParseJsonTo<OpenAdminMenuNotification>([]);
                    HandleAdminMenu(notification);
                    break;
            }
        }

        private void HandleAdminMenu(NotificationAbstractClass notification)
        {
            if (notification is OpenAdminMenuNotification openAdminNotif && openAdminNotif.Success)
            {
                AsyncWorker.RunInMainTread(() =>
                {
                    AdminSettingsUIScript.Create();
                }); 
            }
        }

        private void HandleNotification(NotificationAbstractClass notification)
        {
            AsyncWorker.RunInMainTread(() =>
            {
                Singleton<PreloaderUI>.Instance.NotifierView.method_5(notification);
            });
        }

        private async Task ReconnectWebSocket()
        {
            reconnecting = true;

            while (reconnecting)
            {
                if (_webSocket.ReadyState == WebSocketState.Open)
                {
                    break;
                }

                // Don't attempt to reconnect if we're still attempting to connect.
                if (_webSocket.ReadyState != WebSocketState.Connecting)
                {
                    _webSocket.Connect();
                }

                await Task.Delay(10 * 1000);

                if (_webSocket.ReadyState == WebSocketState.Open)
                {
                    break;
                }
            }

            reconnecting = false;
        }

#if DEBUG
        public static void TestNotification(EFikaNotifications type)
        {
            // Ugly ass one-liner, who cares. It's for debug purposes
            string Username = FikaPlugin.DevelopersList.ToList()[new Random().Next(FikaPlugin.DevelopersList.Count)].Key;

            switch (type)
            {
                case EFikaNotifications.StartedRaid:
                    StartRaidNotification startRaidNotification = new()
                    {
                        Nickname = Username,
                        Location = "Factory"
                    };

                    Singleton<PreloaderUI>.Instance.NotifierView.method_5(startRaidNotification);
                    break;
                case EFikaNotifications.SentItem:
                    ReceivedSentItemNotification SentItemNotification = new()
                    {
                        Nickname = Username,
                        ItemName = "LEDX Skin Transilluminator"
                    };

                    Singleton<PreloaderUI>.Instance.NotifierView.method_5(SentItemNotification);
                    break;
                case EFikaNotifications.PushNotification:
                    PushNotification PushNotification = new()
                    {
                        Notification = "Test notification",
                        NotificationIcon = EFT.Communications.ENotificationIconType.Note
                    };

                    Singleton<PreloaderUI>.Instance.NotifierView.method_5(PushNotification);
                    break;
            }
        }
#endif
    }
}
