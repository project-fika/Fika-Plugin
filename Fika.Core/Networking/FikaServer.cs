// © 2025 Lacyway All Rights Reserved

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
#if DEBUG
using Fika.Core.Console;
#endif
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
using Fika.Core.Jobs;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Backend;
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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
#if DEBUG
using static Fika.Core.Networking.CommandPacket;
# endif
using static Fika.Core.Networking.GenericSubPackets;
using static Fika.Core.Networking.NetworkUtils;
using static Fika.Core.Networking.ReconnectPacket;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    /// <summary>
    /// Server used to synchronize all <see cref="FikaClient"/>s
    /// </summary>
    public class FikaServer : MonoBehaviour, INetEventListener, INatPunchListener, GInterface262, IFikaNetworkManager
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
                return _netServer != null && _netServer.IsRunning;
            }
        }
        public DateTime? GameStartTime
        {
            get
            {
                if (_gameStartTime == null)
                {
                    _gameStartTime = EFTDateTimeClass.UtcNow;
                }
                return _gameStartTime;
            }
            set
            {
                _gameStartTime = value;
            }
        }
        public NetManager NetServer
        {
            get
            {
                return _netServer;
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

        public int NetId { get; set; }
        public ESideType RaidSide { get; set; }
        public bool AllowVOIP { get; set; }
        public List<ObservedCoopPlayer> ObservedCoopPlayers { get; set; }

        private int _sendRate;
        private NetPacketProcessor _packetProcessor;
        private CoopPlayer _hostPlayer;
        private string _externalIp;
        private NetManager _netServer;
        private DateTime? _gameStartTime;
        private NetDataWriter _dataWriter;
        private int _port;
        private CoopHandler _coopHandler;
        private ManualLogSource _logger;
        private int _currentNetId;
        private FikaChat _fikaChat;
        private CancellationTokenSource _natIntroduceRoutineCts;
        private float _statisticsCounter;
        private float _sendThreshold;
        private Dictionary<Profile, bool> _visualProfiles;
        private Dictionary<string, int> _cachedConnections;
        private JobHandle _stateHandle;
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<PlayerStatePacket> _snapshots;
        private int _snapshotCount;

        internal FikaVOIPServer VOIPServer { get; set; }
        internal FikaVOIPClient VOIPClient { get; set; }


        public async void Init()
        {
            _netServer = new(this)
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
                ChannelsCount = 2
            };

            AllowVOIP = FikaPlugin.AllowVOIP.Value;

            _packetProcessor = new();
            _dataWriter = new();
            _externalIp = NetUtils.GetLocalIp(LocalAddrType.IPv4);
            _statisticsCounter = 0f;
            _sendThreshold = 2f;
            _cachedConnections = [];
            _logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Server");
            _snapshotCount = 0;
            _snapshots = _snapshots = new(512, Allocator.Persistent);
            ObservedCoopPlayers = [];

            ReadyClients = 0;

            TimeSinceLastPeerDisconnected = DateTime.Now.AddDays(1);

            _visualProfiles = [];
            if (!FikaBackendUtils.IsHeadless)
            {
                Profile ownProfile = FikaGlobals.GetLiteProfile(FikaBackendUtils.IsScav);
                if (ownProfile != null)
                {
                    _visualProfiles.Add(ownProfile, true);
                }
                else
                {
                    _logger.LogError("Init: Own profile was null!");
                }
            }

            _sendRate = FikaPlugin.SendRate.Value.ToNumber();
            _logger.LogInfo($"Starting server with SendRate: {_sendRate}");
            _port = FikaPlugin.UDPPort.Value;

            NetworkGameSession.Rtt = 0;
            NetworkGameSession.LossPercent = 0;

            _currentNetId = 2;
            NetId = 1;

            RegisterPacketsAndTypes();

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
                    _externalIp = extIp.MapToIPv4().ToString();

                    await device.CreatePortMapAsync(new Mapping(Protocol.Udp, _port, _port, 300, "Fika UDP"));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error when attempting to map UPnP. Make sure the selected port is not already open! Exception: {ex.Message}");
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
                _externalIp = FikaPlugin.ForceIP.Value;
            }
            else
            {
                if (FikaPlugin.Instance.WanIP == null)
                {
                    throw new NullReferenceException("Failed to start Fika Server because WAN IP was null!");
                }

                _externalIp = FikaPlugin.Instance.WanIP.ToString();
            }

            if (FikaPlugin.UseNatPunching.Value)
            {
                _netServer.NatPunchModule.UnsyncedEvents = true;
                _netServer.NatPunchModule.Init(this);
                _netServer.Start();

                _natIntroduceRoutineCts = new CancellationTokenSource();

                string natPunchServerIP = FikaPlugin.Instance.NatPunchServerIP;
                int natPunchServerPort = FikaPlugin.Instance.NatPunchServerPort;
                string token = $"server:{RequestHandler.SessionId}";

                Task natIntroduceTask = Task.Run(() =>
                {
                    NatIntroduceRoutine(natPunchServerIP, natPunchServerPort, token, _natIntroduceRoutineCts.Token);
                });
            }
            else
            {
                if (FikaPlugin.ForceBindIP.Value != "Disabled")
                {
                    _netServer.Start(FikaPlugin.ForceBindIP.Value, "", _port);
                }
                else
                {
                    _netServer.Start(_port);
                }
            }

            _logger.LogInfo("Started Fika Server");

            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.SERVER_STARTED.Localized(), _port),
                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);

            string[] Ips = [];
            foreach (string ip in FikaPlugin.Instance.LocalIPs)
            {
                if (ValidateLocalIP(ip))
                {
                    Ips = [_externalIp, ip];
                }
            }

            if (Ips.Length < 1)
            {
                Ips = [_externalIp, ""];
                NotificationManagerClass.DisplayMessageNotification(LocaleUtils.NO_VALID_IP.Localized(),
                    iconType: EFT.Communications.ENotificationIconType.Alert);
            }

            SetHostRequest body = new(Ips, _port, FikaPlugin.UseNatPunching.Value, FikaBackendUtils.IsHeadlessGame);
            FikaRequestHandler.UpdateSetHost(body);
        }

        async Task IFikaNetworkManager.InitializeVOIP()
        {
            VoipSettingsClass voipHandler = FikaGlobals.VOIPHandler;
            GClass1068 controller = Singleton<SharedGameSettingsClass>.Instance.Sound.Controller;
            if (voipHandler.MicrophoneChecked && !FikaBackendUtils.IsHeadless)
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
                gameObj.AddComponent<FikaCommsNetwork>();
                Destroy(mirrorCommsNetwork);

                DissonanceComms_Start_Patch.IsReady = true;
                DissonanceComms dissonance = gameObj.GetComponent<DissonanceComms>();
                dissonance.Invoke("Start", 0);
            }
            else if (FikaBackendUtils.IsHeadless)
            {
                await GClass1640.LoadScene(AssetsManagerSingletonClass.Manager,
                    SceneResourceKeyAbstractClass.DissonanceSetupScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);

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

            if (!FikaBackendUtils.IsHeadless)
            {
                do
                {
                    await Task.Yield();
                } while (VOIPServer == null && VOIPClient == null);
            }
            else
            {
                do
                {
                    await Task.Yield();
                } while (VOIPServer == null);
            }

            RegisterPacket<VOIPPacket, NetPeer>(OnVOIPPacketReceived);

            return;
        }

        private void RegisterPacketsAndTypes()
        {
            RegisterCustomType(FikaSerializationExtensions.PutRagdollStruct, FikaSerializationExtensions.GetRagdollStruct);
            RegisterCustomType(FikaSerializationExtensions.PutArtilleryStruct, FikaSerializationExtensions.GetArtilleryStruct);
            RegisterCustomType(FikaSerializationExtensions.PutGrenadeStruct, FikaSerializationExtensions.GetGrenadeStruct);
            RegisterCustomType(FikaSerializationExtensions.PutAirplaneDataPacketStruct, FikaSerializationExtensions.GetAirplaneDataPacketStruct);
            RegisterCustomType(FikaSerializationExtensions.PutLootSyncStruct, FikaSerializationExtensions.GetLootSyncStruct);

            RegisterPacket<PlayerStatePacket, NetPeer>(OnPlayerStatePacketReceived);
            RegisterPacket<WeaponPacket, NetPeer>(OnWeaponPacketReceived);
            RegisterPacket<DamagePacket, NetPeer>(OnDamagePacketReceived);
            RegisterPacket<ArmorDamagePacket, NetPeer>(OnArmorDamagePacketReceived);
            RegisterPacket<InventoryPacket, NetPeer>(OnInventoryPacketReceived);
            RegisterPacket<CommonPlayerPacket, NetPeer>(OnCommonPlayerPacketReceived);
            RegisterPacket<InformationPacket, NetPeer>(OnInformationPacketReceived);
            RegisterPacket<HealthSyncPacket, NetPeer>(OnHealthSyncPacketReceived);
            RegisterPacket<GenericPacket, NetPeer>(OnGenericPacketReceived);
            RegisterPacket<SendCharacterPacket, NetPeer>(OnSendCharacterPacketReceived);
            RegisterPacket<TextMessagePacket, NetPeer>(OnTextMessagePacketReceived);
            RegisterPacket<QuestConditionPacket, NetPeer>(OnQuestConditionPacketReceived);
            RegisterPacket<QuestItemPacket, NetPeer>(OnQuestItemPacketReceived);
            RegisterPacket<QuestDropItemPacket, NetPeer>(OnQuestDropItemPacketReceived);
            RegisterPacket<InteractableInitPacket, NetPeer>(OnInteractableInitPacketReceived);
            RegisterPacket<WorldLootPacket, NetPeer>(OnWorldLootPacketReceived);
            RegisterPacket<ReconnectPacket, NetPeer>(OnReconnectPacketReceived);
            RegisterPacket<BTRInteractionPacket, NetPeer>(OnBTRInteractionPacketReceived);
            RegisterPacket<ResyncInventoryIdPacket, NetPeer>(OnResyncInventoryIdPacketReceived);
            RegisterPacket<UsableItemPacket, NetPeer>(OnUsableItemPacketReceived);
            RegisterPacket<SyncTransitControllersPacket, NetPeer>(OnSyncTransitControllersPacketReceived);
            RegisterPacket<TransitInteractPacket, NetPeer>(OnTransitInteractPacketReceived);
            RegisterPacket<BotStatePacket, NetPeer>(OnBotStatePacketReceived);
            RegisterPacket<PingPacket, NetPeer>(OnPingPacketReceived);
            RegisterPacket<LoadingProfilePacket, NetPeer>(OnLoadingProfilePacketReceived);
            RegisterPacket<SideEffectPacket, NetPeer>(OnSideEffectPacketReceived);
            RegisterPacket<RequestPacket, NetPeer>(OnRequestPacketReceived);
            RegisterPacket<NetworkSettingsPacket, NetPeer>(OnNetworkSettingsPacketReceived);
            RegisterPacket<InraidQuestPacket, NetPeer>(OnInraidQuestPacketReceived);
            RegisterPacket<EventControllerInteractPacket, NetPeer>(OnEventControllerInteractPacketReceived);

            RegisterReusable<WorldPacket, NetPeer>(OnWorldPacketReceived);
        }

        private void OnEventControllerInteractPacketReceived(EventControllerInteractPacket packet, NetPeer peer)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
            {
                if (gameWorld.RunddansController != null)
                {
                    gameWorld.RunddansController.InteractWithEventObject(player, packet.Data);
                }
            }
        }

        private void OnInraidQuestPacketReceived(InraidQuestPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
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
            SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
        }

        private void OnWorldPacketReceived(WorldPacket packet, NetPeer peer)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                _logger.LogError("OnNewWorldPacketReceived: GameWorld was null!");
                return;
            }

            FikaHostWorld.LootSyncPackets.AddRange(packet.LootSyncStructs);
            SendReusableToAll(packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnVOIPPacketReceived(VOIPPacket packet, NetPeer peer)
        {
            VOIPServer.NetworkReceivedPacket(new(new RemotePeer(peer)), new(packet.Data));
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
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

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

            KeyValuePair<Profile, bool> kvp = packet.Profiles.First();
            if (!_visualProfiles.Any(x => x.Key.ProfileId == kvp.Key.ProfileId))
            {
                _visualProfiles.Add(kvp.Key, _visualProfiles.Count == 0 || kvp.Value);
            }
            FikaBackendUtils.AddPartyMembers(_visualProfiles);
            packet.Profiles = _visualProfiles;
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);

            GenericPacket notifPacket = new()
            {
                NetId = 1,
                Type = EGenericSubPacketType.ClientConnected,
                SubPacket = new ClientConnected(kvp.Key.Info.MainProfileNickname)
            };

            if (!FikaBackendUtils.IsHeadless)
            {
                notifPacket.SubPacket.Execute();
            }
            SendDataToAll(ref notifPacket, DeliveryMethod.ReliableOrdered, peer);

            peer.Tag = kvp.Key.Info.MainProfileNickname;
        }

        private void OnPingPacketReceived(PingPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

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
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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

            _logger.LogError("OnSyncTransitControllersPacketReceived: TransitController was null!");
        }

#if DEBUG
        private void AddDebugPackets()
        {
            RegisterPacket<SpawnItemPacket, NetPeer>(OnSpawnItemPacketReceived);
            RegisterPacket<CommandPacket, NetPeer>(OnCommandPacketReceived);
        }

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
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                FikaGlobals.SpawnItemInWorld(packet.Item, playerToApply);
            }
        }
#endif

        private void OnUsableItemPacketReceived(UsableItemPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleUsableItemPacket(packet);
            }
        }

        public void SendAirdropContainerData(EAirdropType containerType, Item item, int ObjectId)
        {
            _logger.LogInfo($"Sending airdrop details, type: {containerType}, id: {ObjectId}");
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
                _logger.LogError("SendAirdropContainerData: Could not find NetId!");
            }

            SpawnSyncObjectPacket packet = new()
            {
                ObjectType = SynchronizableObjectType.AirDrop,
                SubPacket = new SpawnSyncObjectSubPackets.SpawnAirdrop()
                {
                    ObjectId = ObjectId,
                    IsStatic = false,
                    Position = new(1000, 1000, 1000),
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
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
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
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply is ObservedCoopPlayer observedPlayer)
                {
                    SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
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
                    foreach (CoopPlayer player in _coopHandler.HumanPlayers)
                    {
                        if (player.ProfileId == packet.ProfileId && player is ObservedCoopPlayer observedCoopPlayer)
                        {
                            ReconnectPacket ownCharacterPacket = new()
                            {
                                Type = EReconnectDataType.OwnCharacter,
                                Profile = observedCoopPlayer.Profile,
                                ProfileHealthClass = observedCoopPlayer.NetworkHealthController.Store(),
                                PlayerPosition = observedCoopPlayer.Position
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

                GClass815<int, Throwable>.GStruct45 grenades = gameWorld.Grenades.GetValuesEnumerator();
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

                GClass815<int, WindowBreaker>.GStruct45 windows = gameWorld.Windows.GetValuesEnumerator();
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

                foreach (CoopPlayer player in _coopHandler.Players.Values)
                {
                    if (player.ProfileId == packet.ProfileId)
                    {
                        continue;
                    }

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

                ReconnectPacket finishPacket = new()
                {
                    Type = EReconnectDataType.Finished
                };

                SendDataToPeer(peer, ref finishPacket, DeliveryMethod.ReliableOrdered);
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
                _logger.LogError($"Error parsing {LocalIP}, exception: {ex}");
                return false;
            }
        }

        private async void NatIntroduceRoutine(string natPunchServerIP, int natPunchServerPort, string token, CancellationToken ct)
        {
            _logger.LogInfo("NatIntroduceRoutine started.");

            while (!ct.IsCancellationRequested)
            {
                _netServer.NatPunchModule.SendNatIntroduceRequest(natPunchServerIP, natPunchServerPort, token);

                _logger.LogInfo($"SendNatIntroduceRequest: {natPunchServerIP}:{natPunchServerPort}");

                await Task.Delay(TimeSpan.FromSeconds(15));
            }

            _logger.LogInfo("NatIntroduceRoutine ended.");
        }

        private void OnQuestDropItemPacketReceived(QuestDropItemPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (_hostPlayer == null)
            {
                return;
            }

            if (_hostPlayer.HealthController.IsAlive)
            {
                if (_hostPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestDropItemPacket(ref packet);
                }
            }
        }

        private void OnQuestItemPacketReceived(QuestItemPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (_hostPlayer == null)
            {
                return;
            }

            if (_hostPlayer.HealthController.IsAlive)
            {
                if (_hostPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestItemPacket(ref packet);
                }
            }
        }

        private void OnQuestConditionPacketReceived(QuestConditionPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

            if (_hostPlayer == null)
            {
                return;
            }

            if (_hostPlayer.HealthController.IsAlive)
            {
                if (_hostPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestPacket(ref packet);
                }
            }
        }

        private void OnTextMessagePacketReceived(TextMessagePacket packet, NetPeer peer)
        {
            _logger.LogInfo($"Received message from: {packet.Nickname}, Message: {packet.Message}");

            if (_fikaChat != null)
            {
                _fikaChat.ReceiveMessage(packet.Nickname, packet.Message);
            }

            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        public int PopNetId()
        {
            int netId = _currentNetId;
            _currentNetId++;

            return netId;
        }

        public void SetupGameVariables(CoopPlayer coopPlayer)
        {
            _hostPlayer = coopPlayer;
            if (FikaPlugin.EnableChat.Value)
            {
                _fikaChat = gameObject.AddComponent<FikaChat>();
            }
        }

        private void OnSendCharacterPacketReceived(SendCharacterPacket packet, NetPeer peer)
        {
            if (_coopHandler == null)
            {
                return;
            }

            if (_hostPlayer == null || packet.PlayerInfoPacket.Profile.ProfileId != _hostPlayer.ProfileId)
            {
                _coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
                    packet.PlayerInfoPacket.ControllerId, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
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
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply is ObservedCoopPlayer observedPlayer)
                {
                    if (packet.Packet.SyncType == NetworkHealthSyncPacketStruct.ESyncType.IsAlive && !packet.Packet.Data.IsAlive.IsAlive)
                    {
                        if (packet.KillerId.HasValue)
                        {
                            observedPlayer.SetAggressorData(packet.KillerId, packet.BodyPart, packet.WeaponId); 
                        }
                        observedPlayer.CorpseSyncPacket = packet.CorpseSyncPacket;
                        if (packet.TriggerZones.Length > 0)
                        {
                            observedPlayer.TriggerZones.Clear();
                            observedPlayer.TriggerZones.AddRange(packet.TriggerZones);
                        }
                    }
                    observedPlayer.NetworkHealthController.HandleSyncPacket(packet.Packet);
                    return;
                }
                _logger.LogError($"OnHealthSyncPacketReceived::Player with id {playerToApply.NetId} was not observed. Name: {playerToApply.Profile.Nickname}");
            }
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

            SendDataToAll(ref respondPackage, DeliveryMethod.ReliableOrdered);
        }

        private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
            }
        }

        private void OnInventoryPacketReceived(InventoryPacket packet, NetPeer peer)
        {
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(packet.OperationBytes);
                try
                {
                    OperationCallbackPacket operationCallbackPacket;
                    if (playerToApply.InventoryController is Interface16 inventoryController)
                    {
                        BaseDescriptorClass descriptor = eftReader.ReadPolymorph<BaseDescriptorClass>();
                        OperationDataStruct result = inventoryController.CreateOperationFromDescriptor(descriptor);
#if DEBUG
                        ConsoleScreen.Log($"Received InvOperation: {result.Value.GetType().Name}, Id: {result.Value.Id}");
#endif

                        if (result.Failed)
                        {
                            _logger.LogError($"ItemControllerExecutePacket::Operation conversion failed: {result.Error}");
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
                        handler.OperationResult.Value.method_1(handler.HandleResult);
                    }
                    else
                    {
                        throw new InvalidTypeException($"Inventory controller was not of type {nameof(Interface16)}!");
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError($"ItemControllerExecutePacket::Exception thrown: {exception}");
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
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                if (playerToApply.IsAI || playerToApply.IsYourPlayer)
                {
                    playerToApply.HandleDamagePacket(in packet);
                    return;
                }

                SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            }
        }

        private void OnArmorDamagePacketReceived(ArmorDamagePacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.HandleArmorDamagePacket(packet);
            }
        }

        private void OnWeaponPacketReceived(WeaponPacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            if (_coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                packet.SubPacket.Execute(playerToApply);
            }
        }

        private void OnPlayerStatePacketReceived(PlayerStatePacket packet, NetPeer peer)
        {
            SendDataToAll(ref packet, DeliveryMethod.Unreliable, peer);

            if (_snapshotCount < _snapshots.Length)
            {
                _snapshots[_snapshotCount] = packet;
                _snapshotCount++;
            }
        }

        protected void Update()
        {
            _netServer?.PollEvents();
            float unscaledDelta = Time.unscaledDeltaTime;
            _stateHandle = new UpdateInterpolators(unscaledDelta).Schedule(ObservedCoopPlayers.Count, 4,
                new HandlePlayerStates(NetworkTimeSync.NetworkTime, _snapshots).Schedule(_snapshotCount, 4));

            _statisticsCounter += unscaledDelta;
            if (_statisticsCounter > _sendThreshold)
            {
                _statisticsCounter -= _sendThreshold;
                SendStatisticsPacket();
            }
        }

        protected void LateUpdate()
        {
            _stateHandle.Complete();
            for (int i = 0; i < ObservedCoopPlayers.Count; i++)
            {
                ObservedCoopPlayer player = ObservedCoopPlayers[i];
                if (player.CurrentPlayerState.ShouldUpdate)
                {
                    player.ManualStateUpdate();
                }
            }

            _snapshotCount = 0;
        }

        private void SendStatisticsPacket()
        {
            if (_netServer == null)
            {
                return;
            }

            int fps = (int)(1f / Time.unscaledDeltaTime);
            StatisticsPacket packet = new(fps);

            SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        protected void OnDestroy()
        {
            _netServer?.Stop();
            _stateHandle.Complete();
            _snapshots.Dispose();

            if (_fikaChat != null)
            {
                Destroy(_fikaChat);
            }

            FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
        }

        public void SendDataToAll<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peerToExclude = null) where T : INetSerializable
        {
            _dataWriter.Reset();

            _dataWriter.PutEnum(EPacketType.Serializable);
            if (peerToExclude != null)
            {
                if (NetServer.ConnectedPeersCount > 1)
                {
                    _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
                    _netServer.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod, peerToExclude);
                }
                return;
            }

            _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
            _netServer.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod);
        }

        public void SendReusableToAll<T>(T packet, DeliveryMethod deliveryMethod, NetPeer peerToExlude = null) where T : class, IReusable, new()
        {
            _dataWriter.Reset();

            _dataWriter.PutEnum(EPacketType.Serializable);
            _packetProcessor.Write(_dataWriter, packet);
            if (peerToExlude != null)
            {
                _netServer.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod, peerToExlude);
            }
            else
            {
                _netServer.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod);
            }

            packet.Flush();
        }

        public void SendDataToPeer<T>(NetPeer peer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            _dataWriter.Reset();

            _dataWriter.PutEnum(EPacketType.Serializable);
            _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
            peer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
        }

        public void SendVOIPData(ArraySegment<byte> data, NetPeer peer = null)
        {
            if (peer == null)
            {
                _logger.LogError("SendVOIPData: peer was null!");
                return;
            }

            _dataWriter.Reset();
            _dataWriter.PutEnum(EPacketType.VOIP);
            _dataWriter.PutBytesWithLength(data.Array, data.Offset, (ushort)data.Count);
            peer.Send(_dataWriter.AsReadOnlySpan, DeliveryMethod.Sequenced);
        }

        public void SendVOIPPacket(ref VOIPPacket packet, NetPeer peer = null)
        {
            if (peer == null)
            {
                _logger.LogError("SendVOIPPacket: peer was null!");
                return;
            }

            if (packet.Data == null)
            {
                _logger.LogError("SendVOIPPacket: data was null");
                return;
            }

            SendDataToPeer(peer, ref packet, DeliveryMethod.ReliableOrdered);
        }        

        public void OnPeerConnected(NetPeer peer)
        {
            NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_CONNECTED.Localized(), peer.Port),
                iconType: EFT.Communications.ENotificationIconType.Friend);
            _logger.LogInfo($"Connection established with {peer.Address}:{peer.Port}, id: {peer.Id}");

            HasHadPeer = true;

            FikaEventDispatcher.DispatchEvent(new PeerConnectedEvent(peer, this));
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            _logger.LogError("[SERVER] error " + socketErrorCode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            bool started = false;
            if (_coopHandler != null && _coopHandler.LocalGameInstance != null && Singleton<IFikaGame>.Instance.GameController.RaidStarted)
            {
                started = true;
            }

            if (messageType == UnconnectedMessageType.Broadcast)
            {
                _logger.LogInfo("[SERVER] Received discovery request. Send discovery response");
                NetDataWriter resp = new();
                resp.Put(1);
                _netServer.SendUnconnectedMessage(resp.AsReadOnlySpan, remoteEndPoint);

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
                        _netServer.SendUnconnectedMessage(resp.AsReadOnlySpan, remoteEndPoint);
                        break;

                    case "fika.keepalive":
                        resp = new();
                        resp.Put(data);
                        _netServer.SendUnconnectedMessage(resp.AsReadOnlySpan, remoteEndPoint);

                        if (!_natIntroduceRoutineCts.IsCancellationRequested)
                        {
                            _natIntroduceRoutineCts.Cancel();
                        }
                        break;

                    case "fika.reconnect":
                        resp = new();
                        resp.Put("fika.hello");
                        _netServer.SendUnconnectedMessage(resp.AsReadOnlySpan, remoteEndPoint);
                        break;

                    default:
                        _logger.LogError("PingingRequest: Data was not as expected");
                        break;
                }
            }
            else
            {
                _logger.LogError("PingingRequest: Could not parse string");
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (_coopHandler != null && _coopHandler.LocalGameInstance != null && Singleton<IFikaGame>.Instance.GameController.RaidStarted)
            {
                if (request.Data.GetString() == "fika.reconnect")
                {
                    request.Accept();
                    return;
                }
                _dataWriter.Reset();
                _dataWriter.Put("Raid already started");
                request.Reject(_dataWriter);

                return;
            }

            request.AcceptIfKey("fika.core");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _logger.LogInfo("Peer disconnected " + peer.Port + ", info: " + disconnectInfo.Reason);
            if (disconnectInfo.Reason != DisconnectReason.RemoteConnectionClose)
            {
                NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_DISCONNECTED.Localized(), [peer.Port, disconnectInfo.Reason]),
                        iconType: EFT.Communications.ENotificationIconType.Alert);
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

            if (_netServer.ConnectedPeersCount == 0)
            {
                TimeSinceLastPeerDisconnected = DateTime.Now;
            }

            if (FikaBackendUtils.IsHeadless)
            {
                if (_netServer.ConnectedPeersCount == 0)
                {
                    if (disconnectInfo.Reason != DisconnectReason.RemoteConnectionClose)
                    {
                        _ = Task.Run(WaitBeforeStopping);
                    }
                    else
                    {
                        DisconnectHeadless();
                    }
                }
            }
        }

        private async Task WaitBeforeStopping()
        {
            int minutes = 0;
            while (minutes < 5)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                minutes++;
                _logger.LogInfo($"Waited {minutes} minutes for reconnect...");
            }

            if (_netServer.ConnectedPeersCount == 0)
            {
                _logger.LogInfo("No reconnect was made, stopping session");
                AsyncWorker.RunInMainTread(DisconnectHeadless);
            }
        }

        private void DisconnectHeadless()
        {
            Singleton<IFikaGame>.Instance.Stop(null, ExitStatus.Survived, "");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            switch (reader.GetEnum<EPacketType>())
            {
                case EPacketType.Serializable:
                    _packetProcessor.ReadAllPackets(reader, peer);
                    break;
                case EPacketType.VOIP:
                    VOIPServer.NetworkReceivedPacket(new(new RemotePeer(peer)),
                        new(reader.GetBytesWithLength()));
                    break;
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
            _logger.LogInfo($"OnNatIntroductionResponse: {remoteEndPoint}");

            Task.Run(async () =>
            {
                NetDataWriter data = new();
                data.Put("fika.hello");

                for (int i = 0; i < 20; i++)
                {
                    _netServer.SendUnconnectedMessage(data, localEndPoint);
                    _netServer.SendUnconnectedMessage(data, remoteEndPoint);
                    await Task.Delay(250);
                }
            });
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
            _logger.LogInfo("..:: Fika Server Session Statistics ::..");
            _logger.LogInfo($"Sent packets: {_netServer.Statistics.PacketsSent}");
            _logger.LogInfo($"Sent data: {FikaGlobals.FormatFileSize(_netServer.Statistics.BytesSent)}");
            _logger.LogInfo($"Received packets: {_netServer.Statistics.PacketsReceived}");
            _logger.LogInfo($"Received data: {FikaGlobals.FormatFileSize(_netServer.Statistics.BytesReceived)}");
            _logger.LogInfo($"Packet loss: {_netServer.Statistics.PacketLossPercent}%");
        }

        private class InventoryOperationHandler(OperationDataStruct operationResult, uint operationId, int netId, NetPeer peer, FikaServer server)
        {
            public OperationDataStruct OperationResult = operationResult;
            private readonly uint _operationId = operationId;
            private readonly int _netId = netId;
            private readonly NetPeer _peer = peer;
            private readonly FikaServer _server = server;

            internal void HandleResult(IResult result)
            {
                OperationCallbackPacket operationCallbackPacket;

                if (!result.Succeed)
                {
                    _server._logger.LogError($"Error in operation: {result.Error ?? "An unknown error has occured"}");
                    operationCallbackPacket = new(_netId, _operationId, EOperationStatus.Failed, result.Error ?? "An unknown error has occured");
                    _server.SendDataToPeer(_peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

                    ResyncInventoryIdPacket resyncPacket = new(_netId);
                    _server.SendDataToPeer(_peer, ref resyncPacket, DeliveryMethod.ReliableOrdered);

                    return;
                }

                operationCallbackPacket = new(_netId, _operationId, EOperationStatus.Succeeded);
                _server.SendDataToPeer(_peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}
