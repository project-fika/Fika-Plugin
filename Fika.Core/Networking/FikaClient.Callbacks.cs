using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Communications;
using EFT.GlobalEvents;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Vehicle;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Factories;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.Debug;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.World;
using HarmonyLib;

namespace Fika.Core.Networking;

public partial class FikaClient
{
    private void OnSyncEventPacketReceived(SyncEventPacket packet)
    {
        _logger.LogInfo($"Received sync event: {packet.Type}");
        var reader = NetworkUtils.EventDataReader;
        reader.Reset();
        Array.Copy(packet.Data, reader.Buffer, packet.Data.Length);
        switch (packet.Type)
        {
            case 0:
                var initEvent = new TransitInitEvent();
                initEvent.Deserialize(ref reader);
                initEvent.Invoke();
                break;
            case 1:
                var updateEvent = new TransitUpdateEvent();
                updateEvent.Deserialize(ref reader);
                updateEvent.Invoke();
                break;
        }
    }

    private void OnLoadingScreenPlayersPacketReceived(LoadingScreenPlayersPacket packet)
    {
        if (LoadingScreenUI.Instance != null)
        {
            for (var i = 0; i < packet.NetIds.Length; i++)
            {
                LoadingScreenUI.Instance.AddPlayer(packet.NetIds[i], packet.Nicknames[i]);
            }
        }
    }

    private void OnLoadingScreenPacketReceived(LoadingScreenPacket packet)
    {
        if (LoadingScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance.SetProgress(packet.NetId, packet.Progress);
        }
    }

    private void OnStashesPacketReceived(StashesPacket packet)
    {
#if DEBUG
        _logger.LogWarning($"Received [StashesPacket] from server! BTR: {packet.HasBTR}, Transit: {packet.HasTransit}");
#endif
        if (Singleton<GameWorld>.Instantiated)
        {
            var gameWorld = Singleton<GameWorld>.Instance;

            if (packet.HasBTR)
            {
                if (gameWorld.BtrController != null)
                {
                    for (var i = 0; i < packet.BTRStashes.Length; i++)
                    {
                        gameWorld.BtrController.TransferItemsController.InitTransferContainer(packet.BTRStashes[i], "BTR");
                        var array = new StashGridClass[gameWorld.BtrController.TransferItemsController.Stash.Grids.Length + 1];
                        gameWorld.BtrController.TransferItemsController.Stash.Grids.CopyTo(array, 0);
                        array[^1] = new StashGridClass(packet.BTRStashes[i].Id,
                            10, 10, true, false, [],
                            gameWorld.BtrController.TransferItemsController.Stash, -1);
                        gameWorld.BtrController.TransferItemsController.Stash.Grids = array;
                    }
                }
                else
                {
                    _logger.LogError("Received 'StashesPacket' with BTRData but BTRController was null!");
                }
            }

            if (packet.HasTransit)
            {
                if (gameWorld.TransitController != null)
                {
                    for (var i = 0; i < packet.TransitStashes.Length; i++)
                    {
                        gameWorld.TransitController.TransferItemsController.InitTransferContainer(packet.TransitStashes[i], "BTR");
                        var array = new StashGridClass[gameWorld.TransitController.TransferItemsController.Stash.Grids.Length + 1];
                        gameWorld.TransitController.TransferItemsController.Stash.Grids.CopyTo(array, 0);
                        array[^1] = new StashGridClass(packet.TransitStashes[i].Id,
                            10, 10, true, false, [],
                            gameWorld.TransitController.TransferItemsController.Stash, -1);
                        gameWorld.TransitController.TransferItemsController.Stash.Grids = array;
                    }
                }
                else
                {
                    _logger.LogError("Received 'StashesPacket' with TransitData but TransitController was null!");
                }
            }
        }
        else
        {
            _logger.LogError("Received 'StashesPacket' but GameWorld was null!");
        }
    }

    private void OnSyncTrapsPacketReceived(SyncTrapsPacket packet)
    {
        GClass1364 reader = new(packet.Data);
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld.SyncModule != null)
        {
            gameWorld.SyncModule.Deserialize(reader);
        }
        else
        {
            _logger.LogError("Received SyncTrapsPacket but the SyncModule was null!");
        }
    }

    private void OnEventControllerInteractPacketReceived(EventControllerInteractPacket packet)
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld != null && gameWorld.RunddansController is ClientRunddansController clientController)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out var player))
            {
                clientController.DestroyItem(player);
            }
        }
    }

    private void OnEventControllerEventPacketReceived(EventControllerEventPacket packet)
    {
        if (packet.Type == EventControllerEventPacket.EEventType.StartedEvent)
        {
            var clientTransitController = (ClientTransitController)Singleton<GameWorld>.Instance.TransitController;
            if (clientTransitController != null)
            {
                clientTransitController.EnablePoints(false);
                clientTransitController.UpdateTimers();
            }
        }

        if (packet.Type == EventControllerEventPacket.EEventType.RemoveItem)
        {
            var runddansController = (ClientRunddansController)Singleton<GameWorld>.Instance.RunddansController;
            if (runddansController != null)
            {
                if (_coopHandler.Players.TryGetValue(packet.NetId, out var player))
                {
                    if (!runddansController.method_5(player, out var item))
                    {
                        _logger.LogError("Could not find item to remove on player");
                    }

                    if (!runddansController.method_10(item))
                    {
                        _logger.LogError("Remove consumable error");
                    }
                }
            }
        }

        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld != null && gameWorld.RunddansController is ClientRunddansController)
        {
#if DEBUG
            _logger.LogInfo($"Received event: {packet.Event}");
#endif
            (packet.Event as RunddansStateEvent).PlayerId = MyPlayer.NetId;
            packet.Event.Invoke();
        }
    }

    private void OnInraidQuestPacketReceived(InRaidQuestPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var player))
        {
            if (player.AbstractQuestControllerClass is ObservedQuestController controller)
            {
                controller.HandleInraidQuestPacket(packet);
            }
        }
    }

    public void OnCharacterSyncPacketReceived(CharacterSyncPacket packet)
    {
        _missingIds.Clear();
        if (_coopHandler == null)
        {
            return;
        }

        if (packet.PlayerIds == null)
        {
            return;
        }

        _coopHandler.CheckIds(packet.PlayerIds, _missingIds);

        if (_missingIds.Count > 0)
        {
            RequestPacket request = new()
            {
                Type = ERequestSubPacketType.CharacterSync,
                RequestSubPacket = new RequestSubPackets.RequestCharactersPacket(_missingIds)
            };

            SendData(ref request, DeliveryMethod.ReliableOrdered);
        }
    }

    private void OnRequestPacketReceived(RequestPacket packet)
    {
        packet.RequestSubPacket.HandleResponse();
    }

    private void OnWorldPacketReceived(WorldPacket packet)
    {
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null)
        {
            _logger.LogError("OnNewWorldPacketReceived: GameWorld was null!");
            return;
        }

        /*if (packet.RagdollPackets != null)
		{
			for (int i = 0; i < packet.RagdollPackets.Count; i++)
			{
				RagdollPacketStruct ragdollPacket = packet.RagdollPackets[i];
				if (gameWorld.ObservedPlayersCorpses.TryGetValue(ragdollPacket.Id, out ObservedCorpse corpse) && corpse.HasRagdoll)
				{
					corpse.ApplyNetPacket(ragdollPacket);
					if (ragdollPacket.Done && !corpse.IsVisible())
					{
						corpse.ForceApplyTransformSync(ragdollPacket.TransformSyncs);
					}
				}
			}
		}*/

        if (packet.ArtilleryPackets.Count > 0)
        {
            var packets = packet.ArtilleryPackets;
            gameWorld.ClientShellingController.SyncProjectilesStates(ref packets);
        }

        for (var i = 0; i < packet.GrenadePackets.Count; i++)
        {
            var throwablePacket = packet.GrenadePackets[i];
            var grenades = gameWorld.Grenades;
            if (grenades.TryGetByKey(throwablePacket.Id, out var throwable))
            {
                throwable.ApplyNetPacket(throwablePacket);
            }
        }

        FikaClientWorld.SyncObjectPackets.AddRange(packet.SyncObjectPackets);
        FikaClientWorld.LootSyncPackets.AddRange(packet.LootSyncStructs);

        packet.Flush();
    }

    private void OnSideEffectPacketReceived(SideEffectPacket packet)
    {
#if DEBUG
        _logger.LogWarning("OnSideEffectPacketReceived: Received");
#endif
        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null)
        {
            _logger.LogError("OnSideEffectPacketReceived: GameWorld was null!");
            return;
        }

        var gstruct2 = gameWorld.FindItemById(packet.ItemId);
        if (gstruct2.Failed)
        {
            _logger.LogError("OnSideEffectPacketReceived: " + gstruct2.Error);
            return;
        }
        var item = gstruct2.Value;
        if (item.TryGetItemComponent(out SideEffectComponent sideEffectComponent))
        {
            sideEffectComponent.Value = packet.Value;
            item.RaiseRefreshEvent(false, false);
            return;
        }
        _logger.LogError("OnSideEffectPacketReceived: SideEffectComponent was not found!");
    }

    private void OnLoadingProfilePacketReceived(LoadingProfilePacket packet)
    {
        if (packet.Profiles != null)
        {
#if DEBUG
            _logger.LogWarning($"OnLoadingProfilePacketReceived: Received {packet.Profiles.Count} profiles");
#endif
            FikaBackendUtils.AddPartyMembers(packet.Profiles);
            return;
        }

        _logger.LogWarning("OnLoadingProfilePacketReceived: Profiles was null!");
    }

    private void OnPingPacketReceived(PingPacket packet)
    {
        if (FikaPlugin.UsePingSystem.Value)
        {
            PingFactory.ReceivePing(packet.PingLocation, packet.PingType, packet.PingColor, packet.Nickname, packet.LocaleId);
        }
    }

    private void OnBotStatePacketReceived(BotStatePacket packet)
    {
        if (_coopHandler == null)
        {
            return;
        }

        if (_coopHandler.Players.TryGetValue(packet.NetId, out var bot))
        {
            switch (packet.Type)
            {
                case BotStatePacket.EStateType.DisposeBot:
                    {
                        if (!bot.gameObject.activeSelf)
                        {
                            bot.gameObject.SetActive(true);
                        }

                        if (_coopHandler.Players.Remove(packet.NetId))
                        {
                            bot.Dispose();
                            AssetPoolObject.ReturnToPool(bot.gameObject, true);
#if DEBUG
                            _logger.LogInfo("Disposing bot: " + packet.NetId);
#endif
                        }
                        else
                        {
                            _logger.LogWarning("Unable to dispose of bot: " + packet.NetId);
                        }
                    }
                    break;
                case BotStatePacket.EStateType.EnableBot:
                    {
                        if (!bot.gameObject.activeSelf)
                        {
#if DEBUG
                            _logger.LogWarning("Enabling " + packet.NetId);
#endif
                            bot.gameObject.SetActive(true);
                        }
                        else
                        {
                            _logger.LogWarning($"Received packet to enable {bot.ProfileId}, netId {packet.NetId} but the bot was already enabled!");
                        }
                    }
                    break;
                case BotStatePacket.EStateType.DisableBot:
                    {
                        if (bot.gameObject.activeSelf)
                        {
#if DEBUG
                            _logger.LogWarning("Disabling " + packet.NetId);
#endif
                            bot.gameObject.SetActive(false);
                        }
                        else
                        {
                            _logger.LogWarning($"Received packet to disable {bot.ProfileId}, netId {packet.NetId} but the bot was already disabled!");
                        }
                    }
                    break;
            }
        }
    }

    private void OnTransitEventPacketReceived(TransitEventPacket packet)
    {
        if (!(packet.EventType is TransitEventPacket.ETransitEventType.Init or TransitEventPacket.ETransitEventType.Extract))
        {
            packet.TransitEvent.Invoke();
            return;
        }

        if (packet.EventType is TransitEventPacket.ETransitEventType.Init)
        {
            if (Singleton<GameWorld>.Instance.TransitController is ClientTransitController transitController)
            {
                transitController.Init();
                return;
            }
        }

        if (packet.EventType is TransitEventPacket.ETransitEventType.Extract)
        {
            if (Singleton<GameWorld>.Instance.TransitController is ClientTransitController transitController)
            {
                transitController.HandleClientExtract(packet.TransitId, packet.PlayerId);
                return;
            }
        }

        _logger.LogError("OnTransitEventPacketReceived: TransitController was not FikaClientTransitController!");
    }

    private void OnSyncTransitControllersPacketReceived(SyncTransitControllersPacket packet)
    {
        var transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController != null)
        {
            transitController.summonedTransits[packet.ProfileId] = new(packet.RaidId, packet.Count, packet.Maps, packet.Events);
            return;
        }

        _logger.LogError("OnSyncTransitControllersPacketReceived: TransitController was null!");
    }

    private void OnSpawnItemPacketReceived(SpawnItemPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var playerToApply))
        {
            FikaGlobals.SpawnItemInWorld(packet.Item, playerToApply);
        }
    }

    private void OnNetworkSettingsPacketReceived(NetworkSettingsPacket packet)
    {
        _logger.LogInfo($"Received settings from server. SendRate: {packet.SendRate}, NetId: {packet.NetId}, AllowVOIP: {packet.AllowVOIP}");
        _sendRate = packet.SendRate;
        NetId = packet.NetId;
        AllowVOIP = packet.AllowVOIP;

        _genericPacket.NetId = packet.NetId;

        LoadingScreenUI.Instance.AddPlayer(NetId, FikaBackendUtils.PMCName);
        var loadingPacket = new LoadingScreenPlayersPacket
        {
            NetIds = [NetId],
            Nicknames = [FikaBackendUtils.PMCName]
        };
        SendData(ref loadingPacket, DeliveryMethod.ReliableUnordered, true);
    }

    private void OnResyncInventoryIdPacketReceived(ResyncInventoryIdPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var playerToApply))
        {
            if (playerToApply is ObservedPlayer observedPlayer)
            {
                if (observedPlayer.InventoryController is ObservedInventoryController observedController)
                {
                    observedController.SetNewID(packet.MongoId);
                }
                return;
            }

            if (playerToApply.IsYourPlayer)
            {
                ResyncInventoryIdPacket response = new(playerToApply.NetId)
                {
                    MongoId = playerToApply.InventoryController.CurrentId
                };
                SendData(ref response, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }

    private void OnBufferZonePacketReceived(BufferZonePacket packet)
    {
        switch (packet.Status)
        {
            case EFT.BufferZone.EBufferZoneData.Availability:
            case EFT.BufferZone.EBufferZoneData.DisableByZryachiyDead:
            case EFT.BufferZone.EBufferZoneData.DisableByPlayerDead:
                {
                    BufferZoneControllerClass.Instance.SetInnerZoneAvailabilityStatus(packet.Available, packet.Status);
                }
                break;
            case EFT.BufferZone.EBufferZoneData.PlayerAccessStatus:
                {
                    BufferZoneControllerClass.Instance.SetPlayerAccessStatus(packet.ProfileId, packet.Available);
                }
                break;
            case EFT.BufferZone.EBufferZoneData.PlayerInZoneStatusChange:
                {
                    BufferZoneControllerClass.Instance.SetPlayerInZoneStatus(packet.ProfileId, packet.Available);
                }
                break;
            default:
                break;
        }
    }

    private void OnFlareSuccessPacketReceived(FlareSuccessPacket packet)
    {
        if (Singleton<GameWorld>.Instance.MainPlayer.ProfileId == packet.ProfileId)
        {
            if (!packet.Success)
            {
                NotificationManagerClass.DisplayNotification(new GClass2551("AirplaneDelayMessage".Localized(null),
                            ENotificationDurationType.Default, ENotificationIconType.Default, null));
            }
        }
    }

    private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var playerToApply))
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld.BtrController != null && gameWorld.BtrController.BtrView != null)
            {
                if (packet.IsResponse && packet.Status != EBtrInteractionStatus.Confirmed)
                {
                    if (packet.Status - EBtrInteractionStatus.Blacklisted <= 2 && playerToApply.IsYourPlayer)
                    {
                        GlobalEventHandlerClass.CreateEvent<BtrNotificationInteractionMessageEvent>().Invoke(playerToApply.PlayerId, packet.Status);
                        return;
                    }

                    return;
                }

                gameWorld.BtrController.BtrView.Interaction(playerToApply, packet.Data);
            }
        }
    }

    private void OnSpawnSyncObjectPacketReceived(SpawnSyncObjectPacket packet)
    {
        packet.SubPacket.Execute();
    }

    private void OnReconnectPacketReceived(ReconnectPacket packet)
    {
        if (!packet.IsRequest)
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame == null)
            {
                return;
            }

            switch (packet.Type)
            {
                case ReconnectPacket.EReconnectDataType.Throwable:
                    if (packet.ThrowableData != null)
                    {
#if DEBUG
                        _logger.LogWarning("Received reconnect packet for throwables: " + packet.ThrowableData.Count);
#endif
                        var localizedString = LocaleUtils.UI_SYNC_THROWABLES.Localized();
                        fikaGame.GameController.GameInstance.SetMatchmakerStatus(localizedString);
                        Singleton<GameWorld>.Instance.OnSmokeGrenadesDeserialized(packet.ThrowableData);
                    }
                    break;
                case ReconnectPacket.EReconnectDataType.Interactives:
                    {
                        if (packet.InteractivesData != null)
                        {
#if DEBUG
                            _logger.LogWarning("Received reconnect packet for interactives: " + packet.InteractivesData.Count);
#endif
                            var localizedString = LocaleUtils.UI_SYNC_INTERACTABLES.Localized();
                            var worldInteractiveObjects = Traverse.Create(Singleton<GameWorld>.Instance.World_0).Field<WorldInteractiveObject[]>("worldInteractiveObject_0").Value;
                            Dictionary<int, WorldInteractiveObject.WorldInteractiveDataPacketStruct> netIdDictionary = [];
                            {
                                foreach (var data in packet.InteractivesData)
                                {
                                    netIdDictionary.Add(data.NetId, data);
                                }
                            }

                            float total = packet.InteractivesData.Count;
                            var progress = 0f;
                            foreach (var item in worldInteractiveObjects)
                            {
                                if (netIdDictionary.TryGetValue(item.NetId, out var value))
                                {
                                    progress++;
                                    fikaGame.GameController.GameInstance.SetMatchmakerStatus(localizedString, progress / total);
                                    item.SetInitialSyncState(value);
                                }
                            }
                        }
                        break;
                    }
                case ReconnectPacket.EReconnectDataType.LampControllers:
                    {
                        if (packet.LampStates != null)
                        {
#if DEBUG
                            _logger.LogWarning("Received reconnect packet for lampStates: " + packet.LampStates.Count);
#endif
                            var localizedString = LocaleUtils.UI_SYNC_LAMP_STATES.Localized();
                            var lampControllerDictionary = LocationScene.GetAllObjects<LampController>(true)
                                                    .Where(FikaGlobals.LampControllerNetIdNot0)
                                                    .ToDictionary(FikaGlobals.LampControllerGetNetId);

                            float total = packet.LampStates.Count;
                            var progress = 0f;
                            foreach (var lampState in packet.LampStates)
                            {
                                progress++;
                                fikaGame.GameController.GameInstance.SetMatchmakerStatus(localizedString, progress / total);
                                if (lampControllerDictionary.TryGetValue(lampState.Key, out var lampController))
                                {
                                    if (lampController.LampState != (Turnable.EState)lampState.Value)
                                    {
                                        lampController.Switch((Turnable.EState)lampState.Value);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case ReconnectPacket.EReconnectDataType.Windows:
                    {
#if DEBUG
                        _logger.LogWarning("Received reconnect packet for windowBreakers: " + packet.WindowBreakerStates.Count);
#endif
                        if (packet.WindowBreakerStates != null)
                        {
                            var windowBreakerStates = packet.WindowBreakerStates;
                            var localizedString = LocaleUtils.UI_SYNC_WINDOWS.Localized();

                            float total = packet.WindowBreakerStates.Count;
                            var progress = 0f;
                            foreach (var windowBreaker in LocationScene.GetAllObjects<WindowBreaker>(true)
                                .Where(FikaGlobals.WindowBreakerAvailableToSync))
                            {
                                if (windowBreakerStates.TryGetValue(windowBreaker.NetId, out var hitPosition))
                                {
                                    progress++;

                                    fikaGame.GameController.GameInstance.SetMatchmakerStatus(localizedString, progress / total);
                                    try
                                    {
                                        DamageInfoStruct damageInfo = default;
                                        damageInfo.HitPoint = hitPosition;
                                        windowBreaker.MakeHit(in damageInfo, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError("OnReconnectPacketReceived: Exception caught while setting up WindowBreakers: " + ex.Message);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case ReconnectPacket.EReconnectDataType.OwnCharacter:
#if DEBUG
                    _logger.LogWarning("Received reconnect packet for own player");
#endif
                    fikaGame.GameController.GameInstance.SetMatchmakerStatus(LocaleUtils.UI_RECEIVE_OWN_PLAYERS.Localized());
                    if (fikaGame is CoopGame coopGame)
                    {
                        coopGame.Profile_0 = packet.Profile;
                        coopGame.Profile_0.Health = packet.ProfileHealthClass;
                    }
                    FikaBackendUtils.ReconnectPosition = packet.PlayerPosition;
                    break;
                case ReconnectPacket.EReconnectDataType.Finished:
                    fikaGame.GameController.GameInstance.SetMatchmakerStatus(LocaleUtils.UI_FINISH_RECONNECT.Localized());
                    ReconnectDone = true;
                    break;
                default:
                    break;
            }
        }
    }

    private void OnWorldLootPacketReceived(WorldLootPacket packet)
    {
        var fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame != null)
        {
            using var eftReader = PacketToEFTReaderAbstractClass.Get(packet.Data);
            var lootData = eftReader.ReadEFTLootDataDescriptor();
            var lootItems = EFTItemSerializerClass.DeserializeLootData(lootData);
#if RELEASE
            if (lootItems.Count < 1)
            {
                throw new NullReferenceException("LootItems length was less than 1! Something probably went very wrong");
            }
#endif
            fikaGame.GameController.LootItems = lootItems;
            (fikaGame.GameController as ClientGameController).HasReceivedLoot = true;
        }
    }

    private void OnStatisticsPacketReceived(StatisticsPacket packet)
    {
        ServerFPS = packet.ServerFPS;
    }

    private void OnInteractableInitPacketReceived(InteractableInitPacket packet)
    {
        if (!packet.IsRequest)
        {
            var world = Singleton<GameWorld>.Instance.World_0;
            if (world.Interactables == null)
            {
                world.method_0(packet.Interactables);
                var fikaGame = Singleton<IFikaGame>.Instance;
                if (fikaGame != null)
                {
                    (fikaGame.GameController as ClientGameController).InteractablesInitialized = true;
                }
            }
        }
    }

    private void OnQuestDropItemPacketReceived(QuestDropItemPacket packet)
    {
        if (MyPlayer == null)
        {
            return;
        }

        if (MyPlayer.HealthController.IsAlive)
        {
            if (MyPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.ReceiveQuestDropItemPacket(packet);
            }
        }
    }

    private void OnQuestItemPacketReceived(QuestItemPacket packet)
    {
        if (MyPlayer == null)
        {
            return;
        }

        if (MyPlayer.HealthController.IsAlive)
        {
            if (MyPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.ReceiveQuestItemPacket(packet);
            }
        }
    }

    private void OnQuestConditionPacketReceived(QuestConditionPacket packet)
    {
        if (MyPlayer == null)
        {
            return;
        }

        if (MyPlayer.HealthController.IsAlive)
        {
            if (MyPlayer.AbstractQuestControllerClass is ClientSharedQuestController sharedQuestController)
            {
                sharedQuestController.ReceiveQuestPacket(packet);
            }
        }
    }

    private void OnTextMessagePacketReceived(TextMessagePacket packet)
    {
        _logger.LogInfo($"Received message from: {packet.Nickname}, Message: {packet.Message}");

        if (_fikaChat != null)
        {
            _fikaChat.AddMessage(new(packet.Nickname, packet.Message), true);
            _fikaChat.OpenChat(true);
        }
    }

    private void OnOperationCallbackPacketReceived(OperationCallbackPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var player) && player.IsYourPlayer)
        {
            player.HandleCallbackFromServer(packet);
        }
    }

    private void OnGenericPacketReceived(GenericPacket packet)
    {
        if (packet.Type is EGenericSubPacketType.InventoryOperation)
        {
            OnInventoryPacketReceived((InventoryPacket)packet.SubPacket);
            return;
        }
        if (packet.Type is EGenericSubPacketType.OperationCallback)
        {
            OnOperationCallbackPacketReceived((OperationCallbackPacket)packet.SubPacket);
            return;
        }
        packet.Execute();
    }

    private void OnInformationPacketReceived(InformationPacket packet)
    {
        if (_coopHandler != null)
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame != null)
            {
                fikaGame.GameController.RaidStarted = packet.RaidStarted;
                if (packet.HostReady)
                {
                    fikaGame.GameController.SetClientTime(packet.GameTime, packet.SessionTime, packet.GameDateTime);
                }
            }
        }
        ReadyClients = packet.ReadyPlayers;
        HostReady = packet.HostReady;
        HostLoaded = packet.HostLoaded;
        HostReceivedLocation = packet.HostReceivedLocation;
        if (packet.AmountOfPeers > 0)
        {
            Singleton<IFikaNetworkManager>.Instance.PlayerAmount = packet.AmountOfPeers;
        }
    }

    private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var playerToApply))
        {
            packet.Execute(playerToApply);
        }
    }

    private void OnInventoryPacketReceived(InventoryPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var playerToApply))
        {
            HandleInventoryPacket(packet, playerToApply);
        }
    }

    private void OnWeaponPacketReceived(WeaponPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out var playerToApply))
        {
            packet.Execute(playerToApply);
        }
    }

    private void OnHalloweenEventPacketReceived(HalloweenEventPacket packet)
    {
        var controller = HalloweenEventControllerClass.Instance;

        if (controller == null)
        {
            _logger.LogError("OnHalloweenEventPacketReceived: controller was null!");
            return;
        }

        if (packet.SyncEvent == null)
        {
            _logger.LogError("OnHalloweenEventPacketReceived: event was null!");
            return;
        }

        packet.SyncEvent.Invoke();
    }

    /// <summary>
    /// Used by server to notify of important events, usually errors
    /// </summary>
    /// <param name="packet">The packet containing the data</param>
    private void OnMessagePacketReceived(MessagePacket packet)
    {
        NotificationManagerClass.DisplayMessageNotification(packet.Message.Localized(), packet.NotificationDurationType,
            packet.NotificationIconType, packet.Color);
    }
}
