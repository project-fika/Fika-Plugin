using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
    internal class CoopExfilManager : MonoBehaviour
    {
        private CoopGame game;
        private List<ExtractionPlayerHandler> playerHandlers;
        private List<ExfiltrationPoint> countdownPoints;
        private ExfiltrationPoint[] exfiltrationPoints;
        private CarExtraction carExfil = null;

        protected void Awake()
        {
            game = gameObject.GetComponent<CoopGame>();
            playerHandlers = [];
            countdownPoints = [];
            exfiltrationPoints = [];
            carExfil = FindObjectOfType<CarExtraction>();
        }

        protected void Update()
        {
            if (exfiltrationPoints == null)
            {
                return;
            }

            for (int i = 0; i < playerHandlers.Count; i++)
            {
                ExtractionPlayerHandler playerHandler = playerHandlers[i];
                if (playerHandler.startTime + playerHandler.point.Settings.ExfiltrationTime - game.PastTime <= 0)
                {
                    playerHandlers.Remove(playerHandler);
                    game.MyExitLocation = playerHandler.point.Settings.Name;
                    game.Extract(playerHandler.player, playerHandler.point);
                }
            }

            for (int i = 0; i < countdownPoints.Count; i++)
            {
                ExfiltrationPoint exfiltrationPoint = countdownPoints[i];
                if (game.PastTime - exfiltrationPoint.ExfiltrationStartTime > exfiltrationPoint.Settings.ExfiltrationTime)
                {
                    foreach (Player player in exfiltrationPoint.Entered.ToArray())
                    {
                        if (player == null)
                        {
                            continue;
                        }

                        if (!player.IsYourPlayer)
                        {
                            continue;
                        }

                        if (!player.HealthController.IsAlive)
                        {
                            continue;
                        }

                        if (!exfiltrationPoint.UnmetRequirements(player).Any())
                        {
                            game.MyExitLocation = exfiltrationPoint.Settings.Name;
                            game.Extract((CoopPlayer)player, exfiltrationPoint);
                        }
                    }

                    if (carExfil != null)
                    {
                        if (carExfil.Subscribee == exfiltrationPoint)
                        {
                            carExfil.Play();
                        }
                    }

                    exfiltrationPoint.ExternalSetStatus(EExfiltrationStatus.NotPresent);
                    countdownPoints.Remove(exfiltrationPoint);
                }
            }
        }

        public void Run(ExfiltrationPoint[] exfilPoints)
        {
            foreach (ExfiltrationPoint exfiltrationPoint in exfilPoints)
            {
                exfiltrationPoint.OnStartExtraction += ExfiltrationPoint_OnStartExtraction;
                exfiltrationPoint.OnCancelExtraction += ExfiltrationPoint_OnCancelExtraction;
                exfiltrationPoint.OnStatusChanged += ExfiltrationPoint_OnStatusChanged;
                game.UpdateExfiltrationUi(exfiltrationPoint, false, true);
                if (FikaPlugin.Instance.DynamicVExfils && exfiltrationPoint.Settings.PlayersCount > 0 && exfiltrationPoint.Settings.PlayersCount < MatchmakerAcceptPatches.HostExpectedNumberOfPlayers)
                {
                    exfiltrationPoint.Settings.PlayersCount = MatchmakerAcceptPatches.HostExpectedNumberOfPlayers;
                }
            }

            exfiltrationPoints = exfilPoints;
        }

        public void Stop()
        {
            playerHandlers.Clear();
            countdownPoints.Clear();

            if (exfiltrationPoints == null)
            {
                return;
            }

            foreach (ExfiltrationPoint exfiltrationPoint in exfiltrationPoints)
            {
                exfiltrationPoint.OnStartExtraction -= ExfiltrationPoint_OnStartExtraction;
                exfiltrationPoint.OnCancelExtraction -= ExfiltrationPoint_OnCancelExtraction;
                exfiltrationPoint.OnStatusChanged -= ExfiltrationPoint_OnStatusChanged;
                exfiltrationPoint.Disable();
            }
        }

        public void UpdateExfilPointFromServer(ExfiltrationPoint point, bool enable)
        {
            if (enable)
            {
                point.OnStartExtraction += ExfiltrationPoint_OnStartExtraction;
                point.OnCancelExtraction += ExfiltrationPoint_OnCancelExtraction;
                point.OnStatusChanged += ExfiltrationPoint_OnStatusChanged;
            }
            else
            {
                point.OnStartExtraction -= ExfiltrationPoint_OnStartExtraction;
                point.OnCancelExtraction -= ExfiltrationPoint_OnCancelExtraction;
                point.OnStatusChanged -= ExfiltrationPoint_OnStatusChanged;
            }
        }

        private void ExfiltrationPoint_OnCancelExtraction(ExfiltrationPoint point, Player player)
        {
            if (!player.IsYourPlayer)
            {
                return;
            }

            ExtractionPlayerHandler extractionPlayerHandler = playerHandlers.FirstOrDefault(x => x.player == player);
            if (extractionPlayerHandler != null)
            {
                playerHandlers.Remove(extractionPlayerHandler);
            }
        }

        private void ExfiltrationPoint_OnStartExtraction(ExfiltrationPoint point, Player player)
        {
            if (!player.IsYourPlayer)
            {
                return;
            }

            if (playerHandlers.All(x => x.player != player))
            {
                playerHandlers.Add(new(player, point, game.PastTime));
            }
        }

        private void ExfiltrationPoint_OnStatusChanged(ExfiltrationPoint point, EExfiltrationStatus prevStatus)
        {
            bool isCounting = countdownPoints.Contains(point);
            if (isCounting && point.Status != EExfiltrationStatus.Countdown)
            {
                point.ExfiltrationStartTime = -100;
                countdownPoints.Remove(point);

                if (carExfil != null)
                {
                    if (carExfil.Subscribee == point)
                    {
                        carExfil.Play();
                    }
                }
            }

            if (!isCounting && point.Status == EExfiltrationStatus.Countdown)
            {
                if (point.ExfiltrationStartTime is <= 0 and > -90)
                {
                    point.ExfiltrationStartTime = game.PastTime;

                    CoopPlayer mainPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                    GenericPacket packet = new(EPackageType.ExfilCountdown)
                    {
                        NetId = mainPlayer.NetId,
                        ExfilName = point.Settings.Name,
                        ExfilStartTime = point.ExfiltrationStartTime
                    };

                    NetDataWriter writer = mainPlayer.PacketSender.Writer;
                    writer.Reset();

                    if (MatchmakerAcceptPatches.IsServer)
                    {
                        mainPlayer.PacketSender.Server.SendDataToAll(writer, ref packet, DeliveryMethod.ReliableOrdered);
                    }
                    else if (MatchmakerAcceptPatches.IsClient)
                    {
                        mainPlayer.PacketSender.Client.SendData(writer, ref packet, DeliveryMethod.ReliableOrdered);
                    }
                }
                countdownPoints.Add(point);
            }
        }

        private class ExtractionPlayerHandler(Player player, ExfiltrationPoint point, float startTime)
        {
            public CoopPlayer player = (CoopPlayer)player;
            public ExfiltrationPoint point = point;
            public float startTime = startTime;
        }
    }
}
