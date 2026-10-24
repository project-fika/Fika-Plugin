using BepInEx.Logging;
using Comfort.Common;
using Diz.Utils;
using EFT.Communications;
using EFT.UI;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Websocket.Notifications;
using SPTushonka.Common.Http;
using System;
#if DEBUG
using System.Linq;
#endif
using System.Threading.Tasks;
using System.Text.Json;
using Fika.Core.Networking.Http;

namespace Fika.Core.Networking.Websocket;

internal class FikaNotificationManager
{
    private static readonly ManualLogSource _logger = Logger.CreateLogSource("FikaNotificationManager");
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
            return _webSocket.IsOpen;
        }
    }

    public bool reconnecting;

    private readonly FikaWebSocket _webSocket;

    public FikaNotificationManager()
    {
        Instance = this;
        Host = RequestHandler.Host.Replace("http", "ws");
        SessionId = RequestHandler.SessionId;
        Url = $"{Host}/fika/notification/";

        _webSocket = new FikaWebSocket(Url, SessionId, TimeSpan.FromMinutes(1));
        _webSocket.Errored += WebSocket_OnError;
        _webSocket.MessageReceived += data => MainThread.Post(() => WebSocket_OnMessage(data));
        _webSocket.Closed += () =>
        {
            if (reconnecting)
            {
                return;
            }

            Task.Run(ReconnectWebSocket);
        };

        Connect();
    }

    private void WebSocket_OnError(string message)
    {
        _logger.LogInfo($"WS error: {message}");
    }

    public void Connect()
    {
        _webSocket.Connect();
    }

    public void Close()
    {
        _webSocket.Close();
    }

    private void WebSocket_OnMessage(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        using var document = JsonDocument.Parse(data);
        if (!document.RootElement.TryGetProperty("type", out var typeElement))
        {
            return;
        }

        var type = typeElement.ValueKind == JsonValueKind.Number
            ? (EFikaNotification)typeElement.GetInt32()
            : Enum.Parse<EFikaNotification>(typeElement.GetString());

#if DEBUG
        _logger.LogDebug($"Received type: {type}");
#endif
        Notification notification = null;
        switch (type)
        {
            case EFikaNotification.StartedRaid:
                notification = FikaJson.Deserialize<StartRaidNotification>(data);

                if (FikaGlobals.IsInRaid)
                {
                    return;
                }
                HandleNotification(notification);
                break;
            case EFikaNotification.SentItem:
                notification = FikaJson.Deserialize<ReceivedSentItemNotification>(data);
                HandleNotification(notification);
                break;
            case EFikaNotification.PushNotification:
                notification = FikaJson.Deserialize<PushNotification>(data);
                HandleNotification(notification);
                break;
            case EFikaNotification.KeepAlive:
                break;
            case EFikaNotification.OpenAdminSettings:
                notification = FikaJson.Deserialize<OpenAdminMenuNotification>(data);
                HandleAdminMenu(notification);
                break;
            case EFikaNotification.ShutdownClient:
                notification = FikaJson.Deserialize<ShutdownClientNotification>(data);
                HandleShutdown(notification);
                break;
            case EFikaNotification.HeadlessConnected:
                notification = FikaJson.Deserialize<HeadlessConnectedNotification>(data);
                HandleNotification(notification);
                break;
        }
    }

    private void HandleShutdown(Notification notification)
    {
        if (FikaBackendUtils.IsHeadless)
        {
            MainThread.Post(Application.Quit);
        }
    }

    private void HandleAdminMenu(Notification notification)
    {
        if (notification is OpenAdminMenuNotification openAdminNotif && openAdminNotif.Success)
        {
            MainThread.Post(AdminSettingsUIScript.Create);
        }
    }

    private void HandleNotification(Notification notification)
    {
        MainThread.Post(() => Singleton<PreloaderUI>.Instance.NotifierView.NotificationReceivedHandler(notification));
    }

    private async Task ReconnectWebSocket()
    {
        reconnecting = true;

        while (reconnecting)
        {
            if (_webSocket.IsOpen)
            {
                break;
            }

            // Don't attempt to reconnect if we're still attempting to connect.
            if (!_webSocket.IsConnecting)
            {
                _webSocket.Connect();
            }

            await Task.Delay(10 * 1000);

            if (_webSocket.IsOpen)
            {
                break;
            }
        }

        reconnecting = false;
    }

#if DEBUG
    public static void TestNotification(EFikaNotification type)
    {
        // Ugly ass one-liner, who cares. It's for debug purposes
        var Username = FikaPlugin.DevelopersList.ToList()[new System.Random().Next(FikaPlugin.DevelopersList.Count)].Key;

        switch (type)
        {
            case EFikaNotification.StartedRaid:
                StartRaidNotification startRaidNotification = new()
                {
                    Nickname = Username,
                    Location = "Factory"
                };

                Singleton<PreloaderUI>.Instance.NotifierView.NotificationReceivedHandler(startRaidNotification);
                break;
            case EFikaNotification.SentItem:
                ReceivedSentItemNotification SentItemNotification = new()
                {
                    Nickname = Username,
                    ItemName = "LEDX Skin Transilluminator"
                };

                Singleton<PreloaderUI>.Instance.NotifierView.NotificationReceivedHandler(SentItemNotification);
                break;
            case EFikaNotification.PushNotification:
                PushNotification PushNotification = new()
                {
                    Notification = "Test notification",
                    NotificationIcon = ENotificationIconType.Note
                };

                Singleton<PreloaderUI>.Instance.NotifierView.NotificationReceivedHandler(PushNotification);
                break;
        }
    }
#endif
}
