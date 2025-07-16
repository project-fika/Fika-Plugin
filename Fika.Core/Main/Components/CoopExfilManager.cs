using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.World;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Fika.Core.Networking.Packets.World.GenericSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Main.Components
{
    public class CoopExfilManager : MonoBehaviour
    {
        private CoopGame _game;
        private List<ExtractionPlayerHandler> _playerHandlers;
        private List<ExfiltrationPoint> _countdownPoints;
        private ExfiltrationPoint[] _exfiltrationPoints;
        private SecretExfiltrationPoint[] _secretExfiltrationPoints;

        protected void Awake()
        {
            _game = gameObject.GetComponent<CoopGame>();
            _playerHandlers = [];
            _countdownPoints = [];
            _exfiltrationPoints = [];
            _secretExfiltrationPoints = [];
        }

        protected void Update()
        {
            if (_exfiltrationPoints == null || _secretExfiltrationPoints == null)
            {
                return;
            }

            for (int i = 0; i < _playerHandlers.Count; i++)
            {
                ExtractionPlayerHandler playerHandler = _playerHandlers[i];
                if (playerHandler.startTime + playerHandler.point.Settings.ExfiltrationTime - _game.PastTime <= 0)
                {
                    _playerHandlers.Remove(playerHandler);
                    _game.ExitLocation = playerHandler.point.Settings.Name;
                    _game.Extract(playerHandler.player, playerHandler.point);
                }
            }

            for (int i = 0; i < _countdownPoints.Count; i++)
            {
                ExfiltrationPoint exfiltrationPoint = _countdownPoints[i];
                if (_game.PastTime - exfiltrationPoint.ExfiltrationStartTime > exfiltrationPoint.Settings.ExfiltrationTime)
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
                            _game.ExitLocation = exfiltrationPoint.Settings.Name;
                            _game.Extract((FikaPlayer)player, exfiltrationPoint);
                        }
                    }

                    exfiltrationPoint.ExternalSetStatus(EExfiltrationStatus.NotPresent);
                    _countdownPoints.Remove(exfiltrationPoint);
                }
            }
        }

        public void Run(ExfiltrationPoint[] exfilPoints, SecretExfiltrationPoint[] secretExfilPoints)
        {
            foreach (ExfiltrationPoint exfiltrationPoint in exfilPoints)
            {
                exfiltrationPoint.OnStartExtraction += ExfiltrationPoint_OnStartExtraction;
                exfiltrationPoint.OnCancelExtraction += ExfiltrationPoint_OnCancelExtraction;
                exfiltrationPoint.OnStatusChanged += ExfiltrationPoint_OnStatusChanged;
                exfiltrationPoint.OnStatusChanged += _game.method_9;
                _game.UpdateExfiltrationUi(exfiltrationPoint, false, true);
                if (FikaPlugin.Instance.DynamicVExfils && exfiltrationPoint.Settings.PlayersCount > 0 && exfiltrationPoint.Settings.PlayersCount < FikaBackendUtils.HostExpectedNumberOfPlayers)
                {
                    exfiltrationPoint.Settings.PlayersCount = FikaBackendUtils.HostExpectedNumberOfPlayers;
                }
            }

            foreach (SecretExfiltrationPoint secretExfiltrationPoint in secretExfilPoints)
            {
                secretExfiltrationPoint.OnStartExtraction += ExfiltrationPoint_OnStartExtraction;
                secretExfiltrationPoint.OnCancelExtraction += ExfiltrationPoint_OnCancelExtraction;
                secretExfiltrationPoint.OnStatusChanged += ExfiltrationPoint_OnStatusChanged;
                secretExfiltrationPoint.OnStatusChanged += _game.method_9;
                secretExfiltrationPoint.OnStatusChanged += _game.ShowNewSecretExit;
                _game.UpdateExfiltrationUi(secretExfiltrationPoint, false, true);
                secretExfiltrationPoint.OnPointFoundEvent += SecretExfiltrationPoint_OnPointFoundEvent;
            }

            _exfiltrationPoints = exfilPoints;
            _secretExfiltrationPoints = secretExfilPoints;
        }

        private void SecretExfiltrationPoint_OnPointFoundEvent(string exitName, bool sharedExit)
        {
            FikaPlayer mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            GenericPacket packet = new()
            {
                NetId = mainPlayer.NetId,
                Type = EGenericSubPacketType.SecretExfilFound,
                SubPacket = new SecretExfilFound(mainPlayer.GroupId, exitName)
            };

            mainPlayer.PacketSender.SendPacket(ref packet);
        }

        public void Stop()
        {
            _playerHandlers.Clear();
            _countdownPoints.Clear();

            if (_exfiltrationPoints != null)
            {
                foreach (ExfiltrationPoint exfiltrationPoint in _exfiltrationPoints)
                {
                    exfiltrationPoint.OnStartExtraction -= ExfiltrationPoint_OnStartExtraction;
                    exfiltrationPoint.OnCancelExtraction -= ExfiltrationPoint_OnCancelExtraction;
                    exfiltrationPoint.OnStatusChanged -= ExfiltrationPoint_OnStatusChanged;
                    exfiltrationPoint.OnStatusChanged -= _game.method_9;
                    exfiltrationPoint.Disable();
                }
            }

            if (_secretExfiltrationPoints != null)
            {
                foreach (SecretExfiltrationPoint secretExfiltrationPoint in _secretExfiltrationPoints)
                {
                    secretExfiltrationPoint.OnStartExtraction -= ExfiltrationPoint_OnStartExtraction;
                    secretExfiltrationPoint.OnCancelExtraction -= ExfiltrationPoint_OnCancelExtraction;
                    secretExfiltrationPoint.OnStatusChanged -= ExfiltrationPoint_OnStatusChanged;
                    secretExfiltrationPoint.OnStatusChanged -= _game.method_9;
                    secretExfiltrationPoint.OnStatusChanged -= _game.ShowNewSecretExit;
                    secretExfiltrationPoint.OnPointFoundEvent -= SecretExfiltrationPoint_OnPointFoundEvent;
                    secretExfiltrationPoint.Disable();
                }
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

            ExtractionPlayerHandler extractionPlayerHandler = _playerHandlers.FirstOrDefault(x => x.player == player);
            if (extractionPlayerHandler != null)
            {
                _playerHandlers.Remove(extractionPlayerHandler);
            }
        }

        private void ExfiltrationPoint_OnStartExtraction(ExfiltrationPoint point, Player player)
        {
            if (!player.IsYourPlayer)
            {
                return;
            }

            if (_playerHandlers.All(x => x.player != player))
            {
                _playerHandlers.Add(new(player, point, _game.PastTime));
            }
        }

        private void ExfiltrationPoint_OnStatusChanged(ExfiltrationPoint point, EExfiltrationStatus prevStatus)
        {
            bool isCounting = _countdownPoints.Contains(point);
            if (isCounting && point.Status != EExfiltrationStatus.Countdown)
            {
                point.ExfiltrationStartTime = -100;
                _countdownPoints.Remove(point);
            }

            if (!isCounting && point.Status == EExfiltrationStatus.Countdown)
            {
                if (point.ExfiltrationStartTime is <= 0 and > -90)
                {
                    point.ExfiltrationStartTime = _game.PastTime;

                    FikaPlayer mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
                    GenericPacket packet = new()
                    {
                        NetId = mainPlayer.NetId,
                        Type = EGenericSubPacketType.ExfilCountdown,
                        SubPacket = new ExfilCountdown(point.Settings.Name, point.ExfiltrationStartTime)
                    };

                    mainPlayer.PacketSender.SendPacket(ref packet);
                }
                _countdownPoints.Add(point);
            }
        }

        private class ExtractionPlayerHandler(Player player, ExfiltrationPoint point, float startTime)
        {
            public FikaPlayer player = (FikaPlayer)player;
            public ExfiltrationPoint point = point;
            public float startTime = startTime;
        }
    }
}
