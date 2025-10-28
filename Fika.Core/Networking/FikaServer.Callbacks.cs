using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vehicle;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Factories;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Communication;
#if DEBUG
using Fika.Core.Networking.Packets.Debug;
using EFT.UI;
#endif
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.World;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static Fika.Core.Networking.Packets.World.ReconnectPacket;
using Fika.Core.ConsoleCommands;
using static Fika.Core.Networking.Packets.Debug.CommandPacket;

namespace Fika.Core.Networking;

public partial class FikaServer
{
#if DEBUG
    private void OnCommandPacketReceived(CommandPacket packet, NetPeer peer)
    {
        switch (packet.CommandType)
        {
            case ECommandType.SpawnAI:
                FikaCommands.SpawnNPC(packet.SpawnType, packet.SpawnAmount);
                break;
            case ECommandType.DespawnAI:
                FikaCommands.DespawnAllAI();
                break;
            case ECommandType.Bring:
                FikaCommands.BringReplicated(packet.NetId);
                break;
            case ECommandType.SpawnAirdrop:
                FikaCommands.SpawnAirdrop();
                break;
            default:
                break;
        }
    }

    private void OnSpawnItemPacketReceived(SpawnItemPacket packet, NetPeer peer)
    {
        SendData(ref packet, DeliveryMethod.ReliableOrdered, peer);

        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            FikaGlobals.SpawnItemInWorld(packet.Item, playerToApply);
        }
    }
#endif

    private void OnEventControllerInteractPacketReceived(EventControllerInteractPacket packet, NetPeer peer)
    {
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer player))
        {
            if (gameWorld.RunddansController != null)
            {
                gameWorld.RunddansController.InteractWithEventObject(player, packet.Data);
            }
        }
    }

    private void OnInraidQuestPacketReceived(InRaidQuestPacket packet, NetPeer peer)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer player))
        {
            if (player.AbstractQuestControllerClass is ObservedQuestController controller)
            {
                controller.HandleInraidQuestPacket(packet);
            }
        }
    }

    private void OnNetworkSettingsPacketReceived(NetworkSettingsPacket packet, NetPeer peer)
    {
#if DEBUG
        _logger.LogInfo($"Received connection from {packet.ProfileId}");
#endif
        if (!_cachedConnections.TryGetValue(packet.ProfileId, out int netId))
        {
            netId = PopNetId();
            _cachedConnections.Add(packet.ProfileId, netId);
        }

        NetworkSettingsPacket response = new()
        {
            SendRate = _sendRate,
            NetId = netId,
            AllowVOIP = AllowVOIP
        };
        SendDataToPeer(ref response, DeliveryMethod.ReliableOrdered, peer);
    }

    private void OnWorldPacketReceived(WorldPacket packet, NetPeer peer)
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null)
        {
            _logger.LogError("OnNewWorldPacketReceived: GameWorld was null!");
            return;
        }

        FikaHostWorld.LootSyncPackets.AddRange(packet.LootSyncStructs);
    }

    private void OnRequestPacketReceived(RequestPacket packet, NetPeer peer)
    {
        if (packet.RequestSubPacket == null)
        {
            _logger.LogError("OnRequestPacketReceived: RequestSubPacket was null!");
            return;
        }

        packet.RequestSubPacket.HandleRequest(peer, this);
    }

    private void OnSideEffectPacketReceived(SideEffectPacket packet, NetPeer peer)
    {
#if DEBUG
        _logger.LogWarning("OnSideEffectPacketReceived: Received");
#endif

        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null)
        {
            _logger.LogError("OnSideEffectPacketReceived: GameWorld was null!");
            return;
        }

        GStruct156<Item> gstruct2 = gameWorld.FindItemById(packet.ItemId);
        if (gstruct2.Failed)
        {
            _logger.LogError("OnSideEffectPacketReceived: " + gstruct2.Error);
            return;
        }
        Item item = gstruct2.Value;
        if (item.TryGetItemComponent(out SideEffectComponent sideEffectComponent))
        {
#if DEBUG
            _logger.LogInfo("Setting value to: " + packet.Value + ", original: " + sideEffectComponent.Value);
#endif
            sideEffectComponent.Value = packet.Value;
            item.RaiseRefreshEvent(false, false);
            return;
        }
        _logger.LogError("OnSideEffectPacketReceived: SideEffectComponent was not found!");
    }

    private void OnLoadingProfilePacketReceived(LoadingProfilePacket packet, NetPeer peer)
    {
        if (packet.Profiles == null)
        {
            _logger.LogError("OnLoadingProfilePacketReceived: Profiles was null!");
            return;
        }

        (Profile profile, bool isLeader) = packet.Profiles.First();
        if (!_visualProfiles.Any(x => x.Key.ProfileId == profile.ProfileId))
        {
            _visualProfiles.Add(profile, _visualProfiles.Count == 0 || isLeader);
        }
        FikaBackendUtils.AddPartyMembers(_visualProfiles);
        packet.Profiles = _visualProfiles;
        SendData(ref packet, DeliveryMethod.ReliableOrdered);

        ClientConnected clientConnected = ClientConnected.FromValue(profile.Info.MainProfileNickname);
        if (!FikaBackendUtils.IsHeadless)
        {
            clientConnected.Execute();
        }

        SendGenericPacket(EGenericSubPacketType.ClientConnected,
            clientConnected, true);

        peer.Tag = profile.Info.MainProfileNickname;
    }

    private void OnPingPacketReceived(PingPacket packet, NetPeer peer)
    {
        if (FikaPlugin.UsePingSystem.Value && !FikaBackendUtils.IsHeadless)
        {
            PingFactory.ReceivePing(packet.PingLocation, packet.PingType, packet.PingColor, packet.Nickname, packet.LocaleId);
        }
    }

    private void OnBotStatePacketReceived(BotStatePacket packet, NetPeer peer)
    {
        switch (packet.Type)
        {
            case BotStatePacket.EStateType.LoadBot:
                {
                    IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
                    if (fikaGame != null)
                    {
                        (fikaGame.GameController as HostGameController).IncreaseLoadedPlayers(packet.NetId);
                    }
                }
                break;
            case BotStatePacket.EStateType.DisposeBot:
            case BotStatePacket.EStateType.EnableBot:
            case BotStatePacket.EStateType.DisableBot:
            default:
                break;
        }
    }

    private void OnTransitInteractPacketReceived(TransitInteractPacket packet, NetPeer peer)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            TransitControllerAbstractClass transitController = Singleton<GameWorld>.Instance.TransitController;
            if (transitController != null)
            {
                transitController.InteractWithTransit(playerToApply, packet.Data);
            }
        }
    }

    private void OnSyncTransitControllersPacketReceived(SyncTransitControllersPacket packet, NetPeer peer)
    {
        TransitControllerAbstractClass transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController != null)
        {
            transitController.summonedTransits[packet.ProfileId] = new(packet.RaidId, packet.Count, packet.Maps, false);
            return;
        }

        _logger.LogError("OnSyncTransitControllersPacketReceived: TransitController was null!");
    }

    private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet, NetPeer peer)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld.BtrController != null && gameWorld.BtrController.BtrVehicle != null)
            {
                EBtrInteractionStatus status = gameWorld.BtrController.BtrVehicle.method_39(playerToApply, packet.Data);
                BTRInteractionPacket response = new(packet.NetId)
                {
                    IsResponse = true,
                    Status = status,
                    Data = packet.Data
                };

                SendData(ref response, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    private void OnResyncInventoryIdPacketReceived(ResyncInventoryIdPacket packet, NetPeer peer)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            if (playerToApply is ObservedPlayer observedPlayer)
            {
                if (observedPlayer.InventoryController is ObservedInventoryController observedController)
                {
                    observedController.SetNewID(packet.MongoId);
                }
            }
        }
    }

    private void OnReconnectPacketReceived(ReconnectPacket packet, NetPeer peer)
    {
        if (packet.IsRequest)
        {
            if (packet.InitialRequest)
            {
                NotificationManagerClass.DisplayMessageNotification(LocaleUtils.RECONNECT_REQUESTED.Localized(),
                    iconType: EFT.Communications.ENotificationIconType.Alert);
                foreach (FikaPlayer player in _coopHandler.HumanPlayers)
                {
                    if (player.ProfileId == packet.ProfileId && player is ObservedPlayer observedPlayer)
                    {
                        ReconnectPacket ownCharacterPacket = new()
                        {
                            Type = EReconnectDataType.OwnCharacter,
                            Profile = observedPlayer.Profile,
                            ProfileHealthClass = observedPlayer.NetworkHealthController.Store(),
                            PlayerPosition = observedPlayer.Position
                        };

                        SendDataToPeer(ref ownCharacterPacket, DeliveryMethod.ReliableOrdered, peer);

                        observedPlayer.HealthBar.ClearEffects();
                        SendGenericPacket(EGenericSubPacketType.ClearEffects,
                            ClearEffects.FromValue(observedPlayer.NetId), true, peer);
                    }
                }

                return;
            }

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            Traverse worldTraverse = Traverse.Create(gameWorld.World_0);

            GClass818<int, Throwable>.GStruct48 grenades = gameWorld.Grenades.GetValuesEnumerator();
            List<SmokeGrenadeDataPacketStruct> smokeData = [];
            foreach (Throwable item in grenades)
            {
                if (item is SmokeGrenade smokeGrenade)
                {
                    smokeData.Add(smokeGrenade.NetworkData);
                }
            }

            if (smokeData.Count > 0)
            {
                ReconnectPacket throwablePacket = new()
                {
                    Type = EReconnectDataType.Throwable,
                    ThrowableData = smokeData
                };

                SendDataToPeer(ref throwablePacket, DeliveryMethod.ReliableOrdered, peer);
            }

            List<WorldInteractiveObject.WorldInteractiveDataPacketStruct> interactivesData = [];
            foreach (WorldInteractiveObject interactiveObject in worldTraverse.Field<WorldInteractiveObject[]>("worldInteractiveObject_0").Value)
            {
                if ((interactiveObject.DoorState != interactiveObject.InitialDoorState
                    && interactiveObject.DoorState != EDoorState.Interacting)
                    || (interactiveObject is Door door && door.IsBroken))
                {
                    interactivesData.Add(interactiveObject.GetStatusInfo(true));
                }
            }

            if (interactivesData.Count > 0)
            {
                ReconnectPacket interactivePacket = new()
                {
                    Type = EReconnectDataType.Interactives,
                    InteractivesData = interactivesData
                };

                SendDataToPeer(ref interactivePacket, DeliveryMethod.ReliableOrdered, peer);
            }

            IEnumerable<LampController> lampControllers = LocationScene.GetAllObjects<LampController>(false);
            Dictionary<int, byte> lampStates = [];
            foreach (LampController controller in lampControllers)
            {
                lampStates.Add(controller.NetId, (byte)controller.LampState);
            }

            if (lampStates.Count > 0)
            {
                ReconnectPacket lampPacket = new()
                {
                    Type = EReconnectDataType.LampControllers,
                    LampStates = lampStates
                };

                SendDataToPeer(ref lampPacket, DeliveryMethod.ReliableOrdered, peer);
            }

            GClass818<int, WindowBreaker>.GStruct48 windows = gameWorld.Windows.GetValuesEnumerator();
            Dictionary<int, Vector3> windowData = [];
            foreach (WindowBreaker window in windows)
            {
                if (window.AvailableToSync && window.IsDamaged)
                {
                    windowData.Add(window.NetId, window.FirstHitPosition.Value);
                }
            }

            if (windowData.Count > 0)
            {
                ReconnectPacket windowPacket = new()
                {
                    Type = EReconnectDataType.Windows,
                    WindowBreakerStates = windowData
                };

                SendDataToPeer(ref windowPacket, DeliveryMethod.ReliableOrdered, peer);
            }

            foreach (FikaPlayer player in _coopHandler.Players.Values)
            {
                if (player.ProfileId == packet.ProfileId)
                {
                    continue;
                }

                SendCharacterPacket characterPacket = SendCharacterPacket.FromValue(new()
                {
                    Profile = player.Profile,
                    ControllerId = player.InventoryController.CurrentId,
                    FirstOperationId = player.InventoryController.NextOperationId
                },
                player.HealthController.IsAlive, player.IsAI, player.Position, player.NetId);

                if (player.ActiveHealthController != null)
                {
                    characterPacket.PlayerInfoPacket.HealthByteArray = player.ActiveHealthController.SerializeState();
                }
                else if (player is ObservedPlayer observedPlayer)
                {
                    characterPacket.PlayerInfoPacket.HealthByteArray = observedPlayer.NetworkHealthController.Store().SerializeHealthInfo();
                }

                if (player.HandsController != null)
                {
                    characterPacket.PlayerInfoPacket.ControllerType = HandsControllerToEnumClass.FromController(player.HandsController);
                    characterPacket.PlayerInfoPacket.ItemId = player.HandsController.Item.Id;
                    characterPacket.PlayerInfoPacket.IsStationary = player.MovementContext.IsStationaryWeaponInHands;
                }

                SendGenericPacketToPeer(EGenericSubPacketType.SendCharacter, characterPacket, peer);
            }

            StashesPacket stashesPacket = new();
            if (gameWorld.BtrController != null)
            {
                stashesPacket.HasBTR = true;
                int length = gameWorld.BtrController.TransferItemsController.List_0.Count;
                stashesPacket.BTRStashes = new StashItemClass[length];
                for (int i = 0; i < length; i++)
                {
                    stashesPacket.BTRStashes[i] = gameWorld.BtrController.TransferItemsController.List_0[i];
                }
            }

            if (gameWorld.TransitController != null)
            {
                stashesPacket.HasTransit = true;
                int length = gameWorld.TransitController.TransferItemsController.List_0.Count;
                stashesPacket.TransitStashes = new StashItemClass[length];
                for (int i = 0; i < length; i++)
                {
                    stashesPacket.TransitStashes[i] = gameWorld.TransitController.TransferItemsController.List_0[i];
                }
            }

            SendDataToPeer(ref stashesPacket, DeliveryMethod.ReliableOrdered, peer);

            ReconnectPacket finishPacket = new()
            {
                Type = EReconnectDataType.Finished
            };

            SendDataToPeer(ref finishPacket, DeliveryMethod.ReliableOrdered, peer);
        }
    }

    private void OnWorldLootPacketReceived(WorldLootPacket packet, NetPeer peer)
    {
        IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame != null)
        {
            WorldLootPacket response = new()
            {
                Data = (fikaGame.GameController as HostGameController).GetHostLootItems()
            };

            SendDataToPeer(ref response, DeliveryMethod.ReliableOrdered, peer);
        }
    }

    private void OnInteractableInitPacketReceived(InteractableInitPacket packet, NetPeer peer)
    {
        if (packet.IsRequest)
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                World world = Singleton<GameWorld>.Instance.World_0;
                if (world.Interactables != null)
                {
                    InteractableInitPacket response = new(false)
                    {
                        Interactables = (Dictionary<string, int>)world.Interactables
                    };

                    SendDataToPeer(ref response, DeliveryMethod.ReliableOrdered, peer);
                }
            }
        }
    }

    private void OnQuestDropItemPacketReceived(QuestDropItemPacket packet, NetPeer peer)
    {
        if (_hostPlayer == null)
        {
            return;
        }

        if (_hostPlayer.HealthController.IsAlive)
        {
            if (_hostPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.ReceiveQuestDropItemPacket(packet);
            }
        }
    }

    private void OnQuestItemPacketReceived(QuestItemPacket packet, NetPeer peer)
    {
        if (_hostPlayer == null)
        {
            return;
        }

        if (_hostPlayer.HealthController.IsAlive)
        {
            if (_hostPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.ReceiveQuestItemPacket(packet);
            }
        }
    }

    private void OnQuestConditionPacketReceived(QuestConditionPacket packet, NetPeer peer)
    {
        if (_hostPlayer == null)
        {
            return;
        }

        if (_hostPlayer.HealthController.IsAlive)
        {
            if (_hostPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.ReceiveQuestPacket(packet);
            }
        }
    }

    private void OnTextMessagePacketReceived(TextMessagePacket packet, NetPeer peer)
    {
        _logger.LogInfo($"Received message from: {packet.Nickname}, Message: {packet.Message}");

        if (_fikaChat != null)
        {
            _fikaChat.AddMessage(new(packet.Nickname, packet.Message), true);
            _fikaChat.OpenChat(true);
        }
    }

    private void OnGenericPacketReceived(GenericPacket packet, NetPeer peer)
    {
        if (packet.Type is EGenericSubPacketType.InventoryOperation)
        {
            OnInventoryPacketReceived((InventoryPacket)packet.SubPacket, peer);
            return;
        }
        packet.Execute();
    }

    private void OnInformationPacketReceived(InformationPacket packet, NetPeer peer)
    {
        ReadyClients += packet.ReadyPlayers;

        bool gameExists = _coopHandler != null && _coopHandler.LocalGameInstance != null;

        InformationPacket respondPackage = new()
        {
            RaidStarted = gameExists && Singleton<IFikaGame>.Instance.GameController.RaidStarted,
            ReadyPlayers = ReadyClients,
            HostReady = HostReady,
            HostLoaded = RaidInitialized,
            AmountOfPeers = _netServer.ConnectedPeersCount + 1
        };

        if (gameExists && packet.RequestStart)
        {
            Singleton<IFikaGame>.Instance.GameController.RaidStarted = true;
        }

        if (gameExists && HostReady)
        {
            respondPackage.GameTime = _gameStartTime.Value;
            GameTimerClass gameTimer = _coopHandler.LocalGameInstance.GameController.GameInstance.GameTimer;
            respondPackage.SessionTime = gameTimer.SessionTime.Value;
        }

        SendData(ref respondPackage, DeliveryMethod.ReliableOrdered);
    }

    private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet, NetPeer peer)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            packet.Execute(playerToApply);
        }
    }

    private void OnInventoryPacketReceived(InventoryPacket packet, NetPeer peer)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            using GClass1283 eftReader = PacketToEFTReaderAbstractClass.Get(packet.OperationBytes);
            try
            {
                if (playerToApply.InventoryController is Interface18 inventoryController)
                {
                    BaseDescriptorClass descriptor = eftReader.ReadPolymorph<BaseDescriptorClass>();
                    OperationDataStruct result = inventoryController.CreateOperationFromDescriptor(descriptor);
#if DEBUG
                    if (result.Succeeded)
                    {
                        ConsoleScreen.Log($"Received InvOperation: {result.Value.GetType().Name}, Id: {result.Value.Id}");
                    }
#endif

                    if (result.Failed)
                    {
                        _logger.LogError($"ItemControllerExecutePacket::Operation conversion failed: {result.Error}");
                        SendGenericPacketToPeer(EGenericSubPacketType.OperationCallback,
                            OperationCallbackPacket.FromValue(packet.NetId, packet.CallbackId, EOperationStatus.Failed, result.Error.ToString()), peer);

                        ResyncInventoryIdPacket resyncPacket = new(playerToApply.NetId);
                        SendDataToPeer(ref resyncPacket, DeliveryMethod.ReliableOrdered, peer);
                        return;
                    }

                    InventoryOperationHandler handler = new(result, packet.CallbackId, packet.NetId, peer, this);
                    SendGenericPacketToPeer(EGenericSubPacketType.OperationCallback,
                            OperationCallbackPacket.FromValue(packet.NetId, packet.CallbackId, EOperationStatus.Started), peer);

                    SendGenericPacket(EGenericSubPacketType.InventoryOperation, packet,
                        true, peer);
                    handler.OperationResult.Value.method_1(handler.HandleResult);
                }
                else
                {
                    throw new InvalidTypeException($"Inventory controller was not of type {nameof(Interface18)}!");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"ItemControllerExecutePacket::Exception thrown: {exception}");
                SendGenericPacketToPeer(EGenericSubPacketType.OperationCallback,
                            OperationCallbackPacket.FromValue(packet.NetId, packet.CallbackId, EOperationStatus.Failed, exception.Message), peer);

                ResyncInventoryIdPacket resyncPacket = new(playerToApply.NetId);
                SendDataToPeer(ref resyncPacket, DeliveryMethod.ReliableOrdered, peer);
            }
        }
    }

    private void OnWeaponPacketReceived(WeaponPacket packet, NetPeer peer)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            packet.Execute(playerToApply);
        }
    }

    private class InventoryOperationHandler(OperationDataStruct operationResult, ushort operationId, int netId, NetPeer peer, FikaServer server)
    {
        public OperationDataStruct OperationResult = operationResult;
        private readonly ushort _operationId = operationId;
        private readonly int _netId = netId;
        private readonly NetPeer _peer = peer;
        private readonly FikaServer _server = server;

        internal void HandleResult(IResult result)
        {
            if (!result.Succeed)
            {
                _server._logger.LogError($"Error in operation: {result.Error ?? "An unknown error has occured"}");
                _server.SendGenericPacketToPeer(EGenericSubPacketType.OperationCallback,
                            OperationCallbackPacket.FromValue(_netId, _operationId, EOperationStatus.Failed,
                            result.Error ?? "An unknown error has occured"), _peer);

                ResyncInventoryIdPacket resyncPacket = new(_netId);
                _server.SendDataToPeer(ref resyncPacket, DeliveryMethod.ReliableOrdered, _peer);

                return;
            }

            _server.SendGenericPacketToPeer(EGenericSubPacketType.OperationCallback,
                            OperationCallbackPacket.FromValue(_netId, _operationId, EOperationStatus.Succeeded), _peer);
        }
    }
}
