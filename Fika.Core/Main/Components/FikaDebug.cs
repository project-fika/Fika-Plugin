using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using System.Collections.Generic;

namespace Fika.Core.Main.Components
{
    internal class FikaDebug : MonoBehaviour
    {
        private CoopHandler _coopHandler;
        private Rect _windowRect = new(20, 20, 200, 10);
        private int _frameCounter = 0;

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

        private bool isServer = false;

        private List<FikaPlayer> alivePlayers;
        private List<FikaPlayer> aliveBots;

        protected void Awake()
        {
            if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
            {
                _coopHandler = coopHandler;

                if (FikaBackendUtils.IsServer)
                {
                    isServer = true;
                }

                alivePlayers = [];
                aliveBots = [];

                enabled = false;

                return;
            }

            FikaPlugin.Instance.FikaLogger.LogError("FikaDebug: CoopHandlera was null!");
            Destroy(this);
        }

        protected void Update()
        {
            _frameCounter++;
            if (_frameCounter % 300 == 0)
            {
                _frameCounter = 0;

                CheckAndAdd();
            }
        }

        private void CheckAndAdd()
        {
            foreach (FikaPlayer player in _coopHandler.HumanPlayers)
            {
                if (!alivePlayers.Contains(player) && player.HealthController.IsAlive)
                {
                    AddPlayer(player);
                }
            }

            foreach (FikaPlayer player in _coopHandler.Players.Values)
            {
                if (!player.gameObject.name.StartsWith("Player_") && !player.IsYourPlayer)
                {
                    if (!aliveBots.Contains(player) && player.HealthController.IsAlive)
                    {
                        AddBot(player);
                    }
                }
            }
        }

        protected void OnEnable()
        {
            CheckAndAdd();
        }

        protected void OnDisable()
        {
            foreach (FikaPlayer player in alivePlayers)
            {
                player.OnPlayerDead -= PlayerDied;
            }
            alivePlayers.Clear();

            foreach (FikaPlayer bot in aliveBots)
            {
                bot.OnPlayerDead -= BotDied;
            }
            aliveBots.Clear();
        }

        private void AddPlayer(FikaPlayer player)
        {
            player.OnPlayerDead += PlayerDied;
            alivePlayers.Add(player);
        }

        private void PlayerDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
        {
            player.OnPlayerDead -= PlayerDied;
            alivePlayers.Remove((FikaPlayer)player);
        }

        private void AddBot(FikaPlayer bot)
        {
            bot.OnPlayerDead += BotDied;
            aliveBots.Add(bot);
        }

        private void BotDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfoStruct damageInfo, EBodyPart part)
        {
            player.OnPlayerDead -= BotDied;
            aliveBots.Remove((FikaPlayer)player);
        }

        protected void OnGUI()
        {
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.skin.window.alignment = TextAnchor.UpperCenter;

            GUILayout.BeginArea(_windowRect);
            GUILayout.BeginVertical();

            _windowRect = GUILayout.Window(1, _windowRect, DrawWindow, "Fika Debug");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Label($"Alive Players: {alivePlayers.Count}");
            GUILayout.Label($"Alive Bots: {aliveBots.Count}");
            if (isServer)
            {
                GUILayout.Label($"Clients: {Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount}");
            }
            else
            {
                GUILayout.Label($"Ping: {Ping}");
                GUILayout.Label($"RTT: {RTT}");
                GUILayout.Label($"Server FPS: {ServerFPS}");
            }
            GUI.DragWindow();
        }
    }
}
