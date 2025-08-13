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
#if DEBUG
#endif
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.HostClasses;
using Fika.Core.Main.ObservedClasses.Snapshotting;
using Fika.Core.Main.Patches.VOIP;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Jobs;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.VOIP;
using Fika.Core.UI.Custom;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if DEBUG
using Fika.Core.Networking.Packets.Debug;
# endif
using static Fika.Core.Networking.NetworkUtils;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Pooling;
using Fika.Core.Networking.Open.Nat;
using Fika.Core.Networking.Open.Nat.Enums;
using Fika.Core.Networking.Open.Nat.Exceptions;
using Fika.Core.Networking.Models;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Generic.SubPackets;

namespace Fika.Core.Networking;

/// <summary>
/// Server used to synchronize all <see cref="FikaClient"/>s
/// </summary>
public partial class FikaServer : MonoBehaviour, INetEventListener, INatPunchListener, GInterface262, IFikaNetworkManager
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
    public List<ObservedPlayer> ObservedCoopPlayers { get; set; }
    public int PlayerAmount { get; set; }

    private int _sendRate;
    private NetPacketProcessor _packetProcessor;
    private FikaPlayer _hostPlayer;
    private string _externalIp;
    private NetManager _netServer;
    private DateTime? _gameStartTime;
    private NetDataWriter _dataWriter;
    private int _port;
    private CoopHandler _coopHandler;
    private ManualLogSource _logger;
    private int _currentNetId;
    private FikaChatUIScript _fikaChat;
    private RaidAdminUIScript _raidAdminUIScript;
    private CancellationTokenSource _natIntroduceRoutineCts;
    private float _statisticsCounter;
    private float _sendThreshold;
    private Dictionary<Profile, bool> _visualProfiles;
    private Dictionary<string, int> _cachedConnections;
    private JobHandle _stateHandle;
    [NativeDisableContainerSafetyRestriction]
    private NativeArray<ArraySegment<byte>> _snapshots;
    private int _snapshotCount;
    private GenericPacket _genericPacket;

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
        PlayerAmount = 1;

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
        _genericPacket = new()
        {
            NetId = NetId
        };

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
                if (IPAddress.TryParse(FikaPlugin.ForceBindIP.Value, out IPAddress ipv4))
                {
                    _netServer.Start(ipv4, IPAddress.IPv6Any, _port);
                }
                else
                {
                    string error = $"{FikaPlugin.ForceBindIP.Value} could not be parsed into a valid IP, raid will not start";
                    NotificationManagerClass.DisplayWarningNotification(error, EFT.Communications.ENotificationDurationType.Long);
                    throw new ParseException(error);
                }
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

        _raidAdminUIScript = RaidAdminUIScript.Create(this, _netServer);
    }

    public async Task InitializeVOIP()
    {
        VoipSettingsClass voipHandler = FikaGlobals.VOIPHandler;
        GClass1069 controller = Singleton<SharedGameSettingsClass>.Instance.Sound.Controller;
        if (voipHandler.MicrophoneChecked && !FikaBackendUtils.IsHeadless)
        {
            controller.ResetVoipDisabledReason();
            DissonanceComms.ClientPlayerId = FikaGlobals.GetProfile(RaidSide == ESideType.Savage).ProfileId;
            await LoadSceneClass.LoadScene(AssetsManagerSingletonClass.Manager,
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
            await LoadSceneClass.LoadScene(AssetsManagerSingletonClass.Manager,
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

        return;
    }

    private void RegisterPacketsAndTypes()
    {
        PoolUtils.CreateAll();

        RegisterCustomType(FikaSerializationExtensions.PutRagdollStruct, FikaSerializationExtensions.GetRagdollStruct);
        RegisterCustomType(FikaSerializationExtensions.PutArtilleryStruct, FikaSerializationExtensions.GetArtilleryStruct);
        RegisterCustomType(FikaSerializationExtensions.PutGrenadeStruct, FikaSerializationExtensions.GetGrenadeStruct);
        RegisterCustomType(FikaSerializationExtensions.PutAirplaneDataPacketStruct, FikaSerializationExtensions.GetAirplaneDataPacketStruct);
        RegisterCustomType(FikaSerializationExtensions.PutLootSyncStruct, FikaSerializationExtensions.GetLootSyncStruct);

        RegisterPacket<InformationPacket, NetPeer>(OnInformationPacketReceived);
        RegisterPacket<TextMessagePacket, NetPeer>(OnTextMessagePacketReceived);
        RegisterPacket<QuestConditionPacket, NetPeer>(OnQuestConditionPacketReceived);
        RegisterPacket<QuestItemPacket, NetPeer>(OnQuestItemPacketReceived);
        RegisterPacket<QuestDropItemPacket, NetPeer>(OnQuestDropItemPacketReceived);
        RegisterPacket<InteractableInitPacket, NetPeer>(OnInteractableInitPacketReceived);
        RegisterPacket<WorldLootPacket, NetPeer>(OnWorldLootPacketReceived);
        RegisterPacket<ReconnectPacket, NetPeer>(OnReconnectPacketReceived);
        RegisterPacket<BTRInteractionPacket, NetPeer>(OnBTRInteractionPacketReceived);
        RegisterPacket<ResyncInventoryIdPacket, NetPeer>(OnResyncInventoryIdPacketReceived);
        RegisterPacket<SyncTransitControllersPacket, NetPeer>(OnSyncTransitControllersPacketReceived);
        RegisterPacket<TransitInteractPacket, NetPeer>(OnTransitInteractPacketReceived);
        RegisterPacket<BotStatePacket, NetPeer>(OnBotStatePacketReceived);
        RegisterPacket<LoadingProfilePacket, NetPeer>(OnLoadingProfilePacketReceived);
        RegisterPacket<SideEffectPacket, NetPeer>(OnSideEffectPacketReceived);
        RegisterPacket<RequestPacket, NetPeer>(OnRequestPacketReceived);
        RegisterPacket<NetworkSettingsPacket, NetPeer>(OnNetworkSettingsPacketReceived);
        RegisterPacket<InRaidQuestPacket, NetPeer>(OnInraidQuestPacketReceived);
        RegisterPacket<EventControllerInteractPacket, NetPeer>(OnEventControllerInteractPacketReceived);

        RegisterReusable<WorldPacket, NetPeer>(OnWorldPacketReceived);

        RegisterNetReusable<WeaponPacket, NetPeer>(OnWeaponPacketReceived);
        RegisterNetReusable<CommonPlayerPacket, NetPeer>(OnCommonPlayerPacketReceived);
        RegisterNetReusable<GenericPacket, NetPeer>(OnGenericPacketReceived);
    }

#if DEBUG
    private void AddDebugPackets()
    {
        RegisterPacket<SpawnItemPacket, NetPeer>(OnSpawnItemPacketReceived);
        RegisterPacket<CommandPacket, NetPeer>(OnCommandPacketReceived);
    }
#endif

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

        SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    public void SendFlareSuccessEvent(string profileId, bool canSendAirdrop)
    {
        FlareSuccessPacket packet = new(profileId, canSendAirdrop);
        SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }

    private bool ValidateLocalIP(string localIP)
    {
        if (string.IsNullOrWhiteSpace(localIP))
        {
            return false;
        }

        if (!IPAddress.TryParse(localIP, out IPAddress address))
        {
            return false;
        }

        // Only check IPv4
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        byte[] bytes = address.GetAddressBytes();

        return
            bytes[0] == 10 ||                                       // 10.0.0.0/8
            (bytes[0] == 192 && bytes[1] == 168) ||                 // 192.168.0.0/16
            (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31);  // 172.16.0.0/12
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

    public int PopNetId()
    {
        int netId = _currentNetId;
        _currentNetId++;

        return netId;
    }

    public void SetupGameVariables(FikaPlayer fikaPlayer)
    {
        _hostPlayer = fikaPlayer;
    }

    public void CreateFikaChat()
    {
        if (FikaPlugin.EnableChat.Value)
        {
            _fikaChat = FikaChatUIScript.Create();
        }
    }

    protected void Update()
    {
        _netServer?.PollEvents();
        float unscaledDelta = Time.unscaledDeltaTime;
        _stateHandle = new InterpolatorJob(unscaledDelta, NetworkTimeSync.NetworkTime, _snapshots, _snapshotCount)
            .Schedule();

        _statisticsCounter += unscaledDelta;
        if (_statisticsCounter > _sendThreshold)
        {
            _statisticsCounter -= _sendThreshold;
            SendStatisticsPacket();
        }

        if (!FikaBackendUtils.IsHeadless && Input.GetKeyDown(FikaPlugin.ChatKey.Value.MainKey))
        {
            if (_fikaChat != null)
            {
                _fikaChat.ToggleChat();
            }
        }
    }

    protected void LateUpdate()
    {
        try
        {
            _stateHandle.Complete();
            for (int i = 0; i < ObservedCoopPlayers.Count; i++)
            {
                ObservedPlayer player = ObservedCoopPlayers[i];
                if (player.CurrentPlayerState.ShouldUpdate)
                {
                    player.ManualStateUpdate();
                }
            }
        }
        finally
        {
            for (int i = 0; i < _snapshotCount; i++)
            {
                ArraySegmentPooling.Return(_snapshots[i]);
            }
            _snapshotCount = 0;
        }
    }

    private void SendStatisticsPacket()
    {
        if (_netServer == null)
        {
            return;
        }

        int fps = (int)(1f / Time.unscaledDeltaTime);
        StatisticsPacket packet = new(fps);

        SendData(ref packet, DeliveryMethod.ReliableUnordered);
    }

    protected void OnDestroy()
    {
        _netServer?.Stop();
        _stateHandle.Complete();
        _snapshots.Dispose();
        _genericPacket.Clear();

        PoolUtils.ReleaseAll();

        if (_fikaChat != null)
        {
            Destroy(_fikaChat);
        }
        if (_raidAdminUIScript != null)
        {
            Destroy(_raidAdminUIScript);
        }

        FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
    }

    public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, bool multicast = false) where T : INetSerializable
    {
        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
        _netServer.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod);
    }

    public void SendGenericPacket(EGenericSubPacketType type, IPoolSubPacket subpacket, bool multicast = false, NetPeer peerToIgnore = null)
    {
        GenericPacket packet = _genericPacket;
        packet.Type = type;
        packet.SubPacket = subpacket;
        SendNetReusable(ref packet, DeliveryMethod.ReliableOrdered, multicast, peerToIgnore);
    }

    public void SendGenericPacketToPeer(EGenericSubPacketType type, IPoolSubPacket subpacket, NetPeer peer)
    {
        GenericPacket packet = _genericPacket;
        packet.Type = type;
        packet.SubPacket = subpacket;
        SendNetReusableToPeer(ref packet, DeliveryMethod.ReliableOrdered, peer);
    }

    public void SendNetReusable<T>(ref T packet, DeliveryMethod deliveryMethod, bool multicast = false, NetPeer peerToIgnore = null) where T : INetReusable
    {
        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetReusable(_dataWriter, ref packet);
        _netServer.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod, peerToIgnore);

        packet.Clear();
    }

    public void SendNetReusableToPeer<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peer) where T : INetReusable
    {
        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetReusable(_dataWriter, ref packet);
        peer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);

        packet.Clear();
    }

    public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peerToIgnore) where T : INetSerializable
    {
        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
        _netServer.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod, peerToIgnore);
    }

    public void SendPlayerState(ref PlayerStatePacket packet)
    {
        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.PlayerState);
        _dataWriter.PutUnmanaged(packet);

        _netServer.SendToAll(_dataWriter.AsReadOnlySpan, DeliveryMethod.Unreliable);
    }

    public void SendDataToPeer<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peer) where T : INetSerializable
    {
        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
        peer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
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

    public void SendVOIPData(ArraySegment<byte> data, DeliveryMethod deliveryMethod, NetPeer peer = null)
    {
        if (peer == null)
        {
            _logger.LogError("SendVOIPData: peer was null!");
            return;
        }

        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.VOIP);
        _dataWriter.Put(data.AsSpan());
        peer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
    }

    public void SendBTRPacket(ref BTRDataPacketStruct data)
    {
        _dataWriter.Reset();
        _dataWriter.PutEnum(EPacketType.BTR);
        _dataWriter.PutUnmanaged(data);

        _netServer.SendToAll(_dataWriter.AsReadOnlySpan, DeliveryMethod.Unreliable);
    }

    public void OnPeerConnected(NetPeer peer)
    {
        NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_CONNECTED.Localized(), peer.Port),
            iconType: EFT.Communications.ENotificationIconType.Friend);
        _logger.LogInfo($"Connection established with {peer.Address}:{peer.Port}, id: {peer.Id}");

        HasHadPeer = true;
        PlayerAmount++;

        FikaEventDispatcher.DispatchEvent(new PeerConnectedEvent(peer, this));

        SendGenericPacket(EGenericSubPacketType.UpdateBackendData,
            UpdateBackendData.FromValue(PlayerAmount), true);
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
            NetDataWriter resp = new();
            string guid = FikaBackendUtils.ServerGuid.ToString();
            if (data == guid)
            {
                bool reconnect = reader.GetBool();
                resp.Put(started && reconnect ? "fika.inprogress" : "fika.hello");
                _netServer.SendUnconnectedMessage(resp.AsReadOnlySpan, remoteEndPoint);
            }
            else if (data == "fika.keepalive")
            {
                resp.Put(data);
                _netServer.SendUnconnectedMessage(resp.AsReadOnlySpan, remoteEndPoint);

                if (!_natIntroduceRoutineCts.IsCancellationRequested)
                {
                    _natIntroduceRoutineCts.Cancel();
                }
            }
            else
            {
                _logger.LogError($"PingingRequest::Data was not as expected: {data}");
                resp.Put("fika.reject");
                _netServer.SendUnconnectedMessage(resp.AsReadOnlySpan, remoteEndPoint);
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
            ClientDisconnected disconnectPacket = ClientDisconnected.FromValue(nickname);
            disconnectPacket.Execute();
            SendGenericPacket(EGenericSubPacketType.ClientDisconnected,
                disconnectPacket, true, peer);
        }

        PlayerAmount--;
        SendGenericPacket(EGenericSubPacketType.UpdateBackendData,
            UpdateBackendData.FromValue(PlayerAmount), true);

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
        // check for multicast
        if (reader.GetByte() == 1)
        {
            // we're skipping the multicast byte as clients do not care about it
            _netServer.SendToAll(reader.GetRemainingBytesSpan(), deliveryMethod, peer);
        }

        switch (reader.GetEnum<EPacketType>())
        {
            case EPacketType.Serializable:
                _packetProcessor.ReadAllPackets(reader, peer);
                break;
            case EPacketType.PlayerState:
                if (_snapshotCount < _snapshots.Length)
                {
                    _snapshots[_snapshotCount++] = ArraySegmentPooling.Get(reader.GetRemainingBytesSpan());
                }
                break;
            case EPacketType.VOIP:
                VOIPServer.NetworkReceivedPacket(new(new RemotePeer(peer)),
                    reader.GetRemainingBytesSegment());
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

    public void RegisterNetReusable<T>(Action<T> handle) where T : class, INetReusable, new()
    {
        _packetProcessor.SubscribeNetReusable(handle);
    }

    public void RegisterNetReusable<T, TUserData>(Action<T, TUserData> handle) where T : class, INetReusable, new()
    {
        _packetProcessor.SubscribeNetReusable(handle);
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

    public void ToggleAdminUI()
    {
        if (_raidAdminUIScript != null)
        {
            _raidAdminUIScript.Toggle();
        }
    }
}
