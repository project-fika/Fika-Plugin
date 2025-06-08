using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.GlobalEvents;
using EFT.Interactive;
using EFT.UI;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using System.Collections.Generic;

namespace Fika.Core.Coop.HostClasses
{
    public class FikaHostTransitController : GClass1710
    {
        public FikaHostTransitController(BackendConfigSettingsClass.TransitSettingsClass settings, LocationSettingsClass.Location.TransitParameters[] parameters, Profile profile, LocalRaidSettings localRaidSettings)
            : base(settings, parameters, profile, localRaidSettings)
        {
            _localRaidSettings = localRaidSettings;
            string[] array = [.. localRaidSettings.transition.visitedLocations.EmptyIfNull(), localRaidSettings.location];
            summonedTransits[profile.Id] = new(localRaidSettings.transition.transitionRaidId, localRaidSettings.transition.transitionCount, array,
                localRaidSettings.transitionType.HasFlagNoBox(ELocationTransition.Event));
            TransferItemsController.InitItemControllerServer(FikaGlobals.TransitTraderId, FikaGlobals.TransitTraderName);
            _server = Singleton<FikaServer>.Instance;
            _playersInTransitZone = [];
            _transittedPlayers = [];
        }

        public void PostConstruct()
        {
            OnPlayerEnter = FikaGlobals.ClearDelegates(OnPlayerEnter);
            OnPlayerEnter += OnHostPlayerEnter;
            OnPlayerExit = FikaGlobals.ClearDelegates(OnPlayerExit);
            OnPlayerExit += OnHostPlayerExit;
        }

        private readonly LocalRaidSettings _localRaidSettings;
        private readonly FikaServer _server;
        private readonly Dictionary<Player, int> _playersInTransitZone;
        private readonly List<int> _transittedPlayers;

        public int AliveTransitPlayers
        {
            get
            {
                return _transittedPlayers.Count;
            }
        }

        private void OnHostPlayerEnter(TransitPoint point, Player player)
        {
            if (!method_11(player, point.parameters.id, out string _))
            {
                if (player.IsYourPlayer)
                {
                    method_13();
                }
                return;
            }
            else
            {
                if (!method_11(player, point.parameters.id, out string _))
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
                if (player is CoopPlayer coopPlayer)
                {
                    coopPlayer.UpdateBtrTraderServiceData().HandleExceptions();
                }

                if (player.IsYourPlayer)
                {
                    method_14(point.parameters.id, player, method_17());
                    return;
                }

                TransitEventPacket packet = new()
                {
                    EventType = TransitEventPacket.ETransitEventType.Interaction,
                    TransitEvent = new TransitInteractionEvent()
                    {
                        PlayerId = player.Id,
                        PointId = point.parameters.id,
                        Type = TransitInteractionEvent.EType.Show
                    }
                };

                _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                return;
            }
            Dictionary_0[point.parameters.id].GroupEnter(player);
        }

        private void OnHostPlayerExit(TransitPoint point, Player player)
        {
            if (_playersInTransitZone.TryGetValue(player, out int value))
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
                method_18(player);
                return;
            }

            TransitEventPacket packet = new()
            {
                EventType = TransitEventPacket.ETransitEventType.Interaction,
                TransitEvent = new TransitInteractionEvent()
                {
                    PlayerId = player.Id,
                    PointId = point.parameters.id,
                    Type = TransitInteractionEvent.EType.Hide
                }
            };

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public override void Sizes(Dictionary<int, byte> sizes)
        {
#if DEBUG
            foreach (KeyValuePair<int, byte> item in sizes)
            {
                FikaPlugin.Instance.FikaLogger.LogWarning($"int: {item.Key}, byte: {item.Value}");
            }
#endif

            foreach (KeyValuePair<int, byte> size in sizes)
            {
                if (GamePlayerOwner.MyPlayer.Id == size.Key)
                {
                    MonoBehaviourSingleton<GameUI>.Instance.LocationTransitGroupSize.Display();
                    MonoBehaviourSingleton<GameUI>.Instance.LocationTransitGroupSize.Show((int)size.Value);
                }
            }

            TransitEventPacket packet = new()
            {
                EventType = TransitEventPacket.ETransitEventType.GroupSize,
                TransitEvent = new TransitGroupSizeEvent()
                {
                    Sizes = sizes
                }
            };

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public override void Timers(int pointId, Dictionary<int, ushort> timers)
        {
#if DEBUG
            foreach (KeyValuePair<int, ushort> item in timers)
            {
                FikaPlugin.Instance.FikaLogger.LogWarning($"int: {item.Key}, ushort: {item.Value}");
            }
#endif

            foreach (KeyValuePair<int, ushort> timer in timers)
            {
                if (GamePlayerOwner.MyPlayer.Id == timer.Key)
                {
                    method_12(pointId);
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

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public override void InactivePointNotification(int playerId, int pointId)
        {
            if (GamePlayerOwner.MyPlayer.Id == playerId)
            {
                NotificationManagerClass.DisplayWarningNotification("Transit/InactivePoint".Localized(null), ENotificationDurationType.Default);
                method_12(pointId);
                return;
            }

            TransitEventPacket packet = new()
            {
                EventType = TransitEventPacket.ETransitEventType.Interaction,
                TransitEvent = new TransitInteractionEvent()
                {
                    PlayerId = playerId,
                    PointId = pointId,
                    Type = TransitInteractionEvent.EType.InactivePoint
                }
            };

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public override void InteractWithTransit(Player player, TransitInteractionPacketStruct packet)
        {
            TransitPoint point = Dictionary_0[packet.pointId];
            if (point == null)
            {
                return;
            }

            if (!CheckForPlayers(player, packet.pointId))
            {
                return;
            }

            if (player.IsYourPlayer)
            {
                method_18(player);
                transitPlayers.Add(player.ProfileId, player.Id);
                profileKeys[player.ProfileId] = packet.keyId;
                Dictionary_0[packet.pointId].GroupEnter(player);
                ExfiltrationControllerClass.Instance.BannedPlayers.Add(player.Id);
                ExfiltrationControllerClass.Instance.CancelExtractionForPlayer(player);
                ExfiltrationControllerClass.Instance.DisableExitsInteraction();
                return;
            }

            transitPlayers[player.ProfileId] = player.Id;
            profileKeys[player.ProfileId] = packet.keyId;
            Dictionary_0[packet.pointId].GroupEnter(player);
            ExfiltrationControllerClass.Instance.BannedPlayers.Add(player.Id);
            ExfiltrationControllerClass.Instance.CancelExtractionForPlayer(player);

            TransitEventPacket eventPacket = new()
            {
                EventType = TransitEventPacket.ETransitEventType.Interaction,
                TransitEvent = new TransitInteractionEvent()
                {
                    PlayerId = player.Id,
                    PointId = packet.pointId,
                    Type = TransitInteractionEvent.EType.Confirm
                }
            };

            _server.SendDataToAll(ref eventPacket, DeliveryMethod.ReliableOrdered);
        }

        private bool CheckForPlayers(Player player, int pointId)
        {
            int humanPlayers = 0;
            foreach (CoopPlayer coopPlayer in Singleton<IFikaNetworkManager>.Instance.CoopHandler.HumanPlayers)
            {
                if (coopPlayer.HealthController.IsAlive)
                {
                    if (coopPlayer.IsYourPlayer && FikaBackendUtils.IsHeadless)
                    {
                        continue;
                    }

                    humanPlayers++;
                }
            }

            int playersInPoint = 0;
            foreach (KeyValuePair<Player, int> item in _playersInTransitZone)
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
                    NotificationManagerClass.DisplayWarningNotification(TransitMessagesEvent.EType.NonAllTeammates.ToString(), ENotificationDurationType.Default);
                    return false;
                }

                Dictionary<int, TransitMessagesEvent.EType> messages = [];
                messages.Add(player.Id, TransitMessagesEvent.EType.NonAllTeammates);

                TransitEventPacket messagePacket = new()
                {
                    EventType = TransitEventPacket.ETransitEventType.Messages,
                    TransitEvent = new TransitMessagesEvent()
                    {
                        Messages = messages
                    }
                };

                _server.SendDataToAll(ref messagePacket, DeliveryMethod.ReliableOrdered);
                return false;
            }

            return true;
        }

        public override void Transit(TransitPoint point, int playersCount, string hash, Dictionary<string, ProfileKey> keys, Player player)
        {
            if (player.IsYourPlayer)
            {
                string location = point.parameters.location;
                ERaidMode eraidMode = ERaidMode.Local;
                if (TarkovApplication.Exist(out TarkovApplication tarkovApplication))
                {
                    eraidMode = ERaidMode.Local;
                    tarkovApplication.transitionStatus = new(location, false, _localRaidSettings.playerSide, eraidMode, _localRaidSettings.timeVariant);
                }
                string profileId = player.ProfileId;
                AlreadyTransitDataClass gclass = new()
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

                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame is not CoopGame coopGame)
                {
                    FikaGlobals.LogError("FikaGame was not a CoopGame!");
                    return;
                }

                if (coopGame != null)
                {
                    coopGame.Extract((CoopPlayer)player, null, point);
                }

                _transittedPlayers.Add(player.Id);
                return;
            }

            TransitEventPacket packet = new()
            {
                EventType = TransitEventPacket.ETransitEventType.Extract,
                PlayerId = player.PlayerId,
                TransitId = point.parameters.id
            };

            _transittedPlayers.Add(player.Id);

            _server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public override void Dispose()
        {
            base.Dispose();
            OnPlayerEnter -= OnHostPlayerEnter;
            OnPlayerExit -= OnHostPlayerExit;
        }

        public void Init()
        {
            EnablePoints(true);
            method_8(Dictionary_0.Values, GamePlayerOwner.MyPlayer, false);
            method_2(Dictionary_0.Values, GamePlayerOwner.MyPlayer);

            /*TransitEventPacket packet = new()
			{
				EventType = TransitEventPacket.ETransitEventType.Init
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);*/
        }
    }
}
