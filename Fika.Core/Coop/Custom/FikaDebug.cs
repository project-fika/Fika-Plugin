using Comfort.Common;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.Custom
{
    internal class FikaDebug : MonoBehaviour
    {
        private CoopHandler coopHandler;
        private Rect windowRect = new(20, 20, 200, 200);
        private int frameCounter = 0;

        private int Ping
        {
            get
            {
                return Singleton<FikaClient>.Instance.Ping;
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

        private List<CoopPlayer> alivePlayers;
        private List<CoopPlayer> aliveBots;

        protected void Awake()
        {
            coopHandler = CoopHandler.GetCoopHandler();
            if (coopHandler == null)
            {
                FikaPlugin.Instance.FikaLogger.LogError("FikaDebug: CoopHandlera was null!");
                Destroy(this);
            }            

            if (MatchmakerAcceptPatches.IsServer)
            {
                isServer = true;
            }

            alivePlayers = [];
            aliveBots = [];

            enabled = false;
        }

        protected void Update()
        {
            frameCounter++;
            if (frameCounter % 300 == 0)
            {
                frameCounter = 0;

                CheckAndAdd();
            }
        }

        private void CheckAndAdd()
        {
            foreach (CoopPlayer player in coopHandler.Players.Values)
            {
                if (player.gameObject.name.StartsWith("Player_") || player.IsYourPlayer)
                {
                    if (!alivePlayers.Contains(player) && player.HealthController.IsAlive)
                    {
                        AddPlayer(player);
                    }
                }
                else
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
            foreach (CoopPlayer player in alivePlayers)
            {
                player.OnPlayerDead -= PlayerDied;
            }
            alivePlayers.Clear();

            foreach (CoopPlayer bot in aliveBots)
            {
                bot.OnPlayerDead -= BotDied;
            }
            aliveBots.Clear();
        }        

        private void AddPlayer(CoopPlayer player)
        {
            player.OnPlayerDead += PlayerDied;
            alivePlayers.Add(player);
        }

        private void PlayerDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfo damageInfo, EBodyPart part)
        {
            player.OnPlayerDead -= PlayerDied;
            alivePlayers.Remove((CoopPlayer)player);
        }

        private void AddBot(CoopPlayer bot)
        {
            bot.OnPlayerDead += BotDied;
            aliveBots.Add(bot);
        }

        private void BotDied(EFT.Player player, EFT.IPlayer lastAggressor, DamageInfo damageInfo, EBodyPart part)
        {
            player.OnPlayerDead -= BotDied;
            aliveBots.Remove((CoopPlayer)player);
        }

        protected void OnGUI()
        {
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.skin.window.alignment = TextAnchor.UpperCenter;

            GUI.Window(0, windowRect, DrawWindow, "Fika Debug");    
        }

        private void DrawWindow(int windowId)
        {
            Rect rect = new(5, 15, 150, 25);
            GUI.Label(rect, $"Alive Players: {alivePlayers.Count}");
            rect.y += 15;
            GUI.Label(rect, $"Alive Bots: {aliveBots.Count}");            
            if (isServer)
            {
                rect.y += 15;
                GUI.Label(rect, $"Clients: {Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount}"); 
            }
            else
            {
                rect.y += 15;
                GUI.Label(rect, $"Ping: {Ping}");
                rect.y += 15;
                GUI.Label(rect, $"RTT: {RTT}");
            }
        }
    }
}
