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
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using WebSocketSharp;

namespace Fika.Core.Networking.Websocket;

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
        if (string.IsNullOrEmpty(e?.Data))
        {
            return;
        }

        var jsonObject = JObject.Parse(e.Data);

        if (!jsonObject.ContainsKey("Type"))
        {
            return;
        }

        var type = Enum.Parse<EFikaHeadlessWSMessageType>(jsonObject.Value<string>("Type"));

        switch (type)
        {
            case EFikaHeadlessWSMessageType.RequesterJoinRaid:
                {
                    HandleRequesterJoinRaidAsync(e.Data)
                        .Forget();
                    break;
                }
        }
    }

    private async Task HandleRequesterJoinRaidAsync(string json)
    {
        var data = json.ParseJsonTo<RequesterJoinRaid>();

        if (string.IsNullOrEmpty(data.MatchId))
        {
            PreloaderUI.Instance.ShowErrorScreen("Fika Headless Error",
                "Received RequesterJoinRaid WS event but there was no matchId");
            return;
        }

        var tarkovApplication = (TarkovApplication)Singleton<ClientApplication<ISession>>.Instance;

        var success = await MatchMakerUIScript.JoinMatch(tarkovApplication.Session.Profile.Id, data.MatchId,
            null, false);

        if (success)
        {
            FikaBackendUtils.MatchMakerAcceptScreenInstance
                .method_22()
                .HandleExceptions();
        }

        FikaPlugin.HeadlessRequesterWebSocket.Close();
    }
}
