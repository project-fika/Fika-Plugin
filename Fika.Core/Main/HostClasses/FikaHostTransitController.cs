using CommonAssets.Scripts.Game;
using JsonType;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.GlobalEvents;
using EFT.Interactive;
using EFT.UI;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Communication;
using System;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Fika.Core.Main.HostClasses;

public class FikaHostTransitController : LocalTransitController
{
    public FikaHostTransitController(IntPtr pointer) : base(pointer)
    {
    }

    public FikaHostTransitController(TransitGlobalSettings settings, Il2CppReferenceArray<LocationSettings.Location.TransitParameters> parameters, Profile profile, LocalRaidSettings localRaidSettings) : base(Il2CppInjection.Allocate<FikaHostTransitController>())
    {
        ClassInjector.DerivedConstructorBody(this);
        _server = Singleton<FikaServer>.Instance;
        _playersInTransitZone = [];
        _transittedPlayers = [];
        ClassInjector.InvokeBaseConstructor<LocalTransitController>(this, settings, parameters, profile, localRaidSettings);
        IsEvent = localRaidSettings.transitionType.HasFlagNoBox(ELocationTransition.Event);
        string[] array = [.. localRaidSettings.transition.visitedLocations ?? (IEnumerable<string>)[], localRaidSettings.location];
        summonedTransits[profile.Id] = new(localRaidSettings.transition.transitionRaidId, localRaidSettings.transition.transitionCount,
            array, IsEvent);
        TransferItemsController.InitItemControllerServer(FikaGlobals.TransitTraderId, FikaGlobals.TransitTraderName);
    }

    public void PostConstruct()
    {
        _onHostPlayerEnter = new Action<TransitPoint, Player>(OnHostPlayerEnter);
        _onHostPlayerExit = new Action<TransitPoint, Player>(OnHostPlayerExit);
        OnPlayerEnter = _onHostPlayerEnter;
        OnPlayerExit = _onHostPlayerExit;
    }

    private readonly FikaServer _server;
    private Il2CppSystem.Action<TransitPoint, Player> _onHostPlayerEnter;
    private Il2CppSystem.Action<TransitPoint, Player> _onHostPlayerExit;
    private readonly Dictionary<Player, int> _playersInTransitZone;
    private readonly List<int> _transittedPlayers;

    public bool IsEvent { get; }

    public int AliveTransitPlayers
    {
        get
        {
            return _transittedPlayers.Count;
        }
    }

    private void OnHostPlayerEnter(TransitPoint point, Player player)
    {
        if (!TryGetAccessToLocation(player, point.parameters.id, out var _))
        {
            if (player.IsYourPlayer)
            {
                AccessNotGrantedNotification();
            }
            return;
        }
        else
        {
            if (!TryGetAccessToLocation(player, point.parameters.id, out var _))
            {
                return;
            }
        }

        if (!_playersInTransitZone.ContainsKey(player))
        {
            _playersInTransitZone.Add(player, point.parameters.id);
        }

        if (!transitPlayers.ContainsKey(player.ProfileId))
        {
            if (player is FikaPlayer fikaPlayer)
            {
                fikaPlayer.UpdateBtrTraderServiceData()
                    .HandleExceptions();
            }

            if (player.IsYourPlayer)
            {
                ShowInteraction(point.parameters.id, player, GetSelectedTime());
                return;
            }

            TransitEventPacket packet = new()
            {
                EventType = TransitEventPacket.ETransitEventType.Interaction,
                TransitEvent = new TransitInteractionEvent()
                {
                    PlayerRaidId = player.RaidId,
                    PointId = point.parameters.id,
                    Type = TransitInteractionEvent.EType.Show
                }
            };

            _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            return;
        }
        pointsById[point.parameters.id].GroupEnter(player);
    }

    private void OnHostPlayerExit(TransitPoint point, Player player)
    {
        if (_playersInTransitZone.TryGetValue(player, out var value))
        {
            if (value == point.parameters.id)
            {
                _playersInTransitZone.Remove(player);
            }
        }

        if (transitPlayers.ContainsKey(player.ProfileId))
        {
            point.GroupExit(player);
        }
        if (player.IsYourPlayer)
        {
            Cancel(player);
            return;
        }

        TransitEventPacket packet = new()
        {
            EventType = TransitEventPacket.ETransitEventType.Interaction,
            TransitEvent = new TransitInteractionEvent()
            {
                PlayerRaidId = player.RaidId,
                PointId = point.parameters.id,
                Type = TransitInteractionEvent.EType.Hide
            }
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public override void Sizes(int pointId, Il2CppSystem.Collections.Generic.Dictionary<int, byte> sizes)
    {
        _currentTransitPointId = pointId;
#if DEBUG
        foreach (var item in sizes)
        {
            FikaGlobals.LogWarning($"int: {item.Key}, byte: {item.Value}");
        }
#endif

        foreach (var size in sizes)
        {
            if (GamePlayerOwner.MyPlayer.Id == size.Key)
            {
                MonoBehaviourSingleton<GameUI>.Instance.LocationTransitGroupSize.Display();
                MonoBehaviourSingleton<GameUI>.Instance.LocationTransitGroupSize.Show(size.Value, GetCurrentPointMaxGroupSize());
            }
        }

        TransitEventPacket packet = new()
        {
            EventType = TransitEventPacket.ETransitEventType.GroupSize,
            TransitEvent = new TransitGroupSizeEvent()
            {
                PointId = pointId,
                Sizes = sizes
            }
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public override void Timers(int pointId, Il2CppSystem.Collections.Generic.Dictionary<int, ushort> timers)
    {
        _currentTransitPointId = pointId;
#if DEBUG
        foreach (var item in timers)
        {
            FikaGlobals.LogWarning($"int: {item.Key}, ushort: {item.Value}");
        }
#endif

        foreach (var timer in timers)
        {
            if (GamePlayerOwner.MyPlayer.Id == timer.Key)
            {
                ShowPanel(pointId);
                MonoBehaviourSingleton<GameUI>.Instance.LocationTransitTimerPanel.Display();
                MonoBehaviourSingleton<GameUI>.Instance.LocationTransitTimerPanel.Show((float)timer.Value);
            }
        }

        TransitEventPacket packet = new()
        {
            EventType = TransitEventPacket.ETransitEventType.GroupTimer,
            TransitEvent = new TransitGroupTimerEvent()
            {
                PointId = pointId,
                Timers = timers
            }
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public override void InactivePointNotification(int playerId, int pointId)
    {
        if (GamePlayerOwner.MyPlayer.Id == playerId)
        {
            NotificationManager.DisplayWarningNotification("Transit/InactivePoint".Localized(null), ENotificationDurationType.Default);
            ShowPanel(pointId);
            return;
        }

        TransitEventPacket packet = new()
        {
            EventType = TransitEventPacket.ETransitEventType.Interaction,
            TransitEvent = new TransitInteractionEvent()
            {
                PlayerRaidId = playerId,
                PointId = pointId,
                Type = TransitInteractionEvent.EType.InactivePoint
            }
        };

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public override void InteractWithTransit(Player player, InteractWithTransitPacket packet)
    {
        var point = pointsById[packet.pointId];
        if (point == null)
        {
            return;
        }

        if (transitPlayers.ContainsKey(player.ProfileId))
        {
            return;
        }

        if (!CheckForPlayers(player, packet.pointId))
        {
            return;
        }

        if (player.IsYourPlayer)
        {
            Cancel(player);
            HideTransitInteractionNotification();
            transitPlayers.Add(player.ProfileId, player.Id);
            profileKeys[player.ProfileId] = packet.keyId;
            pointsById[packet.pointId].GroupEnter(player);
            ExfiltrationController.Instance.BannedPlayers.Add(player.Id);
            ExfiltrationController.Instance.CancelExtractionForPlayer(player);
            ExfiltrationController.Instance.DisableExitsInteraction();
            return;
        }

        transitPlayers[player.ProfileId] = player.Id;
        profileKeys[player.ProfileId] = packet.keyId;
        pointsById[packet.pointId].GroupEnter(player);
        ExfiltrationController.Instance.BannedPlayers.Add(player.Id);
        ExfiltrationController.Instance.CancelExtractionForPlayer(player);

        TransitEventPacket confirmPacket = new()
        {
            EventType = TransitEventPacket.ETransitEventType.Interaction,
            TransitEvent = new TransitInteractionEvent()
            {
                PlayerRaidId = player.RaidId,
                PointId = packet.pointId,
                Type = TransitInteractionEvent.EType.Confirm
            }
        };

        _server.SendData(ref confirmPacket, DeliveryMethod.ReliableOrdered);
    }

    private bool CheckForPlayers(Player player, int pointId)
    {
        var humanPlayers = 0;
        foreach (var fikaPlayer in FikaGlobals.NetworkManager.CoopHandler.HumanPlayers)
        {
            if (fikaPlayer.HealthController.IsAlive)
            {
                if (fikaPlayer.IsYourPlayer && FikaBackendUtils.IsHeadless)
                {
                    continue;
                }

                humanPlayers++;
            }
        }

        var playersInPoint = 0;
        foreach (var item in _playersInTransitZone)
        {
            if (item.Key.HealthController.IsAlive)
            {
                if (item.Value == pointId)
                {
                    playersInPoint++;
                }
            }
        }

        if (playersInPoint < humanPlayers)
        {
            if (player.IsYourPlayer)
            {
                NotificationManager.DisplayWarningNotification(TransitMessagesEvent.EType.NonAllTeammates.ToString(), ENotificationDurationType.Default);
                return false;
            }

            Dictionary<int, TransitMessagesEvent.EType> messages = [];
            messages.Add(player.Id, TransitMessagesEvent.EType.NonAllTeammates);

            TransitEventPacket messagePacket = new()
            {
                EventType = TransitEventPacket.ETransitEventType.Messages,
                TransitEvent = new TransitMessagesEvent()
                {
                    Messages = (messages).ToIl2CppDictionary()
                }
            };

            _server.SendData(ref messagePacket, DeliveryMethod.ReliableOrdered);
            return false;
        }

        return true;
    }

    public override Il2CppSystem.Threading.Tasks.Task Transit(TransitPoint point, int playersCount, string hash, Il2CppSystem.Collections.Generic.Dictionary<string, ProfileKey> keys, Player player)
    {
        if (player.IsYourPlayer)
        {
            var location = point.parameters.location;
            var eraidMode = ERaidMode.Local;
            if (TarkovApplication.Exist(out var tarkovApplication))
            {
                eraidMode = ERaidMode.Local;
                tarkovApplication.TransitionStatus = new TransitionStatus(location, false, _localRaidSettings.playerSide, eraidMode, _localRaidSettings.timeVariant);
            }
            var profileId = player.ProfileId;
            LocationTransit gclass = new()
            {
                hash = hash,
                playersCount = playersCount,
                ip = "",
                location = location,
                profiles = keys,
                transitionRaidId = summonedTransits[profileId].raidId,
                raidMode = eraidMode,
                side = player.Side is EPlayerSide.Savage ? ESideType.Savage : ESideType.Pmc,
                dayTime = _localRaidSettings.timeVariant
            };
            alreadyTransits.Add(profileId, gclass);

            var fikaGame = FikaGlobals.FikaGame;
            if (fikaGame is not CoopGame coopGame)
            {
                FikaGlobals.LogError("FikaGame was not a CoopGame!");
                return Il2CppSystem.Threading.Tasks.Task.CompletedTask;
            }

            var fikaPlayer = player.TryCast<FikaPlayer>();
            if (fikaPlayer == null)
            {
                FikaGlobals.LogError("Transit: transit player was not a FikaPlayer");
                return Il2CppSystem.Threading.Tasks.Task.CompletedTask;
            }

            coopGame.Extract(fikaPlayer, null, point);

            _transittedPlayers.Add(player.Id);
            return Il2CppSystem.Threading.Tasks.Task.CompletedTask;
        }

        TransitEventPacket packet = new()
        {
            EventType = TransitEventPacket.ETransitEventType.Extract,
            PlayerId = player.PlayerId,
            TransitId = point.parameters.id
        };

        _transittedPlayers.Add(player.Id);

        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        return Il2CppSystem.Threading.Tasks.Task.CompletedTask;
    }

    public override void Dispose()
    {
        base.Dispose();
        OnPlayerEnter -= _onHostPlayerEnter;
        OnPlayerExit -= _onHostPlayerExit;
    }

    public void Init()
    {
        EnablePoints(true);
        SetTimers(pointsById.Values, GamePlayerOwner.MyPlayer, false);
        HandleExits(pointsById.Values, GamePlayerOwner.MyPlayer);
    }
}
