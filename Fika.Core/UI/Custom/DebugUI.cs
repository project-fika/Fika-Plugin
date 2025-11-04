using Comfort.Common;
using EFT.UI;
using Fika.Core;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Mono.Cecil.Cil;
using System.Collections.Generic;
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

    private CoopHandler _coopHandler;
    private int _frameCounter;
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

    protected void Awake()
    {
        _isServer = FikaBackendUtils.IsServer;

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
        _frameCounter++;
        if (_frameCounter % 300 == 0)
        {
            _frameCounter = 0;
            CheckAndAdd();
        }

        AlivePlayersText.SetText($"Alive Players: {_alivePlayers.Count}");
        AliveBotsText.SetText($"Alive Bots: {_aliveBots.Count}");
        if (_isServer)
        {
            ClientsText.SetText($"Clients: {Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount}");
        }
        else
        {
            PingText.SetText($"Ping: {Ping}");
            RTTText.SetText($"RTT: {RTT}");
            ServerFPSText.SetText($"Server FPS: {ServerFPS}");
        }
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
