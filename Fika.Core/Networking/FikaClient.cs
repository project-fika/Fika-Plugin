// © 2025 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using Dissonance;
using Dissonance.Integrations.MirrorIgnorance;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Jobs;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Components;
using Fika.Core.Main.ObservedClasses.Snapshotting;
using Fika.Core.Main.Patches.VOIP;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Backend;
using Fika.Core.Networking.Packets.Communication;
#if DEBUG
using Fika.Core.Networking.Packets.Debug;
#endif
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.Pooling;
using Fika.Core.Networking.VOIP;
using Fika.Core.UI.Custom;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using static Fika.Core.Networking.NetworkUtils;

namespace Fika.Core.Networking;

/// <summary>
/// Client used to communicate with the <see cref="FikaServer"/>
/// </summary>
public partial class FikaClient : MonoBehaviour, INetEventListener, IFikaNetworkManager
{
    public FikaPlayer MyPlayer;
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
    public List<ObservedPlayer> ObservedPlayers { get; set; }
    public int PlayerAmount { get; set; }

    private NetPacketProcessor _packetProcessor;
    private int _sendRate;
    private NetManager _netClient;
    private CoopHandler _coopHandler;
    private ManualLogSource _logger;
    private NetDataWriter _dataWriter;
    private FikaChatUIScript _fikaChat;
    private string _myProfileId;
    private Queue<BaseInventoryOperationClass> _inventoryOperations;
    private List<int> _missingIds;
    private JobHandle _stateHandle;
    private int _snapshotCount;
    private GenericPacket _genericPacket;

    public async void Init()
    {
        _netClient = new(this)
        {
            UnconnectedMessagesEnabled = true,
            UpdateTime = 50,
            NatPunchEnabled = false,
            AutoRecycle = true,
            IPv6Enabled = false,
            DisconnectTimeout = FikaPlugin.ConnectionTimeout.Value * 1000,
            EnableStatistics = true,
            MaxConnectAttempts = 5,
            ReconnectDelay = 1 * 1000,
            ChannelsCount = 2
        };

        _packetProcessor = new();
        _dataWriter = new();
        _logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Client");
        _inventoryOperations = new(8);
        _missingIds = [];
        _snapshotCount = 0;
        ObservedPlayers = [];
        PlayerAmount = 1;

        Ping = 0;
        ServerFPS = 0;
        ReadyClients = 0;

        NetworkGameSession.Rtt = 0;
        NetworkGameSession.LossPercent = 0;

        _myProfileId = FikaBackendUtils.Profile.ProfileId;
        _genericPacket = new();

        PlayerSnapshots.Init();

        RegisterPacketsAndTypes();

#if DEBUG
        AddDebugPackets();
#endif            

        await NetManagerUtils.CreateCoopHandler();

        if (FikaBackendUtils.IsHostNatPunch)
        {
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

    public async Task InitializeVOIP()
    {
        VoipSettingsClass voipHandler = FikaGlobals.VOIPHandler;

        GClass1072 controller = Singleton<SharedGameSettingsClass>.Instance.Sound.Controller;
        if (voipHandler.MicrophoneChecked)
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

        RegisterPacket<InformationPacket>(OnInformationPacketReceived);
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
        RegisterPacket<BTRInteractionPacket>(OnBTRInteractionPacketReceived);
        RegisterPacket<FlareSuccessPacket>(OnFlareSuccessPacketReceived);
        RegisterPacket<BufferZonePacket>(OnBufferZonePacketReceived);
        RegisterPacket<ResyncInventoryIdPacket>(OnResyncInventoryIdPacketReceived);
        RegisterPacket<NetworkSettingsPacket>(OnNetworkSettingsPacketReceived);
        RegisterPacket<SyncTransitControllersPacket>(OnSyncTransitControllersPacketReceived);
        RegisterPacket<TransitEventPacket>(OnTransitEventPacketReceived);
        RegisterPacket<BotStatePacket>(OnBotStatePacketReceived);
        RegisterPacket<LoadingProfilePacket>(OnLoadingProfilePacketReceived);
        RegisterPacket<SideEffectPacket>(OnSideEffectPacketReceived);
        RegisterPacket<RequestPacket>(OnRequestPacketReceived);
        RegisterPacket<InRaidQuestPacket>(OnInraidQuestPacketReceived);
        RegisterPacket<EventControllerEventPacket>(OnEventControllerEventPacketReceived);
        RegisterPacket<EventControllerInteractPacket>(OnEventControllerInteractPacketReceived);
        RegisterPacket<SyncTrapsPacket>(OnSyncTrapsPacketReceived);
        RegisterPacket<StashesPacket>(OnStashesPacketReceived);

        RegisterReusable<WorldPacket>(OnWorldPacketReceived);

        RegisterNetReusable<WeaponPacket>(OnWeaponPacketReceived);
        RegisterNetReusable<CommonPlayerPacket>(OnCommonPlayerPacketReceived);
        RegisterNetReusable<GenericPacket>(OnGenericPacketReceived);
    }

#if DEBUG
    private void AddDebugPackets()
    {
        RegisterPacket<SpawnItemPacket>(OnSpawnItemPacketReceived);
    }
#endif

    public void SetupGameVariables(FikaPlayer fikaPlayer)
    {
        MyPlayer = fikaPlayer;
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
        _netClient?.PollEvents();
        _stateHandle = new UpdateInterpolators(Time.unscaledDeltaTime).ScheduleParallel(ObservedPlayers.Count, 4,
            new HandlePlayerStates(NetworkTimeSync.NetworkTime).ScheduleParallel(_snapshotCount, 4, default));

        int inventoryOps = _inventoryOperations.Count;
        if (inventoryOps > 0)
        {
            if (_inventoryOperations.Peek().WaitingForForeignEvents())
            {
                return;
            }
            _inventoryOperations.Dequeue().method_1(HandleResult);
        }

        if (Input.GetKeyDown(FikaPlugin.ChatKey.Value.MainKey))
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
            for (int i = 0; i < ObservedPlayers.Count; i++)
            {
                ObservedPlayer player = ObservedPlayers[i];
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
                ArraySegmentPooling.Return(PlayerSnapshots.Snapshots[i]);
            }
            _snapshotCount = 0;
        }
    }

    protected void OnDestroy()
    {
        _netClient?.Stop();
        try
        {
            _stateHandle.Complete();
        }
        finally
        {
            for (int i = 0; i < _snapshotCount; i++)
            {
                ArraySegmentPooling.Return(PlayerSnapshots.Snapshots[i]);
            }
            _snapshotCount = 0;
        }
        _genericPacket.Clear();

        PoolUtils.ReleaseAll();
        PlayerSnapshots.Clear();

        if (_fikaChat != null)
        {
            Destroy(_fikaChat);
        }

        FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
    }

    public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, bool broadcast = false) where T : INetSerializable
    {
        NetPeer peer = _netClient.FirstPeer;
        if (peer != null)
        {
            _dataWriter.Reset();
            _dataWriter.Put(broadcast);
            _dataWriter.PutEnum(EPacketType.Serializable);

            _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
            peer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
        }
    }

    public void SendPlayerState(ref PlayerStatePacket packet)
    {
        _dataWriter.Reset();
        _dataWriter.Put(true);
        _dataWriter.PutEnum(EPacketType.PlayerState);
        _dataWriter.PutUnmanaged(packet);

        _netClient.SendToAll(_dataWriter.AsReadOnlySpan, DeliveryMethod.Unreliable);
    }

    public void SendGenericPacket(EGenericSubPacketType type, IPoolSubPacket subpacket, bool broadcast = false, NetPeer peerToIgnore = null)
    {
        GenericPacket packet = _genericPacket;
        packet.Type = type;
        packet.SubPacket = subpacket;
        SendNetReusable(ref packet, DeliveryMethod.ReliableOrdered, broadcast);
    }

    public void SendNetReusable<T>(ref T packet, DeliveryMethod deliveryMethod, bool broadcast = false, NetPeer peerToIgnore = null) where T : INetReusable
    {
        _dataWriter.Reset();
        _dataWriter.Put(broadcast);
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetReusable(_dataWriter, ref packet);
        _netClient.SendToAll(_dataWriter.AsReadOnlySpan, deliveryMethod);

        packet.Clear();
    }

    public void SendDataToPeer<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peer) where T : INetSerializable
    {
        _dataWriter.Reset();
        _dataWriter.Put(false);
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
        peer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
    }

    public void SendVOIPData(ArraySegment<byte> data, DeliveryMethod deliveryMethod, NetPeer peer = null)
    {
        NetPeer firstPeer = _netClient.FirstPeer;
        if (firstPeer != null)
        {
            _dataWriter.Reset();

            _dataWriter.Put(false);
            _dataWriter.PutEnum(EPacketType.VOIP);
            _dataWriter.Put(data.AsSpan());
            firstPeer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
        }
    }

    /// <summary>
    /// Sends a reusable packet
    /// </summary>
    /// <typeparam name="T">The <see cref="IReusable"/> to send</typeparam>
    /// <param name="packet">The <see cref="INetSerializable"/> to send</param>
    /// <param name="deliveryMethod">The deliverymethod</param>
    /// <remarks>
    /// Reusable will always be of type broadcast when sent from a client
    /// </remarks>
    public void SendReusable<T>(T packet, DeliveryMethod deliveryMethod) where T : class, IReusable, new()
    {
        NetPeer peer = _netClient.FirstPeer;
        if (peer != null)
        {
            _dataWriter.Reset();

            _dataWriter.Put(false);
            _dataWriter.PutEnum(EPacketType.Serializable);
            _packetProcessor.Write(_dataWriter, packet);
            peer.Send(_dataWriter.AsReadOnlySpan, deliveryMethod);
        }

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
            case EPacketType.PlayerState:
                if (_snapshotCount < PlayerSnapshots.Snapshots.Length)
                {
                    PlayerSnapshots.Snapshots[_snapshotCount++] = ArraySegmentPooling.Get(reader.GetRemainingBytesSpan());
                }
                break;
            case EPacketType.BTR:
                BTRDataPacketStruct data = reader.GetUnmanaged<BTRDataPacketStruct>();
                BTRControllerClass.Instance?.SyncBTRVehicleFromServer(data);
                break;
            case EPacketType.VOIP:
                if (VOIPClient != null)
                {
                    VOIPClient.NetworkReceivedPacket(reader.GetRemainingBytesSegment());
                }
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.BasicMessage && _netClient.ConnectedPeersCount == 0 && reader.GetInt() == 1)
        {
            _logger.LogInfo("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
            _netClient.Connect(remoteEndPoint, NetDataWriter.FromString("fika.core").AsReadOnlySpan);
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
        _logger.LogInfo("..:: Fika Client Session Statistics ::..");
        _logger.LogInfo($"Sent packets: {_netClient.Statistics.PacketsSent}");
        _logger.LogInfo($"Sent data: {FikaGlobals.FormatFileSize(_netClient.Statistics.BytesSent)}");
        _logger.LogInfo($"Received packets: {_netClient.Statistics.PacketsReceived}");
        _logger.LogInfo($"Received data: {FikaGlobals.FormatFileSize(_netClient.Statistics.BytesReceived)}");
        _logger.LogInfo($"Packet loss: {_netClient.Statistics.PacketLossPercent}%");
    }

    private void HandleInventoryPacket(InventoryPacket packet, FikaPlayer player)
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
                if (controller is Interface18 networkController)
                {
                    using GClass1283 eftReader = PacketToEFTReaderAbstractClass.Get(packet.OperationBytes);
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
