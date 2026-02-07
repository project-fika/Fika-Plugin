using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;

namespace Fika.Core.Main.Components;

public class FikaExfilManager : MonoBehaviour
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

        for (var i = 0; i < _playerHandlers.Count; i++)
        {
            var playerHandler = _playerHandlers[i];
            if (playerHandler.StartTime + playerHandler.ExfilPoint.Settings.ExfiltrationTime - _game.PastTime <= 0)
            {
                _playerHandlers.Remove(playerHandler);
                _game.ExitLocation = playerHandler.ExfilPoint.Settings.Name;
                _game.Extract(playerHandler.Player, playerHandler.ExfilPoint);
            }
        }

        for (var i = 0; i < _countdownPoints.Count; i++)
        {
            var exfiltrationPoint = _countdownPoints[i];
            if (_game.PastTime - exfiltrationPoint.ExfiltrationStartTime > exfiltrationPoint.Settings.ExfiltrationTime)
            {
                foreach (var player in exfiltrationPoint.Entered)
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
        for (var i = 0; i < exfilPoints.Length; i++)
        {
            var exfiltrationPoint = exfilPoints[i];
            exfiltrationPoint.OnStartExtraction += ExfiltrationPoint_OnStartExtraction;
            exfiltrationPoint.OnCancelExtraction += ExfiltrationPoint_OnCancelExtraction;
            exfiltrationPoint.OnStatusChanged += ExfiltrationPoint_OnStatusChanged;
            exfiltrationPoint.OnStatusChanged += _game.method_10;
            _game.UpdateExfiltrationUi(exfiltrationPoint, false, true);
            if (FikaPlugin.Instance.DynamicVExfils && exfiltrationPoint.Settings.PlayersCount > 0 && exfiltrationPoint.Settings.PlayersCount < Singleton<IFikaNetworkManager>.Instance.PlayerAmount)
            {
                exfiltrationPoint.Settings.PlayersCount = Singleton<IFikaNetworkManager>.Instance.PlayerAmount;
            }
        }

        for (var i = 0; i < secretExfilPoints.Length; i++)
        {
            var secretExfiltrationPoint = secretExfilPoints[i];
            secretExfiltrationPoint.OnStartExtraction += ExfiltrationPoint_OnStartExtraction;
            secretExfiltrationPoint.OnCancelExtraction += ExfiltrationPoint_OnCancelExtraction;
            secretExfiltrationPoint.OnStatusChanged += ExfiltrationPoint_OnStatusChanged;
            secretExfiltrationPoint.OnStatusChanged += _game.method_10;
            secretExfiltrationPoint.OnStatusChanged += _game.ShowNewSecretExit;
            _game.UpdateExfiltrationUi(secretExfiltrationPoint, false, true);
            secretExfiltrationPoint.OnPointFoundEvent += SecretExfiltrationPoint_OnPointFoundEvent;
        }

        _exfiltrationPoints = exfilPoints;
        _secretExfiltrationPoints = secretExfilPoints;
    }

    private void SecretExfiltrationPoint_OnPointFoundEvent(string exitName, bool sharedExit)
    {
        var mainPlayer = (FikaPlayer)Singleton<GameWorld>.Instance.MainPlayer;
        Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.SecretExfilFound,
            SecretExfilFound.FromValue(mainPlayer.GroupId, exitName), true);
    }

    public void Stop()
    {
        _playerHandlers.Clear();
        _countdownPoints.Clear();

        if (_exfiltrationPoints != null)
        {
            for (var i = 0; i < _exfiltrationPoints.Length; i++)
            {
                var exfiltrationPoint = _exfiltrationPoints[i];
                exfiltrationPoint.OnStartExtraction -= ExfiltrationPoint_OnStartExtraction;
                exfiltrationPoint.OnCancelExtraction -= ExfiltrationPoint_OnCancelExtraction;
                exfiltrationPoint.OnStatusChanged -= ExfiltrationPoint_OnStatusChanged;
                exfiltrationPoint.OnStatusChanged -= _game.method_10;
                exfiltrationPoint.Disable();
            }
        }

        if (_secretExfiltrationPoints != null)
        {
            for (var i = 0; i < _secretExfiltrationPoints.Length; i++)
            {
                var secretExfiltrationPoint = _secretExfiltrationPoints[i];
                secretExfiltrationPoint.OnStartExtraction -= ExfiltrationPoint_OnStartExtraction;
                secretExfiltrationPoint.OnCancelExtraction -= ExfiltrationPoint_OnCancelExtraction;
                secretExfiltrationPoint.OnStatusChanged -= ExfiltrationPoint_OnStatusChanged;
                secretExfiltrationPoint.OnStatusChanged -= _game.method_10;
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

        var extractionPlayerHandler = _playerHandlers.FirstOrDefault(x => x.Player == player);
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

        if (_playerHandlers.All(x => x.Player != player))
        {
            _playerHandlers.Add(new(player, point, _game.PastTime));
        }
    }

    private void ExfiltrationPoint_OnStatusChanged(ExfiltrationPoint point, EExfiltrationStatus prevStatus)
    {
        var isCounting = _countdownPoints.Contains(point);
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
                Singleton<IFikaNetworkManager>.Instance.SendGenericPacket(EGenericSubPacketType.ExfilCountdown,
                        ExfilCountdown.FromValue(point.Settings.Name, point.ExfiltrationStartTime), true);
            }
            _countdownPoints.Add(point);
        }
    }

    private class ExtractionPlayerHandler(Player player, ExfiltrationPoint point, float startTime)
    {
        public readonly FikaPlayer Player = (FikaPlayer)player;
        public readonly ExfiltrationPoint ExfilPoint = point;
        public readonly float StartTime = startTime;
    }
}
