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
using Unity.Jobs;
using UnityEngine;

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
                return netClient;
            }
        }
        public NetDataWriter Writer
        {
            get
            {
                return dataWriter;
            }
        }
        public bool Started
        {
            get
            {
                if (netClient == null)
                {
                    return false;
                }
                return netClient.IsRunning;
            }
        }
        public int SendRate
        {
            get
            {
                return sendRate;
            }
        }
        public CoopHandler CoopHandler
        {
            get
            {
                return coopHandler;
            }
            set
            {
                coopHandler = value;
            }
        }

        internal FikaVOIPClient VOIPClient { get; set; }

        public int NetId { get; set; }
        public FikaClientWorld FikaClientWorld { get; set; }
        public EPlayerSide RaidSide { get; set; }
        public bool AllowVOIP { get; set; }
        public List<PlayerStatePacket> Snapshots { get; set; }
        public List<ObservedCoopPlayer> ObservedCoopPlayers { get; set; }

        private NetPacketProcessor packetProcessor;
        private int sendRate;
        private NetManager netClient;
        private CoopHandler coopHandler;
        private ManualLogSource logger;
        private NetDataWriter dataWriter;
        private FikaChat fikaChat;
        private string myProfileId;
        private Queue<BaseInventoryOperationClass> inventoryOperations;
        private List<int> missingIds;
        private JobHandle stateHandle;

        public async void Init()
        {
            netClient = new(this)
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

            packetProcessor = new();
            dataWriter = new();
            logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Client");
            inventoryOperations = new();
            missingIds = [];
            Snapshots = new(64);
            ObservedCoopPlayers = [];

            Ping = 0;
            ServerFPS = 0;
            ReadyClients = 0;

            NetworkGameSession.Rtt = 0;
            NetworkGameSession.LossPercent = 0;

            myProfileId = FikaBackendUtils.Profile.ProfileId;

            RegisterPacketsAndTypes();

#if DEBUG
            AddDebugPackets();
#endif            

            await NetManagerUtils.CreateCoopHandler();

            if (FikaBackendUtils.IsHostNatPunch)
            {
                NetManagerUtils.DestroyPingingClient();
                netClient.Start(FikaBackendUtils.LocalPort); // NAT punching has to re-use the same local port
            }
            else
            {
                netClient.Start();
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
                ServerConnection = netClient.Connect(ip, port, connectString);
            }
        }

        async Task IFikaNetworkManager.InitializeVOIP()
        {
            GClass2042 voipHandler = FikaGlobals.VOIPHandler;

            GClass1040 controller = Singleton<SharedGameSettingsClass>.Instance.Sound.Controller;
            if (voipHandler.MicrophoneChecked)
            {
                controller.ResetVoipDisabledReason();
                DissonanceComms.ClientPlayerId = FikaGlobals.GetProfile(RaidSide == EPlayerSide.Savage).ProfileId;
                await GClass1578.LoadScene(AssetsManagerSingletonClass.Manager,
                    GClass2078.DissonanceSetupScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);

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

            RegisterReusable<WorldPacket>(OnWorldPacketReceived);
        }

        private void OnCharacterSyncPacketReceived(CharacterSyncPacket packet)
        {
            missingIds.Clear();

            if (coopHandler == null)
            {
                return;
            }

            if (packet.PlayerIds == null)
            {
                return;
            }

            coopHandler.CheckIds(packet.PlayerIds, missingIds);

            if (missingIds.Count > 0)
            {
                RequestPacket request = new()
                {
                    PacketType = SubPacket.ERequestSubPacketType.CharacterSync,
                    RequestSubPacket = new RequestSubPackets.RequestCharactersPacket(missingIds)
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
                logger.LogError("OnNewWorldPacketReceived: GameWorld was null!");
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
                GClass797<int, Throwable> grenades = gameWorld.Grenades;
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
            logger.LogWarning("OnSideEffectPacketReceived: Received");
#endif
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                logger.LogError("OnSideEffectPacketReceived: GameWorld was null!");
                return;
            }

            GStruct457<Item> gstruct2 = gameWorld.FindItemById(packet.ItemId);
            if (gstruct2.Failed)
            {
                logger.LogError("OnSideEffectPacketReceived: " + gstruct2.Error);
                return;
            }
            Item item = gstruct2.Value;
            if (item.TryGetItemComponent(out SideEffectComponent sideEffectComponent))
            {
                sideEffectComponent.Value = packet.Value;
                item.RaiseRefreshEvent(false, false);
                return;
            }
            logger.LogError("OnSideEffectPacketReceived: SideEffectComponent was not found!");
        }

        private void OnLoadingProfilePacketReceived(LoadingProfilePacket packet)
        {
            if (packet.Profiles != null)
            {
#if DEBUG
                logger.LogWarning($"OnLoadingProfilePacketReceived: Received {packet.Profiles.Count} profiles");
#endif
                FikaBackendUtils.AddPartyMembers(packet.Profiles);
                return;
            }

            logger.LogWarning("OnLoadingProfilePacketReceived: Profiles was null!");
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
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer bot))
            {
                switch (packet.Type)
                {
                    case BotStatePacket.EStateType.DisposeBot:
                        {
                            if (!bot.gameObject.activeSelf)
                            {
                                bot.gameObject.SetActive(true);
                            }

                            if (coopHandler.Players.Remove(packet.NetId))
                            {
                                bot.Dispose();
                                AssetPoolObject.ReturnToPool(bot.gameObject, true);
#if DEBUG
                                logger.LogInfo("Disposing bot: " + packet.NetId);
#endif
                            }
                            else
                            {
                                logger.LogWarning("Unable to dispose of bot: " + packet.NetId);
                            }
                        }
                        break;
                    case BotStatePacket.EStateType.EnableBot:
                        {
                            if (!bot.gameObject.activeSelf)
                            {
#if DEBUG
                                logger.LogWarning("Enabling " + packet.NetId);
#endif
                                bot.gameObject.SetActive(true);
                            }
                            else
                            {
                                logger.LogWarning($"Received packet to enable {bot.ProfileId}, netId {packet.NetId} but the bot was already enabled!");
                            }
                        }
                        break;
                    case BotStatePacket.EStateType.DisableBot:
                        {
                            if (bot.gameObject.activeSelf)
                            {
#if DEBUG
                                logger.LogWarning("Disabling " + packet.NetId);
#endif
                                bot.gameObject.SetActive(false);
                            }
                            else
                            {
                                logger.LogWarning($"Received packet to disable {bot.ProfileId}, netId {packet.NetId} but the bot was already disabled!");
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
                if (coopHandler.LocalGameInstance.GameWorld_0.TransitController is FikaClientTransitController transitController)
                {
                    transitController.Init();
                    return;
                }
            }

            if (packet.EventType is TransitEventPacket.ETransitEventType.Extract)
            {
                if (coopHandler.LocalGameInstance.GameWorld_0.TransitController is FikaClientTransitController transitController)
                {
                    transitController.HandleClientExtract(packet.TransitId, packet.PlayerId);
                    return;
                }
            }

            logger.LogError("OnTransitEventPacketReceived: TransitController was not FikaClientTransitController!");
        }

        private void OnSyncTransitControllersPacketReceived(SyncTransitControllersPacket packet)
        {
            TransitControllerAbstractClass transitController = Singleton<GameWorld>.Instance.TransitController;
            if (transitController != null)
            {
                transitController.summonedTransits[packet.ProfileId] = new(packet.RaidId, packet.Count, packet.Maps, false);
                return;
            }

            logger.LogError("OnSyncTransitControllersPacketReceived: TransitController was null!");
        }

#if DEBUG
        private void AddDebugPackets()
        {
            RegisterPacket<SpawnItemPacket>(OnSpawnItemPacketReceived);
        }
#endif

        private void OnSpawnItemPacketReceived(SpawnItemPacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                FikaGlobals.SpawnItemInWorld(packet.Item, playerToApply);
            }
        }

        private void OnNetworkSettingsPacketReceived(NetworkSettingsPacket packet)
        {
            logger.LogInfo($"Received settings from server. SendRate: {packet.SendRate}, NetId: {packet.NetId}, AllowVOIP: {packet.AllowVOIP}");
            sendRate = packet.SendRate;
            NetId = packet.NetId;
            AllowVOIP = packet.AllowVOIP;
        }

        private void OnUsableItemPacketReceived(UsableItemPacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleUsableItemPacket(packet);
            }
        }

        private void OnResyncInventoryIdPacketReceived(ResyncInventoryIdPacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply is ObservedCoopPlayer observedPlayer)
                {
                    if (observedPlayer.InventoryController is ObservedInventoryController observedController)
                    {
                        observedController.SetNewID(packet.MongoId.Value);
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
                    NotificationManagerClass.DisplayNotification(new GClass2314("AirplaneDelayMessage".Localized(null),
                                ENotificationDurationType.Default, ENotificationIconType.Default, null));
                }
            }
        }

        private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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
                CoopGame coopGame = CoopGame.Instance;
                if (coopGame == null)
                {
                    return;
                }

                switch (packet.Type)
                {
                    case ReconnectPacket.EReconnectDataType.Throwable:
                        if (packet.ThrowableData != null)
                        {
#if DEBUG
                            logger.LogWarning("Received reconnect packet for throwables: " + packet.ThrowableData.Count);
#endif
                            string localizedString = LocaleUtils.UI_SYNC_THROWABLES.Localized();
                            coopGame.SetMatchmakerStatus(localizedString);
                            Singleton<GameWorld>.Instance.OnSmokeGrenadesDeserialized(packet.ThrowableData);
                        }
                        break;
                    case ReconnectPacket.EReconnectDataType.Interactives:
                        {
                            if (packet.InteractivesData != null)
                            {
#if DEBUG
                                logger.LogWarning("Received reconnect packet for interactives: " + packet.InteractivesData.Count);
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
                                        coopGame.SetMatchmakerStatus(localizedString, progress / total);
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
                                logger.LogWarning("Received reconnect packet for lampStates: " + packet.LampStates.Count);
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
                                    coopGame.SetMatchmakerStatus(localizedString, progress / total);
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
                            logger.LogWarning("Received reconnect packet for windowBreakers: " + packet.WindowBreakerStates.Count);
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

                                        coopGame.SetMatchmakerStatus(localizedString, progress / total);
                                        try
                                        {
                                            DamageInfoStruct damageInfo = default;
                                            damageInfo.HitPoint = hitPosition;
                                            windowBreaker.MakeHit(in damageInfo, true);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError("OnReconnectPacketReceived: Exception caught while setting up WindowBreakers: " + ex.Message);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case ReconnectPacket.EReconnectDataType.OwnCharacter:
#if DEBUG
                        logger.LogWarning("Received reconnect packet for own player");
#endif
                        coopGame.SetMatchmakerStatus(LocaleUtils.UI_RECEIVE_OWN_PLAYERS.Localized());
                        coopHandler.LocalGameInstance.Profile_0 = packet.Profile;
                        coopHandler.LocalGameInstance.Profile_0.Health = packet.ProfileHealthClass;
                        FikaBackendUtils.ReconnectPosition = packet.PlayerPosition;
                        break;
                    case ReconnectPacket.EReconnectDataType.Finished:
                        coopGame.SetMatchmakerStatus(LocaleUtils.UI_FINISH_RECONNECT.Localized());
                        ReconnectDone = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnWorldLootPacketReceived(WorldLootPacket packet)
        {
            CoopGame coopGame = CoopGame.Instance;
            if (coopGame != null)
            {
                using GClass1212 eftReader = GClass1215.Get(packet.Data);
                GClass1718 lootData = eftReader.ReadEFTLootDataDescriptor();
                GClass1333 lootItems = EFTItemSerializerClass.DeserializeLootData(lootData);
                if (lootItems.Count < 1)
                {
                    throw new NullReferenceException("LootItems length was less than 1! Something probably went very wrong");
                }
                coopGame.LootItems = lootItems;
                coopGame.HasReceivedLoot = true;
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
                    CoopGame coopGame = CoopGame.Instance;
                    if (coopGame != null)
                    {
                        coopGame.InteractablesInitialized = true;
                    }
                }
            }
        }

        private void OnQuestDropItemPacketReceived(QuestDropItemPacket packet)
        {
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
            logger.LogInfo($"Received message from: {packet.Nickname}, Message: {packet.Message}");

            if (fikaChat != null)
            {
                fikaChat.ReceiveMessage(packet.Nickname, packet.Message);
            }
        }

        public void SetupGameVariables(CoopPlayer coopPlayer)
        {
            MyPlayer = coopPlayer;
            if (FikaPlugin.EnableChat.Value)
            {
                fikaChat = gameObject.AddComponent<FikaChat>();
            }
        }

        private void OnOperationCallbackPacketReceived(OperationCallbackPacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player) && player.IsYourPlayer)
            {
                player.HandleCallbackFromServer(packet);
            }
        }

        private void OnSendCharacterPacketReceived(SendCharacterPacket packet)
        {
            if (coopHandler == null)
            {
                return;
            }

            if (packet.PlayerInfoPacket.Profile.ProfileId != myProfileId)
            {
                coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
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
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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
                logger.LogError($"OnHealthSyncPacketReceived::Player with id {playerToApply.NetId} was not observed. Name: {playerToApply.Profile.Nickname}");
            }
        }

        private void OnInformationPacketReceived(InformationPacket packet)
        {
            if (coopHandler != null)
            {
                CoopGame coopGame = coopHandler.LocalGameInstance;
                if (coopGame != null)
                {
                    coopGame.RaidStarted = packet.RaidStarted;
                    if (packet.HostReady)
                    {
                        coopGame.SetClientTime(packet.GameTime, packet.SessionTime);
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
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
            }
        }

        private void OnInventoryPacketReceived(InventoryPacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                HandleInventoryPacket(ref packet, playerToApply);
            }
        }

        private void OnDamagePacketReceived(DamagePacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply) && playerToApply.IsYourPlayer)
            {
                playerToApply.HandleDamagePacket(packet);
            }
        }

        private void OnArmorDamagePacketReceived(ArmorDamagePacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleArmorDamagePacket(packet);
            }
        }

        private void OnWeaponPacketReceived(WeaponPacket packet)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
            }
        }

        private void OnHalloweenEventPacketReceived(HalloweenEventPacket packet)
        {
            HalloweenEventControllerClass controller = HalloweenEventControllerClass.Instance;

            if (controller == null)
            {
                logger.LogError("OnHalloweenEventPacketReceived: controller was null!");
                return;
            }

            if (packet.SyncEvent == null)
            {
                logger.LogError("OnHalloweenEventPacketReceived: event was null!");
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
            Snapshots.Add(packet);
        }

        protected void Update()
        {
            netClient?.PollEvents();
            stateHandle = new UpdateInterpolators().Schedule(ObservedCoopPlayers.Count, 8,
                new HandlePlayerStates().Schedule(Snapshots.Count, 8));

            int inventoryOps = inventoryOperations.Count;
            if (inventoryOps > 0)
            {
                if (inventoryOperations.Peek().WaitingForForeignEvents())
                {
                    return;
                }
                inventoryOperations.Dequeue().method_1(HandleResult);
            }
        }

        protected void LateUpdate()
        {
            stateHandle.Complete();
            for (int i = 0; i < ObservedCoopPlayers.Count; i++)
            {
                ObservedCoopPlayers[i].ManualStateUpdate();
            }
            Snapshots.Clear();
        }

        protected void OnDestroy()
        {
            netClient?.Stop();

            if (fikaChat != null)
            {
                Destroy(fikaChat);
            }

            FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
        }

        public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            dataWriter.Reset();

            packetProcessor.WriteNetSerializable(dataWriter, ref packet);
            netClient.FirstPeer.Send(dataWriter, deliveryMethod);
        }

        public void SendVOIPPacket(ref VOIPPacket packet, NetPeer peer = null)
        {
            if (packet.Data == null)
            {
                logger.LogError("SendVOIPPacket: data was null");
                return;
            }

            SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public void SendVOIPData(ArraySegment<byte> data, NetPeer peer = null)
        {
            dataWriter.Reset();
            dataWriter.PutBytesWithLength(data.Array, data.Offset, (ushort)data.Count);
            netClient.FirstPeer.Send(dataWriter, 1, DeliveryMethod.Sequenced);
        }

        public void SendReusable<T>(T packet, DeliveryMethod deliveryMethod) where T : class, IReusable, new()
        {
            dataWriter.Reset();

            packetProcessor.Write(dataWriter, packet);
            netClient.FirstPeer.Send(dataWriter, deliveryMethod);

            packet.Flush();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.CONNECTED_TO_SERVER.Localized(), peer.Port),
                ENotificationDurationType.Default, ENotificationIconType.Friend);

            Profile ownProfile = FikaGlobals.GetProfile(FikaBackendUtils.IsScav);
            if (ownProfile == null)
            {
                logger.LogError("OnPeerConnected: Own profile was null!");
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
            logger.LogInfo("[CLIENT] We received error " + socketErrorCode);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            if (channelNumber == 1)
            {
                VOIPClient.NetworkReceivedPacket(new(reader.GetBytesWithLength()));
            }
            else
            {
                packetProcessor.ReadAllPackets(reader, peer);
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.BasicMessage && netClient.ConnectedPeersCount == 0 && reader.GetInt() == 1)
            {
                logger.LogInfo("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
                netClient.Connect(remoteEndPoint, "fika.core");
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
            logger.LogInfo("[CLIENT] We disconnected because " + disconnectInfo.Reason);
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

                logger.LogError("OnPeerDisconnected: Rejected connection but no reason");
            }
        }

        public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new()
        {
            packetProcessor.SubscribeNetSerializable(handle);
        }

        public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new()
        {
            packetProcessor.SubscribeNetSerializable(handle);
        }

        public void RegisterReusable<T>(Action<T> handle) where T : class, IReusable, new()
        {
            packetProcessor.SubscribeReusable(handle);
        }

        public void RegisterReusable<T, TUserData>(Action<T, TUserData> handle) where T : class, IReusable, new()
        {
            packetProcessor.SubscribeReusable(handle);
        }

        public void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate)
        {
            packetProcessor.RegisterNestedType(writeDelegate, readDelegate);
        }

        public void PrintStatistics()
        {
            logger.LogInfo("..:: Fika Client Session Statistics ::..");
            logger.LogInfo($"Sent packets: {netClient.Statistics.PacketsSent}");
            logger.LogInfo($"Sent data: {FikaGlobals.FormatFileSize(netClient.Statistics.BytesSent)}");
            logger.LogInfo($"Received packets: {netClient.Statistics.PacketsReceived}");
            logger.LogInfo($"Received data: {FikaGlobals.FormatFileSize(netClient.Statistics.BytesReceived)}");
            logger.LogInfo($"Packet loss: {netClient.Statistics.PacketLossPercent}%");
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
                        using GClass1212 eftReader = GClass1215.Get(packet.OperationBytes);
                        BaseDescriptorClass descriptor = eftReader.ReadPolymorph<BaseDescriptorClass>();
                        GStruct452 result = networkController.CreateOperationFromDescriptor(descriptor);
                        if (!result.Succeeded)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"ConvertInventoryPacket::Unable to process descriptor from netId {packet.NetId}, error: {result.Error}");
                            return;
                        }

                        inventoryOperations.Enqueue(result.Value);
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
