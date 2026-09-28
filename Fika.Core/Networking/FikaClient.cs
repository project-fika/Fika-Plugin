// © 2026 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using Dissonance;
using Dissonance.Integrations.MirrorIgnorance;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.Settings;
using EFT.UI;
using EFT.Vehicle;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Components;
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
using Fika.Core.Networking.Packets.FirearmController.SubPackets;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Packets.Player;
using Fika.Core.Networking.Packets.Player.Common;
using Fika.Core.Networking.Packets.Player.Common.SubPackets;
using Fika.Core.Networking.Packets.World;
using Fika.Core.Networking.Snapshotting;
using Fika.Core.Networking.VOIP;
using Fika.Core.UI.Custom;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Fika.Core.Networking.NetworkUtils;
using InteractionPacket = Fika.Core.Networking.Packets.Player.Common.SubPackets.InteractionPacket;
using MountingPacket = Fika.Core.Networking.Packets.Player.Common.SubPackets.MountingPacket;
using ReloadMagPacket = Fika.Core.Networking.Packets.FirearmController.SubPackets.ReloadMagPacket;
using RollCylinderPacket = Fika.Core.Networking.Packets.FirearmController.SubPackets.RollCylinderPacket;

namespace Fika.Core.Networking;

/// <summary>
/// Client used to communicate with the <see cref="FikaServer"/>
/// </summary>
public sealed partial class FikaClient : MonoBehaviour, INetEventListener, IFikaNetworkManager
{
    public FikaPlayer MyPlayer;
    public int Ping;
    public int ServerFPS;
    public int ReadyClients;
    public bool HostReady;
    public bool HostLoaded;
    public bool HostReceivedLocation;
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
    public Stash TemporaryStash { get; set; }
    public bool StrictInventorySync { get; set; }
    public Queue<EFT.InventoryLogic.Operations.AbstractOperation> InventoryOperations
    {
        get
        {
            return _inventoryOperations;
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
    private Queue<EFT.InventoryLogic.Operations.AbstractOperation> _inventoryOperations;
    private List<int> _missingIds;
    private DateTime _startTime;
    private Callback _handleInventoryOperationCallback;

    public async Task Init()
    {
        _netClient = new(this)
        {
            UnconnectedMessagesEnabled = true,
            UpdateTime = 50,
            NatPunchEnabled = false,
            AutoRecycle = true,
            IPv6Enabled = true,
            DisconnectTimeout = FikaPlugin.Instance.Settings.ConnectionTimeout.Value * 1000,
            EnableStatistics = true,
            MaxConnectAttempts = 5,
            ReconnectDelay = 1 * 1000,
            ChannelsCount = 2,
            UseNativeSockets = NativeSocket.IsSupported
        };

        _packetProcessor = new();
        _dataWriter = new();
        _logger = Logger.CreateLogSource("Fika.Client");
        _inventoryOperations = new(8);
        _missingIds = [];
        _startTime = DateTime.Now;
        _handleInventoryOperationCallback = HandleResult;
        ObservedPlayers = [];
        PlayerAmount = 1;

        Ping = 0;
        ServerFPS = 0;
        ReadyClients = 0;

        TemporaryStash = Singleton<ItemFactory>.Instance.CreateFakeStash();

        NetworkGameSession.Rtt = 0;
        NetworkGameSession.LossPercent = 0;

        _myProfileId = FikaBackendUtils.Profile.ProfileId;

        await RegisterPacketsAndTypes();

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

        var endPoint = FikaBackendUtils.RemoteEndPoint;
        var connectString = FikaBackendUtils.IsReconnect ? "fika.reconnect" : "fika.core";

        if (endPoint.Address == null)
        {
            Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error",
                "Unable to connect to the raid server. IP and/or Port was empty when requesting data!");
        }
        else
        {
            ServerConnection = _netClient.Connect(FikaBackendUtils.RemoteEndPoint, connectString);
        }
    }

    public async Task InitializeVOIP()
    {
        var voipHandler = FikaGlobals.VOIPHandler;

        var controller = Singleton<SettingsManager>.Instance.Sound.Controller;
        if (voipHandler.MicrophoneChecked)
        {
            controller.ResetVoipDisabledReason();
            DissonanceComms.ClientPlayerId = FikaGlobals.GetProfile(RaidSide == ESideType.Savage).ProfileId;
            await AssetsManagerExtension.LoadScene(EFT.Assets.Manager,
                Scenes.DissonanceSetupScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);

            MirrorIgnoranceCommsNetwork mirrorCommsNetwork;
            do
            {
                mirrorCommsNetwork = FindObjectOfType<MirrorIgnoranceCommsNetwork>();
                await Task.Yield();
            } while (mirrorCommsNetwork == null);

            var gameObj = mirrorCommsNetwork.gameObject;
            var commNet = gameObj.AddComponent<FikaCommsNetwork>();
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

    private Task RegisterPacketsAndTypes()
    {
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
        RegisterPacket<MessagePacket>(OnMessagePacketReceived);
        RegisterPacket<LoadingScreenPacket>(OnLoadingScreenPacketReceived);
        RegisterPacket<LoadingScreenPlayersPacket>(OnLoadingScreenPlayersPacketReceived);
        RegisterPacket<SyncEventPacket>(OnSyncEventPacketReceived);
        RegisterPacket<ClearSnapshotterPacket>(OnClearSnapshotterPacketReceived);
        RegisterPacket<ProceedResponsePacket>(OnProceedResponsePacketReceived);
        RegisterPacket<SpawnItemInInventoryPacket>(SpawnItemInInventoryPacketReceived);

        RegisterReusable<WorldPacket>(OnWorldPacketReceived);

        return Task.CompletedTask;
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
        if (FikaPlugin.Instance.Settings.EnableChat.Value)
        {
            _fikaChat = FikaChatUIScript.Create();
        }
    }

    private void Update()
    {
        _netClient?.PollEvents();

        var networkTime = NetworkTimeSync.NetworkTime;
        for (var i = 0; i < ObservedPlayers.Count; i++)
        {
            ObservedPlayers[i].ManualStateUpdate(networkTime);
        }

        HandleInventoryOperations();

        if (Input.GetKeyDown(FikaPlugin.Instance.Settings.ChatKey.Value.MainKey))
        {
            if (_fikaChat != null)
            {
                _fikaChat.ToggleChat();
            }
        }
    }

    /// <summary>
    /// Processes queued <see cref="EFT.InventoryLogic.Operations.AbstractOperation"/>
    /// </summary>
    private void HandleInventoryOperations()
    {
        while (_inventoryOperations.Count > 0)
        {
            if (_inventoryOperations.Peek().WaitingForForeignEvents())
            {
                return;
            }

            _inventoryOperations.Dequeue()
                .Execute(_handleInventoryOperationCallback);
        }
    }

    private void OnDestroy()
    {
        _netClient?.Stop();

        if (_fikaChat != null)
        {
            Destroy(_fikaChat);
        }

        FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
    }

    public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, bool broadcast = false) where T : INetSerializable
    {
        var peer = ServerConnection;
        if (peer != null)
        {
            _dataWriter.Reset();
            _dataWriter.Put(broadcast);
            _dataWriter.PutEnum(EPacketType.Serializable);

            _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
            peer.Send(_dataWriter.AsReadOnlySpan(), deliveryMethod);
        }
    }

    public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peerToIgnore, bool broadcast = false) where T : INetSerializable
    {
        SendData(ref packet, deliveryMethod, broadcast);
    }

    public void SendPlayerState(ref PlayerStateData packet)
    {
        _dataWriter.Reset();
        _dataWriter.Put(true);
        _dataWriter.PutEnum(EPacketType.PlayerState);
        _dataWriter.Put(NetworkTimeSync.NetworkTime);
        _dataWriter.PutUnmanaged(packet);

        _netClient.SendToAll(_dataWriter.AsReadOnlySpan(), DeliveryMethod.Unreliable);
    }

    public void SendGenericPacket<T>(in T genericPacket, DeliveryMethod deliveryMethod, bool broadcast = false, NetPeer peerToIgnore = null) where T : struct, IGenericPacket
    {
        _dataWriter.Reset();
        _dataWriter.Put(broadcast);
        _dataWriter.PutEnum(EPacketType.Generic);

        _dataWriter.PutEnum(genericPacket.Type);
        genericPacket.Serialize(_dataWriter);
        _netClient.SendToAll(_dataWriter.AsReadOnlySpan(), deliveryMethod);
    }

    public void SendPlayerPacket<T>(in T playerPacket, int netId, DeliveryMethod deliveryMethod, bool broadcast = false, NetPeer peerToIgnore = null) where T : struct, IPlayerPacket
    {
        _dataWriter.Reset();
        _dataWriter.Put(broadcast);
        _dataWriter.PutEnum(EPacketType.Player);

        _dataWriter.Put(netId);
        _dataWriter.PutEnum(playerPacket.Type);
        playerPacket.Serialize(_dataWriter);
        _netClient.SendToAll(_dataWriter.AsReadOnlySpan(), deliveryMethod);
    }

    public void SendFirearmPacket<T>(in T firearmPacket, int netId, DeliveryMethod deliveryMethod, bool broadcast = false, NetPeer peerToIgnore = null) where T : struct, IFirearmPacket
    {
        _dataWriter.Reset();
        _dataWriter.Put(broadcast);
        _dataWriter.PutEnum(EPacketType.Firearm);

        _dataWriter.Put(netId);
        _dataWriter.PutEnum(firearmPacket.Type);
        firearmPacket.Serialize(_dataWriter);
        _netClient.SendToAll(_dataWriter.AsReadOnlySpan(), deliveryMethod);
    }

    public void SendNetReusable<T>(ref T packet, DeliveryMethod deliveryMethod, bool broadcast = false, NetPeer peerToIgnore = null) where T : INetReusable
    {
        _dataWriter.Reset();
        _dataWriter.Put(broadcast);
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetReusable(_dataWriter, ref packet);
        _netClient.SendToAll(_dataWriter.AsReadOnlySpan(), deliveryMethod);

        packet.Clear();
    }

    public void SendDataToPeer<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peer) where T : INetSerializable
    {
        _dataWriter.Reset();
        _dataWriter.Put(false);
        _dataWriter.PutEnum(EPacketType.Serializable);

        _packetProcessor.WriteNetSerializable(_dataWriter, ref packet);
        peer.Send(_dataWriter.AsReadOnlySpan(), deliveryMethod);
    }

    public void SendVOIPData(ArraySegment<byte> data, DeliveryMethod deliveryMethod, NetPeer peer = null)
    {
        var firstPeer = _netClient.FirstPeer;
        if (firstPeer != null)
        {
            _dataWriter.Reset();

            _dataWriter.Put(false);
            _dataWriter.PutEnum(EPacketType.VOIP);
            _dataWriter.Put(data.AsSpan());
            firstPeer.Send(_dataWriter.AsReadOnlySpan(), deliveryMethod);
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
        var peer = _netClient.FirstPeer;
        if (peer != null)
        {
            _dataWriter.Reset();

            _dataWriter.Put(true);
            _dataWriter.PutEnum(EPacketType.Serializable);
            _packetProcessor.Write(_dataWriter, packet);
            peer.Send(_dataWriter.AsReadOnlySpan(), deliveryMethod);
        }

        packet.Flush();
    }

    public void OnPeerConnected(NetPeer peer)
    {
        NotificationManager.DisplayMessageNotification(string.Format(LocaleUtils.CONNECTED_TO_SERVER.Localized(), FikaBackendUtils.RemoteEndPoint.Port),
            ENotificationDurationType.Default, ENotificationIconType.Friend);

        var ownProfile = FikaGlobals.GetLiteProfile(FikaBackendUtils.IsScav);
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
                var remoteTime = reader.GetDouble();
                var localTime = NetworkTimeSync.NetworkTime;
                var remaining = reader.GetRemainingBytesSpan();
                foreach (ref readonly var snapshot in MemoryMarshal.Cast<byte, PlayerStateData>(remaining))
                {
                    if (_coopHandler.Players.TryGetValue(snapshot.NetId, out var player))
                    {
                        var header = new PlayerStateSnapshot(in snapshot, remoteTime, localTime);
                        player.Snapshotter.AddSnapshot(in header);
                    }
                }
                break;
            case EPacketType.BTR:
                ref var data = ref Unsafe.As<byte, ShapshotBTRMessage>(ref MemoryMarshal.GetReference(reader.GetRemainingBytesSpan()));
                if (BtrController.Instance.BtrView != null)
                {
                    BtrController.Instance.BtrView.SyncViewFromServer(ref data);
                }
                break;
            case EPacketType.VOIP:
                if (VOIPClient != null)
                {
                    VOIPClient.NetworkReceivedPacket(reader.GetRemainingBytesSegment());
                }
                break;
            case EPacketType.Generic:
                HandleGenericPacket(reader);
                break;
            case EPacketType.Player:
                HandlePlayerPacket(reader);
                break;
            case EPacketType.Firearm:
                HandleFirearmPacket(reader);
                break;
        }
    }

    private void HandleFirearmPacket(NetPacketReader reader)
    {
        var netId = reader.GetInt();
        if (!_coopHandler.Players.TryGetValue(netId, out var player))
        {
            var type = reader.GetEnum<EFirearmPacketType>();
            _logger.LogWarning($"FikaClient::HandleFirearmPacket: Received FirearmPacket, but there was no player with id {netId} for packet type {type}");
            return;
        }

        switch (reader.GetEnum<EFirearmPacketType>())
        {
            case EFirearmPacketType.ShotInfo:
                new ShotInfoPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.DryShot:
                new DryShotPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.Misfire:
                new MisfirePacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ChangeFireMode:
                new ChangeFireModePacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ToggleAim:
                new ToggleAimPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ExamineWeapon:
                new ExamineWeaponPacket().Execute(player);
                break;
            case EFirearmPacketType.CheckAmmo:
                new CheckAmmoPacket().Execute(player);
                break;
            case EFirearmPacketType.CheckChamber:
                new CheckChamberPacket().Execute(player);
                break;
            case EFirearmPacketType.CheckFireMode:
                new CheckFireModePacket().Execute(player);
                break;
            case EFirearmPacketType.ToggleLightStates:
                new LightStatesPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ToggleScopeStates:
                new ScopeStatesPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ToggleLauncher:
                new ToggleLauncherPacket().Execute(player);
                break;
            case EFirearmPacketType.ToggleInventory:
                new ToggleInventoryPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.Loot:
                new FirearmLootPacket().Execute(player);
                break;
            case EFirearmPacketType.ReloadMag:
                new ReloadMagPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.QuickReloadMag:
                new QuickReloadMagPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ReloadWithAmmo:
                new ReloadWithAmmoPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.CylinderMag:
                new CylinderMagPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ReloadLauncher:
                new ReloadLauncherPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ReloadBarrels:
                new ReloadBarrelsPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.Grenade:
                new GrenadePacket(reader).Execute(player);
                break;
            case EFirearmPacketType.CancelGrenade:
                new CancelGrenadePacket().Execute(player);
                break;
            case EFirearmPacketType.CompassChange:
                new CompassChangePacket(reader).Execute(player);
                break;
            case EFirearmPacketType.Knife:
                new KnifePacket(reader).Execute(player);
                break;
            case EFirearmPacketType.FlareShot:
                new FlareShotPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.RocketShot:
                new RocketShotPacket(reader).Execute(player);
                break;
            case EFirearmPacketType.ReloadBoltAction:
                new ReloadBoltActionPacket().Execute(player);
                break;
            case EFirearmPacketType.RollCylinder:
                new RollCylinderPacket().Execute(player);
                break;
            case EFirearmPacketType.UnderbarrelSightingRangeUp:
                new UnderbarrelSightingRangeUpPacket().Execute(player);
                break;
            case EFirearmPacketType.UnderbarrelSightingRangeDown:
                new UnderbarrelSightingRangeDownPacket().Execute(player);
                break;
            case EFirearmPacketType.ToggleBipod:
                new ToggleBipodPacket().Execute(player);
                break;
            case EFirearmPacketType.LeftStanceChange:
                new LeftStanceChangePacket(reader).Execute(player);
                break;
        }
    }

    private void HandlePlayerPacket(NetPacketReader reader)
    {
        var netId = reader.GetInt();
        if (!_coopHandler.Players.TryGetValue(netId, out var player))
        {
            var type = reader.GetEnum<EPlayerPacketType>();
            _logger.LogWarning($"FikaClient::HandlePlayerPacket: Received PlayerPacket, but there was no player with id {netId} for packet type {type}");
            return;
        }

        switch (reader.GetEnum<EPlayerPacketType>())
        {
            case EPlayerPacketType.Phrase:
                new PhrasePacket(reader).Execute(player);
                break;
            case EPlayerPacketType.WorldInteraction:
                new WorldInteractionPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.ContainerInteraction:
                new ContainerInteractionPacket(reader).Execute();
                break;
            case EPlayerPacketType.Proceed:
                new ProceedPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.HeadLights:
                new HeadLightsPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.InventoryChanged:
                reader.GetUnmanaged<InventoryChangedPacket>().Execute(player);
                break;
            case EPlayerPacketType.Drop:
                reader.GetUnmanaged<DropPacket>().Execute(player);
                break;
            case EPlayerPacketType.MuffledState:
                reader.GetUnmanaged<MuffledStatePacket>().Execute(player);
                break;
            case EPlayerPacketType.Stationary:
                new StationaryPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.Vault:
                reader.GetUnmanaged<VaultPacket>().Execute(player);
                break;
            case EPlayerPacketType.Interaction:
                new InteractionPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.Mounting:
                new MountingPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.Damage:
                new DamagePacket(reader).Execute(player);
                break;
            case EPlayerPacketType.ArmorDamage:
                new ArmorDamagePacket(reader).Execute(player);
                break;
            case EPlayerPacketType.HealthSync:
                new HealthSyncPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.UsableItem:
                new UsableItemPacket(reader).Execute(player);
                break;
            case EPlayerPacketType.DownedSync:
                reader.GetUnmanaged<DownedSyncPacket>().Execute(player);
                break;
            case EPlayerPacketType.RevivedPlayer:
                new RevivedPlayerPacket().Execute(player);
                break;
            case EPlayerPacketType.RevivingPlayer:
                new RevivingPlayerPacket(reader).Execute(player);
                break;
        }
    }

    private void HandleGenericPacket(NetPacketReader reader)
    {
        switch (reader.GetEnum<EGenericPacketType>())
        {
            case EGenericPacketType.ClientExtract:
                reader.GetUnmanaged<ClientExtractPacket>().Execute();
                break;
            case EGenericPacketType.ClientConnected:
                new ClientConnectedPacket(reader).Execute();
                break;
            case EGenericPacketType.ClientDisconnected:
                new ClientDisconnectedPacket(reader).Execute();
                break;
            case EGenericPacketType.ExfilCountdown:
                new ExfilCountdownPacket(reader).Execute();
                break;
            case EGenericPacketType.UpdateBackendData:
                reader.GetUnmanaged<UpdateBackendDataPacket>().Execute();
                break;
            case EGenericPacketType.SecretExfilFound:
                new SecretExfilFoundPacket(reader).Execute();
                break;
            case EGenericPacketType.BorderZoneEvent:
                new BorderZoneEventPacket(reader).Execute();
                break;
            case EGenericPacketType.MineEvent:
                reader.GetUnmanaged<MineEventPacket>().Execute();
                break;
            case EGenericPacketType.DisarmTripwire:
                new DisarmTripwirePacket(reader).Execute();
                break;
            case EGenericPacketType.SpawnBTR:
                new SpawnBTRPacket(reader).Execute();
                break;
            case EGenericPacketType.CharacterSync:
                new CharacterSyncPacket(reader).Execute();
                break;
            case EGenericPacketType.InventoryOperation:
                {
                    var inventory = new InventoryPacket(reader);
                    if (_coopHandler.Players.TryGetValue(inventory.NetId, out var player))
                    {
                        HandleInventoryPacket(in inventory, player);
                    }
                    break;
                }
            case EGenericPacketType.OperationCallback:
                {
                    var operationCallback = new OperationCallbackPacket(reader);
                    if (_coopHandler.Players.TryGetValue(operationCallback.NetId, out var player) && player.IsYourPlayer)
                    {
                        player.HandleCallbackFromServer(in operationCallback);
                    }
                    break;
                }
            case EGenericPacketType.Ping:
                new PingPacket(reader).Execute();
                break;
            case EGenericPacketType.SendCharacter:
                new SendCharacterPacket(reader).Execute();
                break;
            case EGenericPacketType.SyncableItem:
                new SyncableItemPacket(reader).Execute();
                break;
            case EGenericPacketType.SpawnAI:
                reader.GetUnmanaged<SpawnAIPacket>().Execute();
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.BasicMessage && _netClient.ConnectedPeersCount == 0 && reader.GetInt() == 1)
        {
            _logger.LogInfo("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
            _netClient.Connect(remoteEndPoint, NetDataWriter.FromString("fika.core").AsReadOnlySpan());
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
            NotificationManager.DisplayWarningNotification(LocaleUtils.LOST_CONNECTION.Localized());
            MyPlayer.PacketSender.DestroyThis();
            Destroy(this);
            Singleton<FikaClient>.Release(this);
        }

        if (disconnectInfo.Reason is DisconnectReason.ConnectionRejected)
        {
            var reason = disconnectInfo.AdditionalData.GetString();
            if (!string.IsNullOrEmpty(reason))
            {
                NotificationManager.DisplayWarningNotification(reason);
                return;
            }

            _logger.LogError("OnPeerDisconnected: Rejected connection but no reason");
        }

        FikaEventDispatcher.DispatchEvent(new PeerDisconnectedEvent(peer, this));
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

    public void UnregisterPacket<T>() where T : INetSerializable
    {
        if (!_packetProcessor.RemoveSubscription<T>())
        {
            _logger.LogError($"Failed to remove {typeof(T).Name} from the packet subscription list");
        }
    }

    public void UnregisterNetReusable<T>() where T : INetReusable
    {
        if (!_packetProcessor.RemoveSubscription<T>())
        {
            _logger.LogError($"Failed to remove {typeof(T).Name} from the packet subscription list");
        }
    }

    public void PrintStatistics()
    {
        _logger.LogInfo("..:: Fika Client Session Statistics ::..");
        _logger.LogInfo($"Sent packets: {_netClient.Statistics.PacketsSent}");
        _logger.LogInfo($"Sent data: {FikaGlobals.FormatFileSize(_netClient.Statistics.BytesSent)}");
        _logger.LogInfo($"Received packets: {_netClient.Statistics.PacketsReceived}");
        _logger.LogInfo($"Received data: {FikaGlobals.FormatFileSize(_netClient.Statistics.BytesReceived)}");
        _logger.LogInfo($"Packet loss: {_netClient.Statistics.PacketLossPercent}%");
        _logger.LogInfo($"Time in raid: {_startTime - DateTime.Now:hh\\h\\ mm\\m\\ ss\\s}");
    }

    public NetPeer GetPeerById(int id)
    {
        return (NetPeer)_netClient.GetPeerById(id);
    }

    private void HandleInventoryPacket(in InventoryPacket packet, FikaPlayer player)
    {
        if (packet.Descriptor == null)
        {
            _logger.LogError("HandleInventoryPacket::Descriptor was null!");
            return;
        }

        var controller = player.InventoryController;
        if (controller != null)
        {
            try
            {
                if (controller is IOperationHandler networkController)
                {
                    var result = networkController.CreateOperationFromDescriptor(packet.Descriptor);
                    if (!result.Succeeded)
                    {
                        _logger.LogError($"HandleInventoryPacket::Unable to process descriptor from netId {packet.NetId}, error: {result.Error}");
                        return;
                    }

                    _inventoryOperations.Enqueue(result.Value);
                    HandleInventoryOperations();
                }
                else
                {
                    _logger.LogError($"Player {player.Profile.GetCorrectedNickname()} is not allowed to process inventory operations. Operation: {packet.Descriptor.GetType().Name}");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"HandleInventoryPacket::Exception thrown: {exception}");
            }
        }
        else
        {
            _logger.LogError("HandleInventoryPacket: inventory was null!");
        }
    }

    private void HandleResult(IResult result)
    {
        if (result.Failed)
        {
            _logger.LogError($"Error in operation: {result.Error}");
        }
    }
}
