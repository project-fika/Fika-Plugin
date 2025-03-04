﻿// © 2025 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using Dissonance;
using Dissonance.Integrations.MirrorIgnorance;
using Diz.Utils;
using EFT;
using EFT.Airdrop;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using EFT.UI;
using EFT.Vehicle;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.HostClasses;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Patches.VOIP;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.VOIP;
using Fika.Core.Utils;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using Open.Nat;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Networking.GenericSubPackets;
using static Fika.Core.Networking.ReconnectPacket;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    /// <summary>
    /// Server used to synchronize all <see cref="FikaClient"/>s
    /// </summary>
    public class FikaServer : MonoBehaviour, INetEventListener, INatPunchListener, GInterface253, IFikaNetworkManager
    {
        public int ReadyClients;
        public DateTime TimeSinceLastPeerDisconnected;
        public bool HasHadPeer;
        public bool RaidInitialized;
        public bool HostReady;
        public FikaHostWorld FikaHostWorld { get; set; }
        public bool Started
        {
            get
            {
                return netServer != null && netServer.IsRunning;
            }
        }
        public DateTime? GameStartTime
        {
            get
            {
                if (gameStartTime == null)
                {
                    gameStartTime = EFTDateTimeClass.UtcNow;
                }
                return gameStartTime;
            }
            set
            {
                gameStartTime = value;
            }
        }
        public NetManager NetServer
        {
            get
            {
                return netServer;
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

        public bool MultiThreaded
        {
            get
            {
                return netServer != null && netServer.UnsyncedEvents;
            }
        }

        public int NetId { get; set; }
        public EPlayerSide RaidSide { get; set; }
        public bool AllowVOIP { get; set; }

        private int sendRate;
        private NetPacketProcessor packetProcessor;
        private CoopPlayer hostPlayer;
        private List<string> playersMissing;
        private string externalIp;
        private NetManager netServer;
        private DateTime? gameStartTime;
        private NetDataWriter dataWriter;
        private int port;
        private CoopHandler coopHandler;
        private ManualLogSource logger;
        private int currentNetId;
        private FikaChat fikaChat;
        private CancellationTokenSource natIntroduceRoutineCts;
        private int statisticsCounter;
        private Dictionary<Profile, bool> visualProfiles;        

        internal FikaVOIPServer VOIPServer { get; set; }
        internal FikaVOIPClient VOIPClient { get; set; }

        public async Task Init()
        {
            netServer = new(this)
            {
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true,
                UpdateTime = 50,
                AutoRecycle = true,
                IPv6Enabled = false,
                DisconnectTimeout = FikaPlugin.ConnectionTimeout.Value * 1000,
                UseNativeSockets = FikaPlugin.NativeSockets.Value,
                EnableStatistics = true,
                NatPunchEnabled = true,
                UnsyncedEvents = FikaPlugin.NetMultiThreaded.Value,
                ChannelsCount = 2
            };

            AllowVOIP = FikaPlugin.AllowVOIP.Value;

            packetProcessor = new(FikaPlugin.NetMultiThreaded.Value);
            dataWriter = new();
            playersMissing = [];
            externalIp = NetUtils.GetLocalIp(LocalAddrType.IPv4);
            statisticsCounter = 0;
            logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Server");

            ReadyClients = 0;

            TimeSinceLastPeerDisconnected = DateTime.Now.AddDays(1);

            visualProfiles = [];
            if (!FikaBackendUtils.IsHeadless)
            {
                Profile ownProfile = FikaGlobals.GetProfile(FikaBackendUtils.IsScav);
                if (ownProfile != null)
                {
                    visualProfiles.Add(ownProfile, true);
                }
                else
                {
                    logger.LogError("Init: Own profile was null!");
                }
            }

            sendRate = FikaPlugin.SendRate.Value switch
            {
                FikaPlugin.ESendRate.VeryLow => 10,
                FikaPlugin.ESendRate.Low => 15,
                FikaPlugin.ESendRate.Medium => 20,
                FikaPlugin.ESendRate.High => 30,
                _ => 20,
            };
            logger.LogInfo($"Starting server with SendRate: {sendRate}");
            port = FikaPlugin.UDPPort.Value;

            NetworkGameSession.Rtt = 0;
            NetworkGameSession.LossPercent = 0;

            currentNetId = 2;
            NetId = 1;

            packetProcessor.RegisterNestedType(FikaSerializationExtensions.PutRagdollStruct, FikaSerializationExtensions.GetRagdollStruct);
            packetProcessor.RegisterNestedType(FikaSerializationExtensions.PutArtilleryStruct, FikaSerializationExtensions.GetArtilleryStruct);
            packetProcessor.RegisterNestedType(FikaSerializationExtensions.PutGrenadeStruct, FikaSerializationExtensions.GetGrenadeStruct);
            packetProcessor.RegisterNestedType(FikaSerializationExtensions.PutAirplaneDataPacketStruct, FikaSerializationExtensions.GetAirplaneDataPacketStruct);

            RegisterMultiThreadedPackets();

            if (netServer.UnsyncedEvents)
            {
                RegisterMultiThreadedPackets();
            }
            else
            {
                RegisterPackets();
            }

#if DEBUG
            AddDebugPackets();
#endif            
            await NetManagerUtils.CreateCoopHandler();

            if (FikaPlugin.UseUPnP.Value && !FikaPlugin.UseNatPunching.Value)
            {
                bool upnpFailed = false;

                try
                {
                    NatDiscoverer discoverer = new();
                    CancellationTokenSource cts = new(10000);
                    NatDevice device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                    IPAddress extIp = await device.GetExternalIPAsync();
                    externalIp = extIp.MapToIPv4().ToString();

                    await device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, 300, "Fika UDP"));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error when attempting to map UPnP. Make sure the selected port is not already open! Exception: {ex.Message}");
                    upnpFailed = true;
                }

                if (upnpFailed)
                {
                    Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", LocaleUtils.UI_UPNP_FAILED.Localized());
                    throw new MappingException("Error during mapping. Check log file for more information.");
                }
            }
            else if (FikaPlugin.ForceIP.Value != "")
            {
                externalIp = FikaPlugin.ForceIP.Value;
            }
            else
            {
                if (FikaPlugin.Instance.WanIP == null)
                {
                    throw new NullReferenceException("Failed to start Fika Server because WAN IP was null!");
                }

                externalIp = FikaPlugin.Instance.WanIP.ToString();
            }

            if (FikaPlugin.UseNatPunching.Value)
            {
                netServer.NatPunchModule.UnsyncedEvents = true;
                netServer.NatPunchModule.Init(this);
                netServer.Start();

                natIntroduceRoutineCts = new CancellationTokenSource();

                string natPunchServerIP = FikaPlugin.Instance.NatPunchServerIP;
                int natPunchServerPort = FikaPlugin.Instance.NatPunchServerPort;
                string token = $"server:{RequestHandler.SessionId}";

                Task natIntroduceTask = Task.Run(() =>
                {
                    NatIntroduceRoutine(natPunchServerIP, natPunchServerPort, token, natIntroduceRoutineCts.Token);
                });
            }
            else
            {
                if (FikaPlugin.ForceBindIP.Value != "Disabled")
                {
                    netServer.Start(FikaPlugin.ForceBindIP.Value, "", port);
                }
                else
                {
                    netServer.Start(port);
                }
            }

            logger.LogInfo("Started Fika Server");

            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.SERVER_STARTED.Localized(), port),
                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);

            string[] Ips = [];
            foreach (string ip in FikaPlugin.Instance.LocalIPs)
            {
                if (ValidateLocalIP(ip))
                {
                    Ips = [externalIp, ip];
                }
            }

            if (Ips.Length < 1)
            {
                Ips = [externalIp, ""];
                NotificationManagerClass.DisplayMessageNotification(LocaleUtils.NO_VALID_IP.Localized(),
                    iconType: EFT.Communications.ENotificationIconType.Alert);
            }

            SetHostRequest body = new(Ips, port, FikaPlugin.UseNatPunching.Value, FikaBackendUtils.IsHeadlessGame);
            FikaRequestHandler.UpdateSetHost(body);
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
                gameObj.AddComponent<FikaCommsNetwork>();
                Destroy(mirrorCommsNetwork);

                DissonanceComms_Start_Patch.IsReady = true;
                DissonanceComms dissonance = gameObj.GetComponent<DissonanceComms>();
                dissonance.Invoke("Start", 0);
            }
            else
            {
                controller.VoipDisabledByInitializationFail();
            }

            do
            {
                await Task.Yield();
            } while (VOIPServer == null && VOIPClient == null);

            return;
        }

        private void RegisterMultiThreadedPackets()
        {
            packetProcessor.SubscribeNetSerializableMT<PlayerStatePacket, NetPeer>(OnPlayerStatePacketReceived);
            packetProcessor.SubscribeNetSerializableMT<WeaponPacket, NetPeer>(OnWeaponPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<DamagePacket, NetPeer>(OnDamagePacketReceived);
            packetProcessor.SubscribeNetSerializableMT<ArmorDamagePacket, NetPeer>(OnArmorDamagePacketReceived);
            packetProcessor.SubscribeNetSerializableMT<InventoryPacket, NetPeer>(OnInventoryPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<CommonPlayerPacket, NetPeer>(OnCommonPlayerPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<AllCharacterRequestPacket, NetPeer>(OnAllCharacterRequestPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<InformationPacket, NetPeer>(OnInformationPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<HealthSyncPacket, NetPeer>(OnHealthSyncPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<GenericPacket, NetPeer>(OnGenericPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<SendCharacterPacket, NetPeer>(OnSendCharacterPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<TextMessagePacket, NetPeer>(OnTextMessagePacketReceived);
            packetProcessor.SubscribeNetSerializableMT<QuestConditionPacket, NetPeer>(OnQuestConditionPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<QuestItemPacket, NetPeer>(OnQuestItemPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<QuestDropItemPacket, NetPeer>(OnQuestDropItemPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<InteractableInitPacket, NetPeer>(OnInteractableInitPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<WorldLootPacket, NetPeer>(OnWorldLootPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<ReconnectPacket, NetPeer>(OnReconnectPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<BTRInteractionPacket, NetPeer>(OnBTRInteractionPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<ResyncInventoryIdPacket, NetPeer>(OnResyncInventoryIdPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<UsableItemPacket, NetPeer>(OnUsableItemPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<SyncTransitControllersPacket, NetPeer>(OnSyncTransitControllersPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<TransitInteractPacket, NetPeer>(OnTransitInteractPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<BotStatePacket, NetPeer>(OnBotStatePacketReceived);
            packetProcessor.SubscribeNetSerializableMT<PingPacket, NetPeer>(OnPingPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<LootSyncPacket, NetPeer>(OnLootSyncPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<LoadingProfilePacket, NetPeer>(OnLoadingProfilePacketReceived);
            packetProcessor.SubscribeNetSerializableMT<SideEffectPacket, NetPeer>(OnSideEffectPacketReceived);
            packetProcessor.SubscribeNetSerializableMT<RequestPacket, NetPeer>(OnRequestPacketReceived);
        }

        private void RegisterPackets()
        {
            packetProcessor.SubscribeNetSerializable<PlayerStatePacket, NetPeer>(OnPlayerStatePacketReceived);
            packetProcessor.SubscribeNetSerializable<WeaponPacket, NetPeer>(OnWeaponPacketReceived);
            packetProcessor.SubscribeNetSerializable<DamagePacket, NetPeer>(OnDamagePacketReceived);
            packetProcessor.SubscribeNetSerializable<ArmorDamagePacket, NetPeer>(OnArmorDamagePacketReceived);
            packetProcessor.SubscribeNetSerializable<InventoryPacket, NetPeer>(OnInventoryPacketReceived);
            packetProcessor.SubscribeNetSerializable<CommonPlayerPacket, NetPeer>(OnCommonPlayerPacketReceived);
            packetProcessor.SubscribeNetSerializable<AllCharacterRequestPacket, NetPeer>(OnAllCharacterRequestPacketReceived);
            packetProcessor.SubscribeNetSerializable<InformationPacket, NetPeer>(OnInformationPacketReceived);
            packetProcessor.SubscribeNetSerializable<HealthSyncPacket, NetPeer>(OnHealthSyncPacketReceived);
            packetProcessor.SubscribeNetSerializable<GenericPacket, NetPeer>(OnGenericPacketReceived);
            packetProcessor.SubscribeNetSerializable<SendCharacterPacket, NetPeer>(OnSendCharacterPacketReceived);
            packetProcessor.SubscribeNetSerializable<TextMessagePacket, NetPeer>(OnTextMessagePacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestConditionPacket, NetPeer>(OnQuestConditionPacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestItemPacket, NetPeer>(OnQuestItemPacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestDropItemPacket, NetPeer>(OnQuestDropItemPacketReceived);
            packetProcessor.SubscribeNetSerializable<InteractableInitPacket, NetPeer>(OnInteractableInitPacketReceived);
            packetProcessor.SubscribeNetSerializable<WorldLootPacket, NetPeer>(OnWorldLootPacketReceived);
            packetProcessor.SubscribeNetSerializable<ReconnectPacket, NetPeer>(OnReconnectPacketReceived);
            packetProcessor.SubscribeNetSerializable<BTRInteractionPacket, NetPeer>(OnBTRInteractionPacketReceived);
            packetProcessor.SubscribeNetSerializable<ResyncInventoryIdPacket, NetPeer>(OnResyncInventoryIdPacketReceived);
            packetProcessor.SubscribeNetSerializable<UsableItemPacket, NetPeer>(OnUsableItemPacketReceived);
            packetProcessor.SubscribeNetSerializable<SyncTransitControllersPacket, NetPeer>(OnSyncTransitControllersPacketReceived);
            packetProcessor.SubscribeNetSerializable<TransitInteractPacket, NetPeer>(OnTransitInteractPacketReceived);
            packetProcessor.SubscribeNetSerializable<BotStatePacket, NetPeer>(OnBotStatePacketReceived);
            packetProcessor.SubscribeNetSerializable<PingPacket, NetPeer>(OnPingPacketReceived);
            packetProcessor.SubscribeNetSerializable<LootSyncPacket, NetPeer>(OnLootSyncPacketReceived);
            packetProcessor.SubscribeNetSerializable<LoadingProfilePacket, NetPeer>(OnLoadingProfilePacketReceived);
            packetProcessor.SubscribeNetSerializable<SideEffectPacket, NetPeer>(OnSideEffectPacketReceived);
            packetProcessor.SubscribeNetSerializable<RequestPacket, NetPeer>(OnRequestPacketReceived);
        }

        private void OnRequestPacketReceived(RequestPacket packet, NetPeer peer)
        {
            if (packet.RequestSubPacket == null)
            {
                logger.LogError("OnRequestPacketReceived: RequestSubPacket was null!");
                return;
            }

            packet.RequestSubPacket.HandleRequest(peer, this);
        }

        private void OnSideEffectPacketReceived(SideEffectPacket packet, NetPeer peer)
        {
#if DEBUG
            logger.LogWarning("OnSideEffectPacketReceived: Received");
#endif
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

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
#if DEBUG
                logger.LogInfo("Setting value to: " + packet.Value + ", original: " + sideEffectComponent.Value);
#endif
                sideEffectComponent.Value = packet.Value;
                item.RaiseRefreshEvent(false, false);
                return;
            }
            logger.LogError("OnSideEffectPacketReceived: SideEffectComponent was not found!");
        }

        private void OnLoadingProfilePacketReceived(LoadingProfilePacket packet, NetPeer peer)
        {
            if (packet.Profiles == null)
            {
                logger.LogError("OnLoadingProfilePacketReceived: Profiles was null!");
                return;
            }

            KeyValuePair<Profile, bool> kvp = packet.Profiles.First();
            if (!visualProfiles.Any(x => x.Key.ProfileId == kvp.Key.ProfileId))
            {
                visualProfiles.Add(kvp.Key, visualProfiles.Count == 0 || kvp.Value);
            }
            FikaBackendUtils.AddPartyMembers(visualProfiles);
            packet.Profiles = visualProfiles;
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            GenericPacket notifPacket = new()
            {
                NetId = 1,
                Type = EGenericSubPacketType.ClientConnected,
                SubPacket = new ClientConnected(kvp.Key.Nickname)
            };
            if (!FikaBackendUtils.IsHeadless)
            {
                notifPacket.SubPacket.Execute();
            }
            SendDataToAll(ref notifPacket, DeliveryMethod.ReliableOrdered, peer);

            peer.Tag = kvp.Key.Nickname;
        }

        private void OnLootSyncPacketReceived(LootSyncPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, packet.Data.Done ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable, peer);

            if (FikaHostWorld != null)
            {
                FikaHostWorld.LootSyncPackets.Add(packet.Data);
            }
        }

        private void OnPingPacketReceived(PingPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (FikaPlugin.UsePingSystem.Value)
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
                        CoopGame coopGame = coopHandler.LocalGameInstance;
                        if (coopGame != null)
                        {
                            coopGame.IncreaseLoadedPlayers(packet.NetId);
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
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

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
            if (MultiThreaded)
            {
                packetProcessor.SubscribeNetSerializableMT<SpawnItemPacket, NetPeer>(OnSpawnItemPacketReceived);
            }
            else
            {
                packetProcessor.SubscribeNetSerializable<SpawnItemPacket, NetPeer>(OnSpawnItemPacketReceived);
            }
        }
#endif

        private void OnSpawnItemPacketReceived(SpawnItemPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                FikaGlobals.SpawnItemInWorld(packet.Item, playerToApply);
            }
        }
        private void OnUsableItemPacketReceived(UsableItemPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleUsableItemPacket(packet);
            }
        }

        public void SendAirdropContainerData(EAirdropType containerType, Item item, int ObjectId)
        {
            logger.LogInfo($"Sending airdrop details, type: {containerType}, id: {ObjectId}");
            int netId = 0;
            SynchronizableObject containerObject = null;
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            IEnumerable<SynchronizableObject> syncObjects = gameWorld.SynchronizableObjectLogicProcessor.GetSynchronizableObjects();
            foreach (SynchronizableObject syncObject in syncObjects)
            {
                if (syncObject.ObjectId == ObjectId)
                {
                    LootableContainer container = syncObject.GetComponentInChildren<LootableContainer>().gameObject.GetComponentInChildren<LootableContainer>();
                    if (container != null)
                    {
                        netId = container.NetId;
                        containerObject = syncObject;
                        gameWorld.RegisterWorldInteractionObject(container);
                        break;
                    }
                }
            }

            if (netId == 0)
            {
                logger.LogError("SendAirdropContainerData: Could not find NetId!");
            }

            SpawnSyncObjectPacket packet = new()
            {
                ObjectType = SynchronizableObjectType.AirDrop,
                SubPacket = new SpawnSyncObjectSubPackets.SpawnAirdrop()
                {
                    ObjectId = ObjectId,
                    IsStatic = false,
                    Position = containerObject != null ? containerObject.transform.position : Vector3.zero,
                    Rotation = containerObject != null ? containerObject.transform.rotation : Quaternion.identity,
                    AirdropType = containerType,
                    AirdropItem = item,
                    ContainerId = item.Id,
                    NetId = netId
                }
            };

            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        public void SendFlareSuccessEvent(string profileId, bool canSendAirdrop)
        {
            FlareSuccessPacket packet = new(profileId, canSendAirdrop);
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet, NetPeer peer)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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

                    SendDataToAll(ref response, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void OnResyncInventoryIdPacketReceived(ResyncInventoryIdPacket packet, NetPeer peer)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply is ObservedCoopPlayer observedPlayer)
                {
                    SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
                    if (observedPlayer.InventoryController is ObservedInventoryController observedController)
                    {
                        observedController.SetNewID(packet.MongoId.Value);
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
                    foreach (CoopPlayer player in coopHandler.HumanPlayers)
                    {
                        if (player.ProfileId == packet.ProfileId && player is ObservedCoopPlayer observedCoopPlayer)
                        {
                            ReconnectPacket ownCharacterPacket = new()
                            {
                                Type = EReconnectDataType.OwnCharacter,
                                Profile = observedCoopPlayer.Profile,
                                ProfileHealthClass = observedCoopPlayer.NetworkHealthController.Store(),
                                PlayerPosition = observedCoopPlayer.Position,
                                TimeOffset = NetworkTimeSync.Time
                            };

                            SendDataToPeer(peer, ref ownCharacterPacket, DeliveryMethod.ReliableOrdered);

                            observedCoopPlayer.HealthBar.ClearEffects();
                            GenericPacket clearEffectsPacket = new()
                            {
                                NetId = observedCoopPlayer.NetId,
                                Type = EGenericSubPacketType.ClearEffects
                            };

                            SendDataToAll(ref clearEffectsPacket, DeliveryMethod.ReliableOrdered, peer);
                        }
                    }

                    return;
                }

                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                Traverse worldTraverse = Traverse.Create(gameWorld.World_0);

                GClass797<int, Throwable>.GStruct46 grenades = gameWorld.Grenades.GetValuesEnumerator();
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

                    SendDataToPeer(peer, ref throwablePacket, DeliveryMethod.ReliableOrdered);
                }

                List<WorldInteractiveObject.WorldInteractiveDataPacketStruct> interactivesData = [];
                WorldInteractiveObject[] worldInteractiveObjects = worldTraverse.Field<WorldInteractiveObject[]>("worldInteractiveObject_0").Value;
                foreach (WorldInteractiveObject interactiveObject in worldInteractiveObjects)
                {
                    if ((interactiveObject.DoorState != interactiveObject.InitialDoorState && interactiveObject.DoorState != EDoorState.Interacting)
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

                    SendDataToPeer(peer, ref interactivePacket, DeliveryMethod.ReliableOrdered);
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

                    SendDataToPeer(peer, ref lampPacket, DeliveryMethod.ReliableOrdered);
                }

                GClass797<int, WindowBreaker>.GStruct46 windows = gameWorld.Windows.GetValuesEnumerator();
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

                    SendDataToPeer(peer, ref windowPacket, DeliveryMethod.ReliableOrdered);
                }

                foreach (CoopPlayer player in coopHandler.Players.Values)
                {
                    SendCharacterPacket characterPacket = new(new()
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
                    else if (player is ObservedCoopPlayer observedPlayer)
                    {
                        characterPacket.PlayerInfoPacket.HealthByteArray = observedPlayer.NetworkHealthController.Store().SerializeHealthInfo();
                    }

                    if (player.HandsController != null)
                    {
                        characterPacket.PlayerInfoPacket.ControllerType = HandsControllerToEnumClass.FromController(player.HandsController);
                        characterPacket.PlayerInfoPacket.ItemId = player.HandsController.Item.Id;
                        characterPacket.PlayerInfoPacket.IsStationary = player.MovementContext.IsStationaryWeaponInHands;
                    }

                    SendDataToPeer(peer, ref characterPacket, DeliveryMethod.ReliableOrdered);
                }

                foreach (CoopPlayer player in coopHandler.HumanPlayers)
                {
                    if (player.ProfileId == packet.ProfileId)
                    {
                        AssignNetIdPacket assignPacket = new()
                        {
                            NetId = player.NetId
                        };

                        SendDataToPeer(peer, ref assignPacket, DeliveryMethod.ReliableOrdered);
                    }
                }

                ReconnectPacket finishPacket = new()
                {
                    Type = EReconnectDataType.Finished
                };

                SendDataToPeer(peer, ref finishPacket, DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnWorldLootPacketReceived(WorldLootPacket packet, NetPeer peer)
        {
            CoopGame coopGame = CoopGame.Instance;
            if (coopGame != null)
            {
                WorldLootPacket response = new()
                {
                    Data = coopGame.GetHostLootItems()
                };

                SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
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

                        SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        private void OnQuestDropItemPacketReceived(QuestDropItemPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (hostPlayer.HealthController.IsAlive)
            {
                if (hostPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestDropItemPacket(ref packet);
                }
            }
        }

        private bool ValidateLocalIP(string LocalIP)
        {
            try
            {
                if (LocalIP.StartsWith("192.168") || LocalIP.StartsWith("10"))
                {
                    return true;
                }

                //Check for RFC1918's 20 bit block.
                int[] ip = Array.ConvertAll(LocalIP.Split('.'), int.Parse);

                if (ip[0] == 172 && (ip[1] >= 16 && ip[1] <= 31))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error parsing {LocalIP}, exception: {ex}");
                return false;
            }
        }

        private async void NatIntroduceRoutine(string natPunchServerIP, int natPunchServerPort, string token, CancellationToken ct)
        {
            logger.LogInfo("NatIntroduceRoutine started.");

            while (!ct.IsCancellationRequested)
            {
                netServer.NatPunchModule.SendNatIntroduceRequest(natPunchServerIP, natPunchServerPort, token);

                logger.LogInfo($"SendNatIntroduceRequest: {natPunchServerIP}:{natPunchServerPort}");

                await Task.Delay(TimeSpan.FromSeconds(15));
            }

            logger.LogInfo("NatIntroduceRoutine ended.");
        }

        private void OnQuestItemPacketReceived(QuestItemPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (hostPlayer.HealthController.IsAlive)
            {
                if (hostPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestItemPacket(ref packet);
                }
            }
        }

        private void OnQuestConditionPacketReceived(QuestConditionPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (hostPlayer.HealthController.IsAlive)
            {
                if (hostPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestPacket(ref packet);
                }
            }
        }

        private void OnTextMessagePacketReceived(TextMessagePacket packet, NetPeer peer)
        {
            logger.LogInfo($"Received message from: {packet.Nickname}, Message: {packet.Message}");

            if (fikaChat != null)
            {
                fikaChat.ReceiveMessage(packet.Nickname, packet.Message);
            }

            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        public int PopNetId()
        {
            int netId = currentNetId;
            currentNetId++;

            return netId;
        }

        public void SetupGameVariables(CoopPlayer coopPlayer)
        {
            hostPlayer = coopPlayer;
            if (FikaPlugin.EnableChat.Value)
            {
                fikaChat = gameObject.AddComponent<FikaChat>();
            }
        }

        private void OnSendCharacterPacketReceived(SendCharacterPacket packet, NetPeer peer)
        {
            if (coopHandler == null)
            {
                return;
            }

            if (packet.PlayerInfoPacket.Profile.ProfileId != hostPlayer.ProfileId)
            {
                coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
                    packet.PlayerInfoPacket.ControllerId.Value, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
                    packet.PlayerInfoPacket.ControllerType, packet.PlayerInfoPacket.ItemId);
            }

            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnGenericPacketReceived(GenericPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            packet.SubPacket.Execute();
        }

        private void OnHealthSyncPacketReceived(HealthSyncPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
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

        private void OnInformationPacketReceived(InformationPacket packet, NetPeer peer)
        {
            ReadyClients += packet.ReadyPlayers;

            bool gameExists = coopHandler != null && coopHandler.LocalGameInstance != null;

            InformationPacket respondPackage = new()
            {
                RaidStarted = gameExists && coopHandler.LocalGameInstance.RaidStarted,
                ReadyPlayers = ReadyClients,
                HostReady = HostReady,
                HostLoaded = RaidInitialized,
                AmountOfPeers = netServer.ConnectedPeersCount + 1
            };

            if (gameExists && packet.RequestStart)
            {
                coopHandler.LocalGameInstance.RaidStarted = true;
            }

            if (gameExists && HostReady)
            {
                respondPackage.GameTime = gameStartTime.Value;
                GameTimerClass gameTimer = coopHandler.LocalGameInstance.GameTimer;
                respondPackage.SessionTime = gameTimer.SessionTime.Value;
            }

            SendDataToAll(ref respondPackage, DeliveryMethod.ReliableOrdered);
        }

        private void OnAllCharacterRequestPacketReceived(AllCharacterRequestPacket packet, NetPeer peer)
        {
            if (coopHandler == null)
            {
                return;
            }

            if (packet.IsRequest)
            {
                foreach (KeyValuePair<int, CoopPlayer> pair in coopHandler.Players)
                {
                    if (pair.Value.ProfileId == packet.ProfileId)
                    {
                        continue;
                    }

                    if (packet.Characters.Contains(pair.Key))
                    {
                        continue;
                    }

                    AllCharacterRequestPacket requestPacket = new(pair.Value.ProfileId)
                    {
                        IsRequest = false,
                        PlayerInfoPacket = new()
                        {
                            Profile = pair.Value.Profile,
                            ControllerId = pair.Value.InventoryController.CurrentId,
                            FirstOperationId = pair.Value.InventoryController.NextOperationId
                        },
                        IsAlive = pair.Value.HealthController.IsAlive,
                        IsAI = pair.Value.IsAI,
                        Position = pair.Value.Transform.position,
                        NetId = pair.Value.NetId
                    };

                    if (pair.Value.ActiveHealthController != null)
                    {
                        requestPacket.PlayerInfoPacket.HealthByteArray = pair.Value.ActiveHealthController.SerializeState();
                    }

                    if (pair.Value.HandsController != null)
                    {
                        requestPacket.PlayerInfoPacket.ControllerType = HandsControllerToEnumClass.FromController(pair.Value.HandsController);
                        requestPacket.PlayerInfoPacket.ItemId = pair.Value.HandsController.Item.Id;
                        requestPacket.PlayerInfoPacket.IsStationary = pair.Value.MovementContext.IsStationaryWeaponInHands;
                    }

                    SendDataToPeer(peer, ref requestPacket, DeliveryMethod.ReliableOrdered);
                }
            }

            if (!coopHandler.Players.ContainsKey(packet.NetId) && !playersMissing.Contains(packet.ProfileId) && !coopHandler.ExtractedPlayers.Contains(packet.NetId))
            {
                playersMissing.Add(packet.ProfileId);
                logger.LogInfo($"Requesting missing player from server.");
                AllCharacterRequestPacket requestPacket = new(hostPlayer.ProfileId);
                SendDataToPeer(peer, ref requestPacket, DeliveryMethod.ReliableOrdered);
            }

            if (!packet.IsRequest && playersMissing.Contains(packet.ProfileId))
            {
                logger.LogInfo($"Received CharacterRequest from client: ProfileID: {packet.PlayerInfoPacket.Profile.ProfileId}, Nickname: {packet.PlayerInfoPacket.Profile.Nickname}");
                if (packet.ProfileId != hostPlayer.ProfileId)
                {
                    coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
                        packet.PlayerInfoPacket.ControllerId.Value, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
                        packet.PlayerInfoPacket.ControllerType, packet.PlayerInfoPacket.ItemId);
                    playersMissing.Remove(packet.ProfileId);
                }
            }
        }

        private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
            }
        }

        private void OnInventoryPacketReceived(InventoryPacket packet, NetPeer peer)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                FikaReader eftReader = EFTSerializationManager.GetReader(packet.OperationBytes);
                try
                {
                    OperationCallbackPacket operationCallbackPacket;
                    if (playerToApply.InventoryController is Interface16 inventoryController)
                    {
                        BaseDescriptorClass descriptor = eftReader.ReadPolymorph<BaseDescriptorClass>();
                        GStruct452 result = inventoryController.CreateOperationFromDescriptor(descriptor);
#if DEBUG
                        ConsoleScreen.Log($"Received InvOperation: {result.Value.GetType().Name}, Id: {result.Value.Id}");
#endif

                        if (result.Failed)
                        {
                            logger.LogError($"ItemControllerExecutePacket::Operation conversion failed: {result.Error}");
                            OperationCallbackPacket callbackPacket = new(playerToApply.NetId, packet.CallbackId, EOperationStatus.Failed)
                            {
                                Error = result.Error.ToString()
                            };
                            SendDataToPeer(peer, ref callbackPacket, DeliveryMethod.ReliableOrdered);

                            ResyncInventoryIdPacket resyncPacket = new(playerToApply.NetId);
                            SendDataToPeer(peer, ref resyncPacket, DeliveryMethod.ReliableOrdered);
                            return;
                        }

                        InventoryOperationHandler handler = new(result, packet.CallbackId, packet.NetId, peer, this);
                        operationCallbackPacket = new(playerToApply.NetId, packet.CallbackId, EOperationStatus.Started);
                        SendDataToPeer(peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

                        SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
                        handler.OperationResult.Value.method_1(new Callback(handler.HandleResult));
                    }
                    else
                    {
                        throw new NullReferenceException($"Inventory controller was not of type {nameof(Interface16)}!");
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError($"ItemControllerExecutePacket::Exception thrown: {exception}");
                    OperationCallbackPacket callbackPacket = new(playerToApply.NetId, packet.CallbackId, EOperationStatus.Failed)
                    {
                        Error = exception.Message
                    };
                    SendDataToPeer(peer, ref callbackPacket, DeliveryMethod.ReliableOrdered);

                    ResyncInventoryIdPacket resyncPacket = new(playerToApply.NetId);
                    SendDataToPeer(peer, ref resyncPacket, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void OnDamagePacketReceived(DamagePacket packet, NetPeer peer)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply.IsAI || playerToApply.IsYourPlayer)
                {
                    playerToApply.HandleDamagePacket(packet);
                    return;
                }

                SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            }
        }

        private void OnArmorDamagePacketReceived(ArmorDamagePacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleArmorDamagePacket(packet);
            }
        }

        private void OnWeaponPacketReceived(WeaponPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
            }
        }

        private void OnPlayerStatePacketReceived(PlayerStatePacket packet, NetPeer peer)
        {
            if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.Snapshotter.Insert(packet);
            }

            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        protected void Update()
        {
            if (netServer != null)
            {
                if (netServer.UnsyncedEvents)
                {
                    packetProcessor.RunActions();
                }
                else
                {
                    netServer?.PollEvents();
                }
            }

            statisticsCounter++;
            if (statisticsCounter > 600)
            {
                statisticsCounter = 0;
                SendStatisticsPacket();
            }
        }

        private void SendStatisticsPacket()
        {
            if (netServer == null)
            {
                return;
            }

            int fps = (int)(1f / Time.unscaledDeltaTime);
            StatisticsPacket packet = new(fps);

            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        protected void OnDestroy()
        {
            netServer?.Stop();

            if (fikaChat != null)
            {
                Destroy(fikaChat);
            }

            FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
        }

        public void SendDataToAll<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peerToExclude = null) where T : INetSerializable
        {
            dataWriter.Reset();

            if (peerToExclude != null)
            {
                if (NetServer.ConnectedPeersCount > 1)
                {
                    packetProcessor.WriteNetSerializable(dataWriter, ref packet);
                    netServer.SendToAll(dataWriter, deliveryMethod, peerToExclude);
                }
                return;
            }

            packetProcessor.WriteNetSerializable(dataWriter, ref packet);
            netServer.SendToAll(dataWriter, deliveryMethod);
        }

        public void SendReusableToAll<T>(T packet, DeliveryMethod deliveryMethod) where T : class, IReusable, new()
        {
            dataWriter.Reset();

            packetProcessor.Write(dataWriter, packet);
            netServer.SendToAll(dataWriter, deliveryMethod);

            packet.Flush();
        }

        public void SendDataToPeer<T>(NetPeer peer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            dataWriter.Reset();

            packetProcessor.WriteNetSerializable(dataWriter, ref packet);
            peer.Send(dataWriter, deliveryMethod);
        }

        public void SendVOIPPacket(ArraySegment<byte> data, bool reliable, NetPeer peer = null)
        {
            if (peer == null)
            {
                logger.LogError("SendVOIPPacket: peer was null!");
                return;
            }

            dataWriter.Reset();

            dataWriter.PutBytesWithLength(data.Array, data.Offset, (ushort)data.Count);
            //peer.Send(dataWriter, 1, reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);
            peer.Send(dataWriter, 1, DeliveryMethod.ReliableOrdered);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            AsyncWorker.RunInMainTread(() => NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_CONNECTED.Localized(), peer.Port),
                iconType: EFT.Communications.ENotificationIconType.Friend));
            logger.LogInfo($"Connection established with {peer.Address}:{peer.Port}, id: {peer.Id}.");

            HasHadPeer = true;

            NetworkSettingsPacket packet = new()
            {
                SendRate = sendRate,
                NetId = PopNetId(),
                AllowVOIP = AllowVOIP
            };
            SendDataToPeer(peer, ref packet, DeliveryMethod.ReliableOrdered);

            FikaEventDispatcher.DispatchEvent(new PeerConnectedEvent(peer, this));
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            logger.LogError("[SERVER] error " + socketErrorCode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            bool started = false;
            if (coopHandler != null && coopHandler.LocalGameInstance != null && coopHandler.LocalGameInstance.RaidStarted)
            {
                started = true;
            }

            if (messageType == UnconnectedMessageType.Broadcast)
            {
                logger.LogInfo("[SERVER] Received discovery request. Send discovery response");
                NetDataWriter resp = new();
                resp.Put(1);
                netServer.SendUnconnectedMessage(resp, remoteEndPoint);

                return;
            }

            if (reader.TryGetString(out string data))
            {
                NetDataWriter resp;

                switch (data)
                {
                    case "fika.hello":
                        resp = new();
                        resp.Put(started ? "fika.reject" : data);
                        netServer.SendUnconnectedMessage(resp, remoteEndPoint);
                        break;

                    case "fika.keepalive":
                        resp = new();
                        resp.Put(data);
                        netServer.SendUnconnectedMessage(resp, remoteEndPoint);

                        if (!natIntroduceRoutineCts.IsCancellationRequested)
                        {
                            natIntroduceRoutineCts.Cancel();
                        }
                        break;

                    case "fika.reconnect":
                        resp = new();
                        resp.Put("fika.hello");
                        netServer.SendUnconnectedMessage(resp, remoteEndPoint);
                        break;

                    default:
                        logger.LogError("PingingRequest: Data was not as expected");
                        break;
                }
            }
            else
            {
                logger.LogError("PingingRequest: Could not parse string");
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (coopHandler != null && coopHandler.LocalGameInstance != null && coopHandler.LocalGameInstance.RaidStarted)
            {
                if (request.Data.GetString() == "fika.reconnect")
                {
                    request.Accept();
                    return;
                }
                dataWriter.Reset();
                dataWriter.Put("Raid already started");
                request.Reject(dataWriter);

                return;
            }

            request.AcceptIfKey("fika.core");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger.LogInfo("Peer disconnected " + peer.Port + ", info: " + disconnectInfo.Reason);
            if (disconnectInfo.Reason != DisconnectReason.RemoteConnectionClose)
            {
                AsyncWorker.RunInMainTread(() => NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_DISCONNECTED.Localized(), [peer.Port, disconnectInfo.Reason]),
                        iconType: EFT.Communications.ENotificationIconType.Alert));
            }

            if (peer.Tag is string nickname)
            {
                GenericPacket packet = new()
                {
                    NetId = 1,
                    Type = EGenericSubPacketType.ClientDisconnected,
                    SubPacket = new ClientDisconnected(nickname)
                };

                packet.SubPacket.Execute();
                SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            }

            if (netServer.ConnectedPeersCount == 0)
            {
                TimeSinceLastPeerDisconnected = DateTime.Now;
            }

            if (FikaBackendUtils.IsHeadless)
            {
                if (netServer.ConnectedPeersCount == 0)
                {
                    AsyncWorker.RunInMainTread(DisconnectHeadless);
                }
            }
        }

        private void DisconnectHeadless()
        {
            foreach (Profile profile in Singleton<ClientApplication<ISession>>.Instance.Session.AllProfiles)
            {
                if (profile is null)
                {
                    continue;
                }

                if (profile.ProfileId == RequestHandler.SessionId)
                {
                    foreach (Profile.ProfileHealthClass.GClass1975 bodyPartHealth in profile.Health.BodyParts.Values)
                    {
                        bodyPartHealth.Effects.Clear();
                        bodyPartHealth.Health.Current = bodyPartHealth.Health.Maximum;
                    }
                }
            }

            // End the raid
            CoopGame coopGame = CoopGame.Instance;
            coopGame.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.ExitStatus, coopGame.ExitLocation, 0);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            if (channelNumber == 1)
            {
                VOIPServer.NetworkReceivedPacket(new(new RemotePeer(peer)), new(reader.GetBytesWithLength())); 
            }
            else
            {
                packetProcessor.ReadAllPackets(reader, peer);
            }
        }

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            // Do nothing
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            // Do nothing
        }

        public void OnNatIntroductionResponse(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            logger.LogInfo($"OnNatIntroductionResponse: {remoteEndPoint}");

            Task.Run(async () =>
            {
                NetDataWriter data = new();
                data.Put("fika.hello");

                for (int i = 0; i < 20; i++)
                {
                    netServer.SendUnconnectedMessage(data, localEndPoint);
                    netServer.SendUnconnectedMessage(data, remoteEndPoint);
                    await Task.Delay(250);
                }
            });
        }

        public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new()
        {
            if (MultiThreaded)
            {
                packetProcessor.SubscribeNetSerializableMT(handle);
            }
            else
            {
                packetProcessor.SubscribeNetSerializable(handle);
            }
        }

        public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new()
        {
            if (MultiThreaded)
            {
                packetProcessor.SubscribeNetSerializableMT(handle);
            }
            else
            {
                packetProcessor.SubscribeNetSerializable(handle);
            }
        }

        public void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate)
        {
            packetProcessor.RegisterNestedType(writeDelegate, readDelegate);
        }

        public void PrintStatistics()
        {
            logger.LogInfo("..:: Fika Server Session Statistics ::..");
            logger.LogInfo($"Sent packets: {netServer.Statistics.PacketsSent}");
            logger.LogInfo($"Sent data: {FikaGlobals.FormatFileSize(netServer.Statistics.BytesSent)}");
            logger.LogInfo($"Received packets: {netServer.Statistics.PacketsReceived}");
            logger.LogInfo($"Received data: {FikaGlobals.FormatFileSize(netServer.Statistics.BytesReceived)}");
            logger.LogInfo($"Packet loss: {netServer.Statistics.PacketLossPercent}%");
        }

        private class InventoryOperationHandler(GStruct452 operationResult, uint operationId, int netId, NetPeer peer, FikaServer server)
        {
            public GStruct452 OperationResult = operationResult;
            private readonly uint operationId = operationId;
            private readonly int netId = netId;
            private readonly NetPeer peer = peer;
            private readonly FikaServer server = server;

            internal void HandleResult(IResult result)
            {
                OperationCallbackPacket operationCallbackPacket;

                if (!result.Succeed)
                {
                    server.logger.LogError($"Error in operation: {result.Error ?? "An unknown error has occured"}");
                    operationCallbackPacket = new(netId, operationId, EOperationStatus.Failed, result.Error ?? "An unknown error has occured");
                    server.SendDataToPeer(peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

                    ResyncInventoryIdPacket resyncPacket = new(netId);
                    server.SendDataToPeer(peer, ref resyncPacket, DeliveryMethod.ReliableOrdered);

                    return;
                }

                operationCallbackPacket = new(netId, operationId, EOperationStatus.Succeeded);
                server.SendDataToPeer(peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}
