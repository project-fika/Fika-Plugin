using System.Collections.Generic;
using Comfort.Common;
using EFT.UI;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using TMPro;

public class DebugUI : MonoBehaviour
{
    public TMP_Text AlivePlayersText;
    public TMP_Text AliveBotsText;
    public TMP_Text ClientsText;
    public TMP_Text PingText;
    public TMP_Text RTTText;
    public TMP_Text ServerFPSText;
    public RectTransform Border;
    public RectTransform Frame;

    private const float _serverHeight = 90f;
    private const float _clientHeight = 130f;

    private FikaServer _server;
    private CoopHandler _coopHandler;
    private float _frameCounter;
    private bool _isServer;
    private List<FikaPlayer> _alivePlayers;
    private List<FikaPlayer> _aliveBots;

    private int Ping
    {
        get
        {
            return Singleton<FikaClient>.Instance.Ping;
        }
    }

    private int ServerFPS
    {
        get
        {
            return Singleton<FikaClient>.Instance.ServerFPS;
        }
    }

    private int RTT
    {
        get
        {
            return Singleton<FikaClient>.Instance.NetClient.FirstPeer.RoundTripTime;
        }
    }

    private Color DefaultColor;

    protected void Awake()
    {
        _isServer = FikaBackendUtils.IsServer;
        if (_isServer)
        {
            _server = Singleton<FikaServer>.Instance;
        }

        if (CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            _coopHandler = coopHandler;

            _alivePlayers = [];
            _aliveBots = [];
        }
        else
        {
            FikaGlobals.LogError("FikaDebug: CoopHandler was null!");
            Destroy(gameObject);
        }

        DefaultColor = AlivePlayersText.color;

        var sizeDelta = Frame.sizeDelta;
        var borderSizeDelta = Border.sizeDelta;
        if (_isServer)
        {
            sizeDelta.y = _serverHeight;
            borderSizeDelta.y = _serverHeight + 10f;
            PingText.gameObject.SetActive(false);
            RTTText.gameObject.SetActive(false);
            ServerFPSText.gameObject.SetActive(false);
        }
        else
        {
            sizeDelta.y = _clientHeight;
            borderSizeDelta.y = _clientHeight + 10f;
            ClientsText.gameObject.SetActive(false);
        }
        Frame.sizeDelta = sizeDelta;
        Border.sizeDelta = borderSizeDelta;

        Border.gameObject.AddComponent<UIDragComponent>()
            .Init(Border, true);
    }

    protected void Update()
    {
        _frameCounter += Time.unscaledDeltaTime;
        if (_frameCounter >= 5f)
        {
            _frameCounter = 0f;
            CheckAndAdd();
        }

        AlivePlayersText.SetText("Alive Players: {0}", _alivePlayers.Count);
        AliveBotsText.SetText("Alive Bots: {0}", _aliveBots.Count);
        if (_isServer)
        {
            ClientsText.SetText("Clients: {0}", _server.NetServer.ConnectedPeersCount);
        }
        else
        {
            var ping = Ping;
            PingText.SetText("Ping: {0}", ping);
            PingText.color = GetPingColor(ping);
            var rtt = RTT;
            RTTText.SetText("RTT: {0}", rtt);
            RTTText.color = GetRTTColor(rtt);
            var serverFps = ServerFPS;
            ServerFPSText.SetText("Server FPS: {0}", serverFps);
            ServerFPSText.color = GetServerFPSColor(serverFps);
        }
    }

    private Color GetServerFPSColor(int serverFps)
    {
        if (serverFps < 30)
        {
            return Color.red;
        }

        if (serverFps < 50)
        {
            return Color.yellow;
        }

        return DefaultColor;
    }

    private Color GetRTTColor(int rtt)
    {
        if (rtt < 0.0 || rtt > 120.0)
        {
            return Color.red;
        }

        if (rtt > 60.0)
        {
            return Color.yellow;
        }

        return DefaultColor;
    }

    private Color GetPingColor(int ping)
    {
        if (ping <= 75)
        {
            return DefaultColor;
        }

        if (ping <= 125)
        {
            return Color.yellow;
        }

        return Color.red;
    }

    private void CheckAndAdd()
    {
        _alivePlayers.Clear();
        foreach (var player in _coopHandler.HumanPlayers)
        {
            if (!_alivePlayers.Contains(player) && player.HealthController.IsAlive)
            {
                AddPlayer(player);
            }
        }

        foreach (var player in _coopHandler.Players.Values)
        {
            if ((player.IsObservedAI || player.IsAI) && !_aliveBots.Contains(player) && player.HealthController.IsAlive)
            {
                AddBot(player);
            }
        }
    }

    protected void OnEnable()
    {
        CheckAndAdd();
    }

    protected void OnDisable()
    {
        foreach (var player in _alivePlayers)
        {
            player.OnPlayerDead -= PlayerDied;
        }
        _alivePlayers.Clear();

        foreach (var bot in _aliveBots)
        {
            bot.OnPlayerDead -= BotDied;
        }
        _aliveBots.Clear();
    }

    private void AddPlayer(FikaPlayer player)
    {
        player.OnPlayerDead += PlayerDied;
        _alivePlayers.Add(player);
    }

    private void PlayerDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
    {
        player.OnPlayerDead -= PlayerDied;
        _alivePlayers.Remove((FikaPlayer)player);
    }

    private void AddBot(FikaPlayer bot)
    {
        bot.OnPlayerDead += BotDied;
        _aliveBots.Add(bot);
    }

    private void BotDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
    {
        player.OnPlayerDead -= BotDied;
        _aliveBots.Remove((FikaPlayer)player);
    }
}
