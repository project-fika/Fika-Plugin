using System;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using Diz.Utils;
using EFT;
using EFT.UI;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Websocket.Headless;
using Fika.Core.UI.Custom;
using SPTushonka.Common.Http;
using System.Text.Json;
using Fika.Core.Networking.Http;

namespace Fika.Core.Networking.Websocket;

public class HeadlessRequesterWebSocket
{
    private static readonly ManualLogSource _logger = Logger.CreateLogSource("Fika.HeadlessWebSocket");

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

    private readonly FikaWebSocket _webSocket;

    public HeadlessRequesterWebSocket()
    {
        Host = RequestHandler.Host.Replace("http", "ws");
        SessionId = RequestHandler.SessionId;
        Url = $"{Host}/fika/headless/requester";

        _webSocket = new FikaWebSocket(Url, SessionId, TimeSpan.FromMinutes(1));
        _webSocket.Opened += WebSocket_OnOpen;
        _webSocket.Errored += WebSocket_OnError;
        // Run the OnMessage event on main thread
        _webSocket.MessageReceived += data => MainThread.Post(() => WebSocket_OnMessage(data));
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

    private void WebSocket_OnOpen()
    {
        _logger.LogInfo("Connected to HeadlessRequesterWebSocket");
    }

    private void WebSocket_OnMessage(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        using var document = JsonDocument.Parse(data);
        if (!document.RootElement.TryGetProperty("Type", out var typeElement))
        {
            return;
        }

        var type = typeElement.ValueKind == JsonValueKind.Number
            ? (EFikaHeadlessWSMessageType)typeElement.GetInt32()
            : Enum.Parse<EFikaHeadlessWSMessageType>(typeElement.GetString());

        switch (type)
        {
            case EFikaHeadlessWSMessageType.RequesterJoinRaid:
                {
                    HandleRequesterJoinRaidAsync(data)
                        .Forget();
                    break;
                }
        }
    }

    private async Task HandleRequesterJoinRaidAsync(string json)
    {
        var data = FikaJson.Deserialize<RequesterJoinRaid>(json);

        if (string.IsNullOrEmpty(data.MatchId))
        {
            PreloaderUI.Instance.ShowErrorScreen("Fika Headless Error",
                "Received RequesterJoinRaid WS event but there was no matchId");
            return;
        }

        var tarkovApplication = (TarkovApplication)Singleton<ClientApplication<IEftSession>>.Instance;

        var success = await MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, data.MatchId,
            null, false);

        if (success)
        {
            FikaBackendUtils.MatchMakerAcceptScreenInstance
                .GameStart()
                .HandleExceptions();
        }

        FikaPlugin.HeadlessRequesterWebSocket.Close();
    }
}
