using Comfort.Common;
using EFT.UI;
using Fika.Core;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
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

        if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
        {
            _coopHandler = coopHandler;

            _alivePlayers = [];
            _aliveBots = [];
        }
        else
        {
            FikaPlugin.Instance.FikaLogger.LogError("FikaDebug: CoopHandler was null!");
            Destroy(gameObject);
        }

        var sizeDelta = Frame.sizeDelta;
        if (_isServer)
        {
            sizeDelta.y = _serverHeight;
            PingText.gameObject.SetActive(false);
            RTTText.gameObject.SetActive(false);
            ServerFPSText.gameObject.SetActive(false);
        }
        else
        {
            sizeDelta.y = _clientHeight;
            ClientsText.gameObject.SetActive(false);
        }
        Frame.sizeDelta = sizeDelta;
    }

    protected void Start()
    {
        Frame.gameObject.AddComponent<UIDragComponent>()
            .Init(Frame, true);
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
        foreach (FikaPlayer player in _coopHandler.HumanPlayers)
        {
            if (!_alivePlayers.Contains(player) && player.HealthController.IsAlive)
            {
                AddPlayer(player);
            }
        }

        foreach (FikaPlayer player in _coopHandler.Players.Values)
        {
            if (player.IsObservedAI && !_aliveBots.Contains(player) && player.HealthController.IsAlive)
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
        foreach (FikaPlayer player in _alivePlayers)
        {
            player.OnPlayerDead -= PlayerDied;
        }
        _alivePlayers.Clear();

        foreach (FikaPlayer bot in _aliveBots)
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
