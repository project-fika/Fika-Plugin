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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fika.Core.Networking;
public partial class FikaClient
{
    private void OnSyncTrapsPacketReceived(SyncTrapsPacket packet)
    {
        GClass1359 reader = new(packet.Data);
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
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
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld != null && gameWorld.RunddansController is ClientRunddansController clientController)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer player))
            {
                clientController.DestroyItem(player);
            }
        }
    }

    private void OnEventControllerEventPacketReceived(EventControllerEventPacket packet)
    {
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld != null && gameWorld.RunddansController is ClientRunddansController)
        {
            (packet.Event as RunddansStateEvent).PlayerId = MyPlayer.NetId;
            packet.Event.Invoke();
        }
    }

    private void OnInraidQuestPacketReceived(InRaidQuestPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer player))
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
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
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
            List<ArtilleryPacketStruct> packets = packet.ArtilleryPackets;
            gameWorld.ClientShellingController.SyncProjectilesStates(ref packets);
        }

        for (int i = 0; i < packet.GrenadePackets.Count; i++)
        {
            GrenadeDataPacketStruct throwablePacket = packet.GrenadePackets[i];
            GClass816<int, Throwable> grenades = gameWorld.Grenades;
            if (grenades.TryGetByKey(throwablePacket.Id, out Throwable throwable))
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
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null)
        {
            _logger.LogError("OnSideEffectPacketReceived: GameWorld was null!");
            return;
        }

        GStruct461<Item> gstruct2 = gameWorld.FindItemById(packet.ItemId);
        if (gstruct2.Failed)
        {
            _logger.LogError("OnSideEffectPacketReceived: " + gstruct2.Error);
            return;
        }
        Item item = gstruct2.Value;
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

        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer bot))
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
        TransitControllerAbstractClass transitController = Singleton<GameWorld>.Instance.TransitController;
        if (transitController != null)
        {
            transitController.summonedTransits[packet.ProfileId] = new(packet.RaidId, packet.Count, packet.Maps, false);
            return;
        }

        _logger.LogError("OnSyncTransitControllersPacketReceived: TransitController was null!");
    }

    private void OnSpawnItemPacketReceived(SpawnItemPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
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
    }

    private void OnResyncInventoryIdPacketReceived(ResyncInventoryIdPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
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
                NotificationManagerClass.DisplayNotification(new GClass2380("AirplaneDelayMessage".Localized(null),
                            ENotificationDurationType.Default, ENotificationIconType.Default, null));
            }
        }
    }

    private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
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
            IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
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
                        string localizedString = LocaleUtils.UI_SYNC_THROWABLES.Localized();
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
                            string localizedString = LocaleUtils.UI_SYNC_INTERACTABLES.Localized();
                            WorldInteractiveObject[] worldInteractiveObjects = Traverse.Create(Singleton<GameWorld>.Instance.World_0).Field<WorldInteractiveObject[]>("worldInteractiveObject_0").Value;
                            Dictionary<int, WorldInteractiveObject.WorldInteractiveDataPacketStruct> netIdDictionary = [];
                            {
                                foreach (WorldInteractiveObject.WorldInteractiveDataPacketStruct data in packet.InteractivesData)
                                {
                                    netIdDictionary.Add(data.NetId, data);
                                }
                            }

                            float total = packet.InteractivesData.Count;
                            float progress = 0f;
                            foreach (WorldInteractiveObject item in worldInteractiveObjects)
                            {
                                if (netIdDictionary.TryGetValue(item.NetId, out WorldInteractiveObject.WorldInteractiveDataPacketStruct value))
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
                            string localizedString = LocaleUtils.UI_SYNC_LAMP_STATES.Localized();
                            Dictionary<int, LampController> lampControllerDictionary = LocationScene.GetAllObjects<LampController>(true)
                                                    .Where(FikaGlobals.LampControllerNetIdNot0)
                                                    .ToDictionary(FikaGlobals.LampControllerGetNetId);

                            float total = packet.LampStates.Count;
                            float progress = 0f;
                            foreach (KeyValuePair<int, byte> lampState in packet.LampStates)
                            {
                                progress++;
                                fikaGame.GameController.GameInstance.SetMatchmakerStatus(localizedString, progress / total);
                                if (lampControllerDictionary.TryGetValue(lampState.Key, out LampController lampController))
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
                            Dictionary<int, Vector3> windowBreakerStates = packet.WindowBreakerStates;
                            string localizedString = LocaleUtils.UI_SYNC_WINDOWS.Localized();

                            float total = packet.WindowBreakerStates.Count;
                            float progress = 0f;
                            foreach (WindowBreaker windowBreaker in LocationScene.GetAllObjects<WindowBreaker>(true)
                                .Where(FikaGlobals.WindowBreakerAvailableToSync))
                            {
                                if (windowBreakerStates.TryGetValue(windowBreaker.NetId, out Vector3 hitPosition))
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
        IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
        if (fikaGame != null)
        {
            using GClass1278 eftReader = PacketToEFTReaderAbstractClass.Get(packet.Data);
            GClass1782 lootData = eftReader.ReadEFTLootDataDescriptor();
            GClass1399 lootItems = EFTItemSerializerClass.DeserializeLootData(lootData);
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
            World world = Singleton<GameWorld>.Instance.World_0;
            if (world.Interactables == null)
            {
                world.method_0(packet.Interactables);
                IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
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
                sharedQuestController.ReceiveQuestDropItemPacket(ref packet);
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
                sharedQuestController.ReceiveQuestItemPacket(ref packet);
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
                sharedQuestController.ReceiveQuestPacket(ref packet);
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
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer player) && player.IsYourPlayer)
        {
            player.HandleCallbackFromServer(packet);
        }
    }

    private void OnSendCharacterPacketReceived(SendCharacterPacket packet)
    {
        if (_coopHandler == null)
        {
            return;
        }

        if (packet.PlayerInfoPacket.Profile.ProfileId != _myProfileId)
        {
            _coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
                         packet.PlayerInfoPacket.ControllerId, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
                         packet.PlayerInfoPacket.ControllerType, packet.PlayerInfoPacket.ItemId);
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
            IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame != null)
            {
                fikaGame.GameController.RaidStarted = packet.RaidStarted;
                if (packet.HostReady)
                {
                    fikaGame.GameController.SetClientTime(packet.GameTime, packet.SessionTime);
                }
            }
        }
        ReadyClients = packet.ReadyPlayers;
        HostReady = packet.HostReady;
        HostLoaded = packet.HostLoaded;
        if (packet.AmountOfPeers > 0)
        {
            Singleton<IFikaNetworkManager>.Instance.PlayerAmount = packet.AmountOfPeers;
        }
    }

    private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            packet.Execute(playerToApply);
        }
    }

    private void OnInventoryPacketReceived(InventoryPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            HandleInventoryPacket(packet, playerToApply);
        }
    }

    private void OnWeaponPacketReceived(WeaponPacket packet)
    {
        if (_coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer playerToApply))
        {
            packet.Execute(playerToApply);
        }
    }

    private void OnHalloweenEventPacketReceived(HalloweenEventPacket packet)
    {
        HalloweenEventControllerClass controller = HalloweenEventControllerClass.Instance;

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
}
