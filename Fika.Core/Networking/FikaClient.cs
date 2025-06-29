// © 2025 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using Dissonance;
using Dissonance.Integrations.MirrorIgnorance;
using EFT;
using EFT.AssetsManager;
using EFT.Communications;
using EFT.GlobalEvents;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.Vehicle;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Patches.VOIP;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Jobs;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.VOIP;
using Fika.Core.Utils;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using static Fika.Core.Networking.NetworkUtils;

namespace Fika.Core.Networking
{
    /// <summary>
    /// Client used to communicate with the <see cref="FikaServer"/>
    /// </summary>
    public class FikaClient : MonoBehaviour, INetEventListener, IFikaNetworkManager
    {
        public CoopPlayer MyPlayer;
        public int Ping;
        public int ServerFPS;
        public int ReadyClients;
        public bool HostReady;
        public bool HostLoaded;
        public bool ReconnectDone;
        public NetPeer ServerConnection { get; private set; }
        public NetManager NetClient
        {
            get
            {
                return _netClient;
            }
        }
        public NetDataWriter Writer
        {
            get
            {
                return _dataWriter;
            }
        }
        public bool Started
        {
            get
            {
                if (_netClient == null)
                {
                    return false;
                }
                return _netClient.IsRunning;
            }
        }
        public int SendRate
        {
            get
            {
                return _sendRate;
            }
        }
        public CoopHandler CoopHandler
        {
            get
            {
                return _coopHandler;
            }
            set
            {
                _coopHandler = value;
            }
        }

        internal FikaVOIPClient VOIPClient { get; set; }

        public int NetId { get; set; }
        public FikaClientWorld FikaClientWorld { get; set; }
        public ESideType RaidSide { get; set; }
        public bool AllowVOIP { get; set; }
        public List<ObservedCoopPlayer> ObservedCoopPlayers { get; set; }

        private NetPacketProcessor _packetProcessor;
        private int _sendRate;
        private NetManager _netClient;
        private CoopHandler _coopHandler;
        private ManualLogSource _logger;
        private NetDataWriter _dataWriter;
        private FikaChat _fikaChat;
        private string _myProfileId;
        private Queue<BaseInventoryOperationClass> _inventoryOperations;
        private List<int> _missingIds;
        private JobHandle _stateHandle;
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<PlayerStatePacket> _snapshots;
        private int _snapshotCount;

        public async void Init()
        {
            _netClient = new(this)
            {
                UnconnectedMessagesEnabled = true,
                UpdateTime = 50,
                NatPunchEnabled = false,
                IPv6Enabled = false,
                DisconnectTimeout = FikaPlugin.ConnectionTimeout.Value * 1000,
                UseNativeSockets = FikaPlugin.NativeSockets.Value,
                EnableStatistics = true,
                MaxConnectAttempts = 5,
                ReconnectDelay = 1 * 1000,
                ChannelsCount = 2
            };

            _packetProcessor = new();
            _dataWriter = new();
            _logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Client");
            _inventoryOperations = new();
            _missingIds = [];
            _snapshotCount = 0;
            _snapshots = new(512, Allocator.Persistent);
            ObservedCoopPlayers = [];

            Ping = 0;
            ServerFPS = 0;
            ReadyClients = 0;

            NetworkGameSession.Rtt = 0;
            NetworkGameSession.LossPercent = 0;

            _myProfileId = FikaBackendUtils.Profile.ProfileId;

            RegisterPacketsAndTypes();

#if DEBUG
            AddDebugPackets();
#endif            

            await NetManagerUtils.CreateCoopHandler();

            if (FikaBackendUtils.IsHostNatPunch)
            {
                NetManagerUtils.DestroyPingingClient();
                _netClient.Start(FikaBackendUtils.LocalPort); // NAT punching has to re-use the same local port
            }
            else
            {
                _netClient.Start();
            }

            string ip = FikaBackendUtils.RemoteIp;
            int port = FikaBackendUtils.RemotePort;
            string connectString = FikaBackendUtils.IsReconnect ? "fika.reconnect" : "fika.core";

            if (string.IsNullOrEmpty(ip))
            {
                Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", "Unable to connect to the raid server. IP and/or Port was empty when requesting data!");
            }
            else
            {
                ServerConnection = _netClient.Connect(ip, port, connectString);
            }
        }

        async Task IFikaNetworkManager.InitializeVOIP()
        {
            VoipSettingsClass voipHandler = FikaGlobals.VOIPHandler;

            GClass1068 controller = Singleton<SharedGameSettingsClass>.Instance.Sound.Controller;
            if (voipHandler.MicrophoneChecked)
            {
                controller.ResetVoipDisabledReason();
                DissonanceComms.ClientPlayerId = FikaGlobals.GetProfile(RaidSide == ESideType.Savage).ProfileId;
                await GClass1640.LoadScene(AssetsManagerSingletonClass.Manager,
                    SceneResourceKeyAbstractClass.DissonanceSetupScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);

                MirrorIgnoranceCommsNetwork mirrorCommsNetwork;
                do
                {
                    mirrorCommsNetwork = FindObjectOfType<MirrorIgnoranceCommsNetwork>();
                    await Task.Yield();
                } while (mirrorCommsNetwork == null);

                GameObject gameObj = mirrorCommsNetwork.gameObject;
                FikaCommsNetwork commNet = gameObj.AddComponent<FikaCommsNetwork>();
                Destroy(mirrorCommsNetwork);

                DissonanceComms_Start_Patch.IsReady = true;
                gameObj.GetComponent<DissonanceComms>().Invoke("Start", 0);
            }
            else
            {
                controller.VoipDisabledByInitializationFail();
            }

            do
            {
                await Task.Yield();
            } while (VOIPClient == null);

            RegisterPacket<VOIPPacket>(OnVOIPPacketReceived);

            return;
        }

        private void RegisterPacketsAndTypes()
        {
            RegisterCustomType(FikaSerializationExtensions.PutRagdollStruct, FikaSerializationExtensions.GetRagdollStruct);
            RegisterCustomType(FikaSerializationExtensions.PutArtilleryStruct, FikaSerializationExtensions.GetArtilleryStruct);
            RegisterCustomType(FikaSerializationExtensions.PutGrenadeStruct, FikaSerializationExtensions.GetGrenadeStruct);
            RegisterCustomType(FikaSerializationExtensions.PutAirplaneDataPacketStruct, FikaSerializationExtensions.GetAirplaneDataPacketStruct);
            RegisterCustomType(FikaSerializationExtensions.PutLootSyncStruct, FikaSerializationExtensions.GetLootSyncStruct);

            RegisterPacket<PlayerStatePacket>(OnPlayerStatePacketReceived);
            RegisterPacket<WeaponPacket>(OnWeaponPacketReceived);
            RegisterPacket<DamagePacket>(OnDamagePacketReceived);
            RegisterPacket<ArmorDamagePacket>(OnArmorDamagePacketReceived);
            RegisterPacket<InventoryPacket>(OnInventoryPacketReceived);
            RegisterPacket<CommonPlayerPacket>(OnCommonPlayerPacketReceived);
            RegisterPacket<InformationPacket>(OnInformationPacketReceived);
            RegisterPacket<HealthSyncPacket>(OnHealthSyncPacketReceived);
            RegisterPacket<GenericPacket>(OnGenericPacketReceived);
            RegisterPacket<SendCharacterPacket>(OnSendCharacterPacketReceived);
            RegisterPacket<OperationCallbackPacket>(OnOperationCallbackPacketReceived);
            RegisterPacket<TextMessagePacket>(OnTextMessagePacketReceived);
            RegisterPacket<QuestConditionPacket>(OnQuestConditionPacketReceived);
            RegisterPacket<QuestItemPacket>(OnQuestItemPacketReceived);
            RegisterPacket<QuestDropItemPacket>(OnQuestDropItemPacketReceived);
            RegisterPacket<HalloweenEventPacket>(OnHalloweenEventPacketReceived);
            RegisterPacket<InteractableInitPacket>(OnInteractableInitPacketReceived);
            RegisterPacket<StatisticsPacket>(OnStatisticsPacketReceived);
            RegisterPacket<WorldLootPacket>(OnWorldLootPacketReceived);
            RegisterPacket<ReconnectPacket>(OnReconnectPacketReceived);
            RegisterPacket<SpawnSyncObjectPacket>(OnSpawnSyncObjectPacketReceived);
            RegisterPacket<BTRPacket>(OnBTRPacketReceived);
            RegisterPacket<BTRInteractionPacket>(OnBTRInteractionPacketReceived);
            RegisterPacket<FlareSuccessPacket>(OnFlareSuccessPacketReceived);
            RegisterPacket<BufferZonePacket>(OnBufferZonePacketReceived);
            RegisterPacket<ResyncInventoryIdPacket>(OnResyncInventoryIdPacketReceived);
            RegisterPacket<UsableItemPacket>(OnUsableItemPacketReceived);
            RegisterPacket<NetworkSettingsPacket>(OnNetworkSettingsPacketReceived);
            RegisterPacket<SyncTransitControllersPacket>(OnSyncTransitControllersPacketReceived);
            RegisterPacket<TransitEventPacket>(OnTransitEventPacketReceived);
            RegisterPacket<BotStatePacket>(OnBotStatePacketReceived);
            RegisterPacket<PingPacket>(OnPingPacketReceived);
            RegisterPacket<LoadingProfilePacket>(OnLoadingProfilePacketReceived);
            RegisterPacket<SideEffectPacket>(OnSideEffectPacketReceived);
            RegisterPacket<RequestPacket>(OnRequestPacketReceived);
            RegisterPacket<CharacterSyncPacket>(OnCharacterSyncPacketReceived);
            RegisterPacket<InraidQuestPacket>(OnInraidQuestPacketReceived);
            RegisterPacket<EventControllerEventPacket>(OnEventControllerEventPacketReceived);
            RegisterPacket<EventControllerInteractPacket>(OnEventControllerInteractPacketReceived);
            RegisterPacket<SyncTrapsPacket>(OnSyncTrapsPacketReceived);

            RegisterReusable<WorldPacket>(OnWorldPacketReceived);
        }

        private void OnSyncTrapsPacketReceived(SyncTrapsPacket packet)
        {
            GClass1358 reader = new(packet.Data);
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
                if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
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

        private void OnInraidQuestPacketReceived(InraidQuestPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
            {
                if (player.AbstractQuestControllerClass is ObservedQuestController controller)
                {
                    controller.HandleInraidQuestPacket(packet);
                }
            }
        }

        private void OnCharacterSyncPacketReceived(CharacterSyncPacket packet)
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
                    Type = SubPacket.ERequestSubPacketType.CharacterSync,
                    RequestSubPacket = new RequestSubPackets.RequestCharactersPacket(_missingIds)
                };

                SendData(ref request, DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnVOIPPacketReceived(VOIPPacket packet)
        {
            VOIPClient.NetworkReceivedPacket(new(packet.Data));
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
                GClass815<int, Throwable> grenades = gameWorld.Grenades;
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

            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer bot))
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
                if (Singleton<GameWorld>.Instance.TransitController is FikaClientTransitController transitController)
                {
                    transitController.Init();
                    return;
                }
            }

            if (packet.EventType is TransitEventPacket.ETransitEventType.Extract)
            {
                if (Singleton<GameWorld>.Instance.TransitController is FikaClientTransitController transitController)
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

#if DEBUG
        private void AddDebugPackets()
        {
            RegisterPacket<SpawnItemPacket>(OnSpawnItemPacketReceived);
        }
#endif

        private void OnSpawnItemPacketReceived(SpawnItemPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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
        }

        private void OnUsableItemPacketReceived(UsableItemPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleUsableItemPacket(packet);
            }
        }

        private void OnResyncInventoryIdPacketReceived(ResyncInventoryIdPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply is ObservedCoopPlayer observedPlayer)
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
                    SendData(ref response, DeliveryMethod.ReliableOrdered);
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
                    NotificationManagerClass.DisplayNotification(new GClass2379("AirplaneDelayMessage".Localized(null),
                                ENotificationDurationType.Default, ENotificationIconType.Default, null));
                }
            }
        }

        private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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
                using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(packet.Data);
                GClass1780 lootData = eftReader.ReadEFTLootDataDescriptor();
                GClass1398 lootItems = EFTItemSerializerClass.DeserializeLootData(lootData);
                if (lootItems.Count < 1)
                {
                    throw new NullReferenceException("LootItems length was less than 1! Something probably went very wrong");
                }
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
                if (MyPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
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
                if (MyPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
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
                if (MyPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
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
                _fikaChat.ReceiveMessage(packet.Nickname, packet.Message);
            }
        }

        public void SetupGameVariables(CoopPlayer coopPlayer)
        {
            MyPlayer = coopPlayer;
            if (FikaPlugin.EnableChat.Value)
            {
                _fikaChat = gameObject.AddComponent<FikaChat>();
            }
        }

        private void OnOperationCallbackPacketReceived(OperationCallbackPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player) && player.IsYourPlayer)
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
                             packet.PlayerInfoPacket.ControllerId.Value, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
                             packet.PlayerInfoPacket.ControllerType, packet.PlayerInfoPacket.ItemId);
            }
        }

        private void OnGenericPacketReceived(GenericPacket packet)
        {
            packet.SubPacket.Execute();
        }

        private void OnHealthSyncPacketReceived(HealthSyncPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply is ObservedCoopPlayer observedPlayer)
                {
                    if (packet.Packet.SyncType == NetworkHealthSyncPacketStruct.ESyncType.IsAlive && !packet.Packet.Data.IsAlive.IsAlive)
                    {
                        observedPlayer.SetAggressorData(packet.KillerId, packet.BodyPart, packet.WeaponId);
                        observedPlayer.CorpseSyncPacket = packet.CorpseSyncPacket;
                        if (packet.TriggerZones.Length > 0)
                        {
                            observedPlayer.TriggerZones.Clear();
                            foreach (string triggerZone in packet.TriggerZones)
                            {
                                observedPlayer.TriggerZones.Add(triggerZone);
                            }
                        }
                    }
                    observedPlayer.NetworkHealthController.HandleSyncPacket(packet.Packet);
                    return;
                }
                _logger.LogError($"OnHealthSyncPacketReceived::Player with id {playerToApply.NetId} was not observed. Name: {playerToApply.Profile.Nickname}");
            }
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
                FikaBackendUtils.HostExpectedNumberOfPlayers = packet.AmountOfPeers;
            }
        }

        private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
            }
        }

        private void OnInventoryPacketReceived(InventoryPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                HandleInventoryPacket(ref packet, playerToApply);
            }
        }

        private void OnDamagePacketReceived(DamagePacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply) && playerToApply.IsYourPlayer)
            {
                playerToApply.HandleDamagePacket(in packet);
            }
        }

        private void OnArmorDamagePacketReceived(ArmorDamagePacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleArmorDamagePacket(packet);
            }
        }

        private void OnWeaponPacketReceived(WeaponPacket packet)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
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

        private void OnBTRPacketReceived(BTRPacket packet)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                gameWorld.BtrController?.SyncBTRVehicleFromServer(packet.Data);
            }
        }

        private void OnPlayerStatePacketReceived(PlayerStatePacket packet)
        {
            if (_snapshotCount < _snapshots.Length)
            {
                _snapshots[_snapshotCount] = packet;
                _snapshotCount++;
            }
        }

        protected void Update()
        {
            _netClient?.PollEvents();
            _stateHandle = new UpdateInterpolators(Time.unscaledDeltaTime).Schedule(ObservedCoopPlayers.Count, 32,
                new HandlePlayerStates(NetworkTimeSync.NetworkTime, _snapshots).Schedule(_snapshotCount, 32));

            int inventoryOps = _inventoryOperations.Count;
            if (inventoryOps > 0)
            {
                if (_inventoryOperations.Peek().WaitingForForeignEvents())
                {
                    return;
                }
                _inventoryOperations.Dequeue().method_1(HandleResult);
            }
        }

        protected void LateUpdate()
        {
            _stateHandle.Complete();
            for (int i = 0; i < ObservedCoopPlayers.Count; i++)
            {
                ObservedCoopPlayers[i].ManualStateUpdate();
            }

            _snapshotCount = 0;
        }

        protected void OnDestroy()
        {
            _netClient?.Stop();
            _stateHandle.Complete();
            _snapshots.Dispose();

            if (_fikaChat != null)
            {
                Destroy(_fikaChat);
            }

            FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
        }

        public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            _dataWriter.Reset();

            _dataWriter.PutEnum(EPacketType.Serializable);
            _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
            _netClient.FirstPeer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
        }

        public void SendVOIPPacket(ref VOIPPacket packet, NetPeer peer = null)
        {
            if (packet.Data == null)
            {
                _logger.LogError("SendVOIPPacket: data was null");
                return;
            }

            SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public void SendVOIPData(ArraySegment<byte> data, NetPeer peer = null)
        {
            _dataWriter.Reset();

            _dataWriter.PutEnum(EPacketType.VOIP);
            _dataWriter.PutBytesWithLength(data.Array, data.Offset, (ushort)data.Count);
            _netClient.FirstPeer.Send(_dataWriter.AsReadOnlySpan, DeliveryMethod.Sequenced);
        }

        public void SendReusable<T>(T packet, DeliveryMethod deliveryMethod) where T : class, IReusable, new()
        {
            _dataWriter.Reset();

            _dataWriter.PutEnum(EPacketType.Serializable);
            _packetProcessor.Write(_dataWriter, packet);
            _netClient.FirstPeer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);

            packet.Flush();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.CONNECTED_TO_SERVER.Localized(), peer.Port),
                ENotificationDurationType.Default, ENotificationIconType.Friend);

            Profile ownProfile = FikaGlobals.GetLiteProfile(FikaBackendUtils.IsScav);
            if (ownProfile == null)
            {
                _logger.LogError("OnPeerConnected: Own profile was null!");
                return;
            }

            NetworkSettingsPacket settingsPacket = new()
            {
                ProfileId = ownProfile.ProfileId
            };
            SendData(ref settingsPacket, DeliveryMethod.ReliableOrdered);

            ownProfile.Info.SetProfileNickname(FikaBackendUtils.PMCName ?? ownProfile.Nickname);
            Dictionary<Profile, bool> profiles = [];
            profiles.Add(ownProfile, false);
            LoadingProfilePacket profilePacket = new()
            {
                Profiles = profiles
            };
            SendData(ref profilePacket, DeliveryMethod.ReliableOrdered);

            FikaEventDispatcher.DispatchEvent(new PeerConnectedEvent(peer, this));
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            _logger.LogError("[CLIENT] We received error " + socketErrorCode);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            switch (reader.GetEnum<EPacketType>())
            {
                case EPacketType.Serializable:
                    _packetProcessor.ReadAllPackets(reader, peer);
                    break;
                case EPacketType.VOIP:
                    VOIPClient.NetworkReceivedPacket(new(reader.GetBytesWithLength()));
                    break;
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.BasicMessage && _netClient.ConnectedPeersCount == 0 && reader.GetInt() == 1)
            {
                _logger.LogInfo("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
                _netClient.Connect(remoteEndPoint, "fika.core");
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            Ping = latency;
            NetworkGameSession.Rtt = peer.RoundTripTime;
            NetworkGameSession.LossPercent = (int)NetClient.Statistics.PacketLossPercent;
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {

        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _logger.LogInfo("[CLIENT] We disconnected because " + disconnectInfo.Reason);
            if (disconnectInfo.Reason is DisconnectReason.Timeout)
            {
                NotificationManagerClass.DisplayWarningNotification(LocaleUtils.LOST_CONNECTION.Localized());
                MyPlayer.PacketSender.DestroyThis();
                Destroy(this);
                Singleton<FikaClient>.Release(this);
            }

            if (disconnectInfo.Reason is DisconnectReason.ConnectionRejected)
            {
                string reason = disconnectInfo.AdditionalData.GetString();
                if (!string.IsNullOrEmpty(reason))
                {
                    NotificationManagerClass.DisplayWarningNotification(reason);
                    return;
                }

                _logger.LogError("OnPeerDisconnected: Rejected connection but no reason");
            }
        }

        public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new()
        {
            _packetProcessor.SubscribeNetSerializable(handle);
        }

        public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new()
        {
            _packetProcessor.SubscribeNetSerializable(handle);
        }

        public void RegisterReusable<T>(Action<T> handle) where T : class, IReusable, new()
        {
            _packetProcessor.SubscribeReusable(handle);
        }

        public void RegisterReusable<T, TUserData>(Action<T, TUserData> handle) where T : class, IReusable, new()
        {
            _packetProcessor.SubscribeReusable(handle);
        }

        public void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate)
        {
            _packetProcessor.RegisterNestedType(writeDelegate, readDelegate);
        }

        public void PrintStatistics()
        {
            _logger.LogInfo("..:: Fika Client Session Statistics ::..");
            _logger.LogInfo($"Sent packets: {_netClient.Statistics.PacketsSent}");
            _logger.LogInfo($"Sent data: {FikaGlobals.FormatFileSize(_netClient.Statistics.BytesSent)}");
            _logger.LogInfo($"Received packets: {_netClient.Statistics.PacketsReceived}");
            _logger.LogInfo($"Received data: {FikaGlobals.FormatFileSize(_netClient.Statistics.BytesReceived)}");
            _logger.LogInfo($"Packet loss: {_netClient.Statistics.PacketLossPercent}%");
        }

        private void HandleInventoryPacket(ref InventoryPacket packet, CoopPlayer player)
        {
            if (packet.OperationBytes.Length == 0)
            {
                FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Bytes were null!");
                return;
            }

            InventoryController controller = player.InventoryController;
            if (controller != null)
            {
                try
                {
                    if (controller is Interface16 networkController)
                    {
                        using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(packet.OperationBytes);
                        BaseDescriptorClass descriptor = eftReader.ReadPolymorph<BaseDescriptorClass>();
                        OperationDataStruct result = networkController.CreateOperationFromDescriptor(descriptor);
                        if (!result.Succeeded)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Unable to process descriptor from netId {packet.NetId}, error: {result.Error}");
                            return;
                        }

                        _inventoryOperations.Enqueue(result.Value);
                    }
                }
                catch (Exception exception)
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Exception thrown: {exception}");
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError("ConvertInventoryPacket: inventory was null!");
            }
        }

        private void HandleResult(IResult result)
        {
            if (result.Failed)
            {
                FikaPlugin.Instance.FikaLogger.LogError($"Error in operation: {result.Error}");
            }
        }
    }
}
