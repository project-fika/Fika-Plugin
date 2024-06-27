// © 2024 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.Networking.NatPunch;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Communication;
using Fika.Core.Networking.Packets.GameWorld;
using Fika.Core.Networking.Packets.Player;
using LiteNetLib;
using LiteNetLib.Utils;
using Open.Nat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Coop.Airdrops;
using Fika.Core.Coop.Airdrops;
using SPT.Custom.Airdrops;
using HarmonyLib;

namespace Fika.Core.Networking
{
    public class FikaServer : MonoBehaviour, INetEventListener, INetLogger
    {
        private NetManager _netServer;
        public NetPacketProcessor packetProcessor = new();
        private readonly NetDataWriter _dataWriter = new();
        public CoopPlayer MyPlayer;
        public Dictionary<int, CoopPlayer> Players => coopHandler.Players;
        public List<string> PlayersMissing = [];
        public string MyExternalIP { get; private set; } = NetUtils.GetLocalIp(LocalAddrType.IPv4);
        private int Port => FikaPlugin.UDPPort.Value;
        private CoopHandler coopHandler;
        public int ReadyClients = 0;
        public NetManager NetServer
        {
            get
            {
                return _netServer;
            }
        }
        public DateTime timeSinceLastPeerDisconnected = DateTime.Now.AddDays(1);
        public bool hasHadPeer = false;
        private readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Server");
        private int _currentNetId;
        public bool Started
        {
            get
            {
                if (_netServer == null)
                {
                    return false;
                }
                return _netServer.IsRunning;
            }
        }
        private FikaChat fikaChat;
        public FikaNatPunchServer FikaNatPunchServer;
        private CancellationTokenSource StunQueryRoutineCts;

        public async Task Init()
        {
            // Start at 1 to avoid having 0 and making us think it's working when it's not
            _currentNetId = 1;

            packetProcessor.SubscribeNetSerializable<PlayerStatePacket, NetPeer>(OnPlayerStatePacketReceived);
            packetProcessor.SubscribeNetSerializable<GameTimerPacket, NetPeer>(OnGameTimerPacketReceived);
            packetProcessor.SubscribeNetSerializable<WeaponPacket, NetPeer>(OnFirearmPacketReceived);
            packetProcessor.SubscribeNetSerializable<DamagePacket, NetPeer>(OnDamagePacketReceived);
            packetProcessor.SubscribeNetSerializable<ArmorDamagePacket, NetPeer>(OnArmorDamagePacketReceived);
            packetProcessor.SubscribeNetSerializable<InventoryPacket, NetPeer>(OnInventoryPacketReceived);
            packetProcessor.SubscribeNetSerializable<CommonPlayerPacket, NetPeer>(OnCommonPlayerPacketReceived);
            packetProcessor.SubscribeNetSerializable<AllCharacterRequestPacket, NetPeer>(OnAllCharacterRequestPacketReceived);
            packetProcessor.SubscribeNetSerializable<InformationPacket, NetPeer>(OnInformationPacketReceived);
            packetProcessor.SubscribeNetSerializable<HealthSyncPacket, NetPeer>(OnHealthSyncPacketReceived);
            packetProcessor.SubscribeNetSerializable<GenericPacket, NetPeer>(OnGenericPacketReceived);
            packetProcessor.SubscribeNetSerializable<ExfiltrationPacket, NetPeer>(OnExfiltrationPacketReceived);
            packetProcessor.SubscribeNetSerializable<WeatherPacket, NetPeer>(OnWeatherPacketReceived);
            packetProcessor.SubscribeNetSerializable<BTRInteractionPacket, NetPeer>(OnBTRInteractionPacketReceived);
            packetProcessor.SubscribeNetSerializable<BTRServicePacket, NetPeer>(OnBTRServicePacketReceived);
            packetProcessor.SubscribeNetSerializable<MinePacket, NetPeer>(OnMinePacketReceived);
            packetProcessor.SubscribeNetSerializable<BorderZonePacket, NetPeer>(OnBorderZonePacketReceived);
            packetProcessor.SubscribeNetSerializable<SendCharacterPacket, NetPeer>(OnSendCharacterPacketReceived);
            packetProcessor.SubscribeNetSerializable<ReconnectRequestPacket, NetPeer>(OnReconnectRequestPacketReceived);
            packetProcessor.SubscribeNetSerializable<ConditionChangePacket, NetPeer>(OnConditionChangedPacketReceived);
            packetProcessor.SubscribeNetSerializable<TextMessagePacket, NetPeer>(OnTextMessagePacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestConditionPacket, NetPeer>(OnQuestConditionPacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestItemPacket, NetPeer>(OnQuestItemPacketReceived);

            _netServer = new NetManager(this)
            {
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true,
                UpdateTime = 15,
                AutoRecycle = true,
                IPv6Enabled = false,
                DisconnectTimeout = FikaPlugin.ConnectionTimeout.Value * 1000,
                UseNativeSockets = FikaPlugin.NativeSockets.Value,
                EnableStatistics = true
            };

            if (FikaPlugin.UseUPnP.Value && !FikaPlugin.UseNatPunching.Value)
            {
                bool upnpFailed = false;

                try
                {
                    NatDiscoverer discoverer = new();
                    CancellationTokenSource cts = new(10000);
                    NatDevice device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                    IPAddress extIp = await device.GetExternalIPAsync();
                    MyExternalIP = extIp.MapToIPv4().ToString();

                    await device.CreatePortMapAsync(new Mapping(Protocol.Udp, Port, Port, 300, "Fika UDP"));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error when attempting to map UPnP. Make sure the selected port is not already open! Error message: {ex.Message}");
                    upnpFailed = true;
                }

                if (upnpFailed)
                {
                    Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", "UPnP mapping failed. Make sure the selected port is not already open!");
                }
            }
            else if (FikaPlugin.ForceIP.Value != "")
            {
                MyExternalIP = FikaPlugin.ForceIP.Value;
            }
            else
            {
                try
                {
                    HttpClient client = new();
                    string ipAdress = await client.GetStringAsync("https://ipv4.icanhazip.com/");
                    MyExternalIP = ipAdress.Replace("\n", "");
                    client.Dispose();
                }
                catch (Exception)
                {
                    Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", "Error when trying to receive IP automatically.");
                }
            }

            if (FikaPlugin.UseNatPunching.Value)
            {
                FikaNatPunchServer = new FikaNatPunchServer(_netServer);
                FikaNatPunchServer.Connect();

                if (FikaNatPunchServer.Connected)
                {
                    StunQueryRoutineCts = new CancellationTokenSource();
                    Task stunQueryRoutine = Task.Run(() => NatPunchUtils.StunQueryRoutine(_netServer, FikaNatPunchServer, StunQueryRoutineCts.Token));
                }
                else
                {
                    logger.LogError("Unable to connect to FikaNatPunchRelayService.");
                }
            }
            else
            {
                if (FikaPlugin.ForceBindIP.Value != "Disabled")
                {
                    _netServer.Start(FikaPlugin.ForceBindIP.Value, "", Port);
                }
                else
                {
                    _netServer.Start(Port);
                }
            }

            logger.LogInfo("Started Fika Server");

            string serverStartedMessage;

            if (FikaPlugin.UseNatPunching.Value)
            {
                serverStartedMessage = "Server started using Nat Punching.";
            }
            else
            {
                serverStartedMessage = $"Server started on port {_netServer.LocalPort}.";
            }

            NotificationManagerClass.DisplayMessageNotification(serverStartedMessage,
                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);

            string[] Ips = [];

            foreach (string ip in FikaPlugin.Instance.LocalIPs)
            {
                if (ip.StartsWith("192.168")) // need to add more cases here later, for now only check normal range...
                {
                    Ips = [MyExternalIP, ip];
                }
            }

            if (Ips.Length < 1)
            {
                Ips = [MyExternalIP, ""];
                NotificationManagerClass.DisplayMessageNotification("Could not find a valid local IP!",
                iconType: EFT.Communications.ENotificationIconType.Alert);
            }

            SetHostRequest body = new(Ips, Port, FikaPlugin.UseNatPunching.Value);
            FikaRequestHandler.UpdateSetHost(body);

            FikaEventDispatcher.DispatchEvent(new FikaServerCreatedEvent(this));
        }

        private void OnQuestItemPacketReceived(QuestItemPacket packet, NetPeer peer)
        {
            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);

            if (MyPlayer.HealthController.IsAlive)
            {
                if (MyPlayer.GClass3227_0 is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestItemPacket(ref packet);
                }
            }
        }

        private void OnQuestConditionPacketReceived(QuestConditionPacket packet, NetPeer peer)
        {
            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);

            if (MyPlayer.HealthController.IsAlive)
            {
                if (MyPlayer.GClass3227_0 is CoopClientSharedQuestController sharedQuestController)
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

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);
        }

        public int PopNetId()
        {
            int netId = _currentNetId;
            _currentNetId++;

            return netId;
        }

        public void SetupGameVariables(CoopPlayer coopPlayer)
        {
            coopHandler = CoopHandler.CoopHandlerParent.GetComponent<CoopHandler>();
            MyPlayer = coopPlayer;
            fikaChat = gameObject.AddComponent<FikaChat>();
        }

        private void OnSendCharacterPacketReceived(SendCharacterPacket packet, NetPeer peer)
        {
            int netId = PopNetId();
            packet.netId = netId;
            if (packet.PlayerInfo.Profile.ProfileId != MyPlayer.ProfileId)
            {
                coopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.netId, packet.IsAlive, packet.IsAI);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);

            AssignNetIdPacket assignNetIdPacket = new()
            {
                NetId = netId
            };

            _dataWriter.Reset();
            packetProcessor.WriteNetSerializable(_dataWriter, ref assignNetIdPacket);
            peer.Send(_dataWriter, DeliveryMethod.ReliableUnordered);
        }

        private void OnBorderZonePacketReceived(BorderZonePacket packet, NetPeer peer)
        {
            // This shouldn't happen
        }

        private void OnMinePacketReceived(MinePacket packet, NetPeer peer)
        {
            if (Singleton<GameWorld>.Instance.MineManager != null)
            {
                NetworkGame<EftGamePlayerOwner>.Class1407 mineSeeker = new()
                {
                    minePosition = packet.MinePositon
                };
                MineDirectional mineDirectional = Singleton<GameWorld>.Instance.MineManager.Mines.FirstOrDefault(new Func<MineDirectional, bool>(mineSeeker.method_0));
                if (mineDirectional == null)
                {
                    return;
                }
                mineDirectional.Explosion();
            }
        }

        private void OnBTRServicePacketReceived(BTRServicePacket packet, NetPeer peer)
        {
            if (coopHandler.serverBTR != null)
            {
                coopHandler.serverBTR.NetworkBtrTraderServicePurchased(packet);
            }
        }

        private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet, NetPeer peer)
        {
            if (coopHandler.serverBTR != null)
            {
                if (Players.TryGetValue(packet.NetId, out CoopPlayer player))
                {
                    if (coopHandler.serverBTR.CanPlayerEnter(player))
                    {
                        coopHandler.serverBTR.HostObservedInteraction(player, packet.InteractPacket);

                        _dataWriter.Reset();
                        SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        BTRInteractionPacket newPacket = new(packet.NetId)
                        {
                            HasInteractPacket = false
                        };

                        _dataWriter.Reset();
                        SendDataToAll(_dataWriter, ref newPacket, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        private void OnWeatherPacketReceived(WeatherPacket packet, NetPeer peer)
        {
            if (packet.IsRequest)
            {
                if (FikaBackendUtils.Nodes != null)
                {
                    WeatherPacket weatherPacket2 = new()
                    {
                        IsRequest = false,
                        HasData = true,
                        Amount = FikaBackendUtils.Nodes.Length,
                        WeatherClasses = FikaBackendUtils.Nodes
                    };
                    _dataWriter.Reset();
                    SendDataToPeer(peer, _dataWriter, ref weatherPacket2, DeliveryMethod.ReliableOrdered);
                };
            }
        }

        private void OnExfiltrationPacketReceived(ExfiltrationPacket packet, NetPeer peer)
        {
            if (packet.IsRequest)
            {
                if (ExfiltrationControllerClass.Instance != null)
                {
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    if (exfilController.ExfiltrationPoints == null)
                        return;

                    ExfiltrationPacket exfilPacket = new(false)
                    {
                        ExfiltrationAmount = exfilController.ExfiltrationPoints.Length,
                        ExfiltrationPoints = []
                    };

                    foreach (ExfiltrationPoint exfilPoint in exfilController.ExfiltrationPoints)
                    {
                        exfilPacket.ExfiltrationPoints.Add(exfilPoint.Settings.Name, exfilPoint.Status);
                    }

                    if (MyPlayer.Side == EPlayerSide.Savage && exfilController.ScavExfiltrationPoints != null)
                    {
                        exfilPacket.HasScavExfils = true;
                        exfilPacket.ScavExfiltrationAmount = exfilController.ScavExfiltrationPoints.Length;
                        exfilPacket.ScavExfiltrationPoints = [];

                        foreach (ScavExfiltrationPoint scavExfilPoint in exfilController.ScavExfiltrationPoints)
                        {
                            exfilPacket.ScavExfiltrationPoints.Add(scavExfilPoint.Settings.Name, scavExfilPoint.Status);
                        }
                    }

                    _dataWriter.Reset();
                    SendDataToPeer(peer, _dataWriter, ref exfilPacket, DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    logger.LogError($"ExfiltrationPacketPacketReceived: ExfiltrationController was null");
                }
            }
        }

        private void OnGenericPacketReceived(GenericPacket packet, NetPeer peer)
        {
            if (packet.PacketType == EPackageType.ClientExtract)
            {
                if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
                {
                    coopHandler.Players.Remove(packet.NetId);
                    if (!coopHandler.ExtractedPlayers.Contains(packet.NetId))
                    {
                        coopHandler.ExtractedPlayers.Add(packet.NetId);
                        CoopGame coopGame = coopHandler.LocalGameInstance;
                        coopGame.ExtractedPlayers.Add(packet.NetId);
                        coopGame.ClearHostAI(playerToApply);

                        if (FikaPlugin.ShowNotifications.Value)
                        {
                            string nickname = !string.IsNullOrEmpty(playerToApply.Profile.Info.MainProfileNickname) ? playerToApply.Profile.Info.MainProfileNickname : playerToApply.Profile.Nickname;
                            NotificationManagerClass.DisplayMessageNotification($"Group member '{nickname}' has extracted.",
                                            EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);
                        }
                    }

                    playerToApply.Dispose();
                    AssetPoolObject.ReturnToPool(playerToApply.gameObject, true);
                }
            }
            else if (packet.PacketType == EPackageType.Ping && FikaPlugin.UsePingSystem.Value)
            {
                if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
                {
                    playerToApply.ReceivePing(packet.PingLocation, packet.PingType, packet.PingColor, packet.Nickname);
                }
            }
            else if (packet.PacketType == EPackageType.LoadBot)
            {
                CoopGame coopGame = coopHandler.LocalGameInstance;
                coopGame.IncreaseLoadedPlayers(packet.BotNetId);

                return;
            }
            else if (packet.PacketType == EPackageType.ExfilCountdown)
            {
                if (ExfiltrationControllerClass.Instance != null)
                {
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    ExfiltrationPoint exfilPoint = exfilController.ExfiltrationPoints.FirstOrDefault(x => x.Settings.Name == packet.ExfilName);
                    if (exfilPoint != null)
                    {
                        CoopGame game = coopHandler.LocalGameInstance;
                        exfilPoint.ExfiltrationStartTime = game != null ? game.PastTime : packet.ExfilStartTime;

                        if (exfilPoint.Status != EExfiltrationStatus.Countdown)
                        {
                            exfilPoint.Status = EExfiltrationStatus.Countdown;
                        }
                    }
                }
            }
            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnHealthSyncPacketReceived(HealthSyncPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.HealthSyncPackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnInformationPacketReceived(InformationPacket packet, NetPeer peer)
        {
            ReadyClients += packet.ReadyPlayers;

            InformationPacket respondPackage = new(false)
            {
                NumberOfPlayers = _netServer.ConnectedPeersCount,
                ReadyPlayers = ReadyClients,
            };

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref respondPackage, DeliveryMethod.ReliableOrdered);
        }

        private void OnAllCharacterRequestPacketReceived(AllCharacterRequestPacket packet, NetPeer peer)
        {
            if (packet.IsRequest)
            {
                foreach (CoopPlayer player in coopHandler.Players.Values)
                {
                    if (player.ProfileId == packet.ProfileId)
                    {
                        continue;
                    }

                    if (packet.Characters.Contains(player.ProfileId))
                    {
                        continue;
                    }

                    AllCharacterRequestPacket requestPacket = new(player.ProfileId)
                    {
                        IsRequest = false,
                        PlayerInfo = new()
                        {
                            Profile = player.Profile
                        },
                        IsAlive = player.HealthController.IsAlive,
                        IsAI = player is CoopBot,
                        Position = player.Transform.position,
                        NetId = player.NetId
                    };
                    _dataWriter.Reset();
                    SendDataToPeer(peer, _dataWriter, ref requestPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            if (!Players.ContainsKey(packet.NetId) && !PlayersMissing.Contains(packet.ProfileId) && !coopHandler.ExtractedPlayers.Contains(packet.NetId))
            {
                PlayersMissing.Add(packet.ProfileId);
                logger.LogInfo($"Requesting missing player from server.");
                AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId);
                _dataWriter.Reset();
                SendDataToPeer(peer, _dataWriter, ref requestPacket, DeliveryMethod.ReliableOrdered);
            }
            if (!packet.IsRequest && PlayersMissing.Contains(packet.ProfileId))
            {
                logger.LogInfo($"Received CharacterRequest from client: ProfileID: {packet.PlayerInfo.Profile.ProfileId}, Nickname: {packet.PlayerInfo.Profile.Nickname}");
                if (packet.ProfileId != MyPlayer.ProfileId)
                {
                    coopHandler.QueueProfile(packet.PlayerInfo.Profile, new Vector3(packet.Position.x, packet.Position.y + 0.5f, packet.Position.y), packet.NetId, packet.IsAlive);
                    PlayersMissing.Remove(packet.ProfileId);
                }
            }
        }

        private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.CommonPlayerPackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnInventoryPacketReceived(InventoryPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                using MemoryStream memoryStream = new(packet.ItemControllerExecutePacket.OperationBytes);
                using BinaryReader binaryReader = new(memoryStream);
                try
                {
                    GStruct411 result = playerToApply.ToInventoryOperation(binaryReader.ReadPolymorph<GClass1543>());

                    InventoryOperationHandler opHandler = new()
                    {
                        opResult = result,
                        operationId = packet.ItemControllerExecutePacket.CallbackId,
                        netId = playerToApply.NetId,
                        peer = peer,
                        server = this
                    };

                    OperationCallbackPacket operationCallbackPacket = new(playerToApply.NetId, packet.ItemControllerExecutePacket.CallbackId, EOperationStatus.Started);
                    SendDataToPeer(peer, new(), ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

                    opHandler.opResult.Value.vmethod_0(new Callback(opHandler.HandleResult), false);

                    // TODO: Hacky workaround to fix errors due to each client generating new IDs. Might need to find a more 'elegant' solution later.
                    // Unknown what problems this might cause so far.
                    if (result.Value is GClass2878 unloadOperation)
                    {
                        if (unloadOperation.InternalOperation is SplitOperationClass internalSplitOperation)
                        {
                            Item item = internalSplitOperation.To.Item;
                            if (item != null)
                            {
                                if (item.Id != internalSplitOperation.CloneId && item.TemplateId == internalSplitOperation.Item.TemplateId)
                                {
                                    item.Id = internalSplitOperation.CloneId;
                                }
                                else
                                {
                                    FikaPlugin.Instance.FikaLogger.LogWarning($"Matching failed: ItemID: {item.Id}, SplitOperationItemID: {internalSplitOperation.To.Item.Id}");
                                }
                            }
                            else
                            {
                                FikaPlugin.Instance.FikaLogger.LogError("Split: Item was null");
                            }
                        }
                    }

                    // TODO: Same as above.
                    if (result.Value is SplitOperationClass splitOperation)
                    {
                        Item item = splitOperation.To.Item;
                        if (item != null)
                        {
                            if (item.Id != splitOperation.CloneId && item.TemplateId == splitOperation.Item.TemplateId)
                            {
                                item.Id = splitOperation.CloneId;
                            }
                            else
                            {
                                FikaPlugin.Instance.FikaLogger.LogWarning($"Matching failed: ItemID: {item.Id}, SplitOperationItemID: {splitOperation.To.Item.Id}");
                            }
                        }
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogError("Split: Item was null");
                        }
                    }

                    /*// Fix for folding not replicating
                    if (result.Value is GClass2858 foldOperation)
                    {
                        if (playerToApply.HandsController is CoopObservedFirearmController observedFirearmController)
                        {
                            if (observedFirearmController.Weapon != null && observedFirearmController.Weapon.Foldable != null)
                            {
                                observedFirearmController.InitiateOperation<FirearmController.Class1020>().Start(foldOperation, null);
                            }
                        }
                    }*/
                }
                catch (Exception exception)
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"ItemControllerExecutePacket::Exception thrown: {exception}");
                    OperationCallbackPacket callbackPacket = new(playerToApply.NetId, packet.ItemControllerExecutePacket.CallbackId, EOperationStatus.Failed);
                    SendDataToAll(new(), ref callbackPacket, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void OnDamagePacketReceived(DamagePacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.DamagePackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnArmorDamagePacketReceived(ArmorDamagePacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.ArmorDamagePackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnFirearmPacketReceived(WeaponPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.FirearmPackets?.Enqueue(packet);
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnGameTimerPacketReceived(GameTimerPacket packet, NetPeer peer)
        {
            if (!packet.IsRequest)
                return;

            CoopGame game = coopHandler.LocalGameInstance;
            if (game != null)
            {
                GameTimerPacket gameTimerPacket = new(false, (game.GameTimer.SessionTime - game.GameTimer.PastTime).Value.Ticks, game.GameTimer.StartDateTime.Value.Ticks);
                _dataWriter.Reset();
                SendDataToPeer(peer, _dataWriter, ref gameTimerPacket, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                logger.LogError("OnGameTimerPacketReceived: Game was null!");
            }
        }

        private void OnPlayerStatePacketReceived(PlayerStatePacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.NewState = packet;
            }

            _dataWriter.Reset();
            SendDataToAll(_dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        protected void Update()
        {
            _netServer?.PollEvents();
        }

        protected void OnDestroy()
        {
            _netServer?.Stop();

            if (fikaChat != null)
            {
                Destroy(fikaChat);
            }

            FikaEventDispatcher.DispatchEvent(new FikaServerDestroyedEvent(this));
        }

        public void SendDataToAll<T>(NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod, NetPeer peerToExclude = null) where T : INetSerializable
        {

            if (peerToExclude != null)
            {
                if (NetServer.ConnectedPeersCount > 1)
                {
                    packetProcessor.WriteNetSerializable(writer, ref packet);
                    _netServer.SendToAll(writer, deliveryMethod, peerToExclude);
                }
            }
            else
            {
                packetProcessor.WriteNetSerializable(writer, ref packet);
                _netServer.SendToAll(writer, deliveryMethod);
            }
        }

        public void SendDataToPeer<T>(NetPeer peer, NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            packetProcessor.WriteNetSerializable(writer, ref packet);
            peer.Send(writer, deliveryMethod);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            NotificationManagerClass.DisplayMessageNotification($"Peer connected to server on port {peer.Port}.", iconType: EFT.Communications.ENotificationIconType.Friend);
            logger.LogInfo($"Connection established with {peer.Address}:{peer.Port}, id: {peer.Id}.");

            hasHadPeer = true;
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            logger.LogError("[SERVER] error " + socketErrorCode);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.Broadcast)
            {
                logger.LogInfo("[SERVER] Received discovery request. Send discovery response");
                NetDataWriter resp = new();
                resp.Put(1);
                _netServer.SendUnconnectedMessage(resp, remoteEndPoint);
            }
            else
            {
                if (reader.TryGetString(out string data))
                {
                    NetDataWriter resp;

                    switch (data)
                    {
                        case "fika.hello":
                            resp = new();
                            resp.Put(data);
                            _netServer.SendUnconnectedMessage(resp, remoteEndPoint);
                            logger.LogInfo("PingingRequest: Correct ping query, sending response");
                            break;

                        case "fika.keepalive":
                            resp = new();
                            resp.Put(data);
                            _netServer.SendUnconnectedMessage(resp, remoteEndPoint);

                            if (!StunQueryRoutineCts.IsCancellationRequested)
                            {
                                StunQueryRoutineCts.Cancel();
                            }
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
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("fika.core");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger.LogInfo("Peer disconnected " + peer.Port + ", info: " + disconnectInfo.Reason);
            NotificationManagerClass.DisplayMessageNotification("Peer disconnected " + peer.Port + ", info: " + disconnectInfo.Reason, iconType: EFT.Communications.ENotificationIconType.Alert);
            if (_netServer.ConnectedPeersCount == 0)
            {
                timeSinceLastPeerDisconnected = DateTime.Now;
            }
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args)
        {
            Debug.LogFormat(str, args);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            packetProcessor.ReadAllPackets(reader, peer);
        }

        private class InventoryOperationHandler
        {
            public GStruct411 opResult;
            public uint operationId;
            public int netId;
            public NetPeer peer;
            public FikaServer server;

            internal void HandleResult(IResult result)
            {
                NetDataWriter writer = new();
                OperationCallbackPacket operationCallbackPacket;

                if (!result.Succeed)
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"Error in operation: {result.Error ?? "An unknown error has occured"}");
                    operationCallbackPacket = new(netId, operationId, EOperationStatus.Failed, result.Error ?? "An unknown error has occured");
                    writer.Reset();
                    server.SendDataToPeer(peer, writer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

                    return;
                }

                InventoryPacket packet = new(netId)
                {
                    HasItemControllerExecutePacket = true
                };

                using MemoryStream memoryStream = new();
                using BinaryWriter binaryWriter = new(memoryStream);
                binaryWriter.WritePolymorph(FromObjectAbstractClass.FromInventoryOperation(opResult.Value, false));
                byte[] opBytes = memoryStream.ToArray();
                packet.ItemControllerExecutePacket = new()
                {
                    CallbackId = operationId,
                    OperationBytes = opBytes
                };

                server.SendDataToAll(writer, ref packet, DeliveryMethod.ReliableOrdered, peer);

                operationCallbackPacket = new(netId, operationId, EOperationStatus.Finished);
                writer.Reset();
                server.SendDataToPeer(peer, writer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnConditionChangedPacketReceived(ConditionChangePacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer player))
            {
                foreach (KeyValuePair<string, GClass3242> taskConditionCounter in player.Profile.TaskConditionCounters)
                {
                    if (taskConditionCounter.Key == packet.ConditionId)
                    {
                        taskConditionCounter.Value.Value = (int)packet.ConditionValue;
                    }
                }
            }
        }

        private void OnReconnectRequestPacketReceived(ReconnectRequestPacket packet, NetPeer peer)
        {
            logger.LogError($"Player Wanting to reconnect {packet.ProfileId}");

            switch (packet.PackageType)
            {
                case EReconnectPackgeType.Everything:
                    logger.LogError($"Reconnecting player requesting eveything");
                    StartCoroutine(SyncClientToHost(packet, peer));
                    break;
                case EReconnectPackgeType.AirdropSetup:
                    _ = SendClientAirdropPackets(peer);
                    break;
                default:
                    throw new Exception("reconnecting player sent unknown package type");
            }
        }

        private async Task SendClientAirdropPackets(NetPeer peer)
        {
            logger.LogError($"Reconnecting player requesting airdrops");
            // send airdrop data

            if (FikaBackendUtils.OldAirdropPackets.Count == 0)
            {
                // no recorded airdrops
                return;
            }

            for (int i = 0; i < FikaBackendUtils.OldAirdropBoxes.Count; i++)
            {
                FikaAirdropBox box = FikaBackendUtils.OldAirdropBoxes.ElementAt(i);
                AirdropPacket airPacket = FikaBackendUtils.OldAirdropPackets.ElementAt(i);
                AirdropLootPacket lootPacket = FikaBackendUtils.OldLootPackets.ElementAt(i);

                // edit airdrop config for boxes current height - allow some give way otherwise it clips the floor
                airPacket.DropHeight = (int)box.transform.position.y + 5;

                // send airdrop config
                _dataWriter.Reset();
                SendDataToPeer(peer, _dataWriter, ref airPacket, DeliveryMethod.ReliableOrdered);

                // send loot packet
                await Task.Delay(500);
                _dataWriter.Reset();
                SendDataToPeer(peer, _dataWriter, ref lootPacket, DeliveryMethod.ReliableOrdered);

                // wait 2 seconds between?
                await Task.Delay(2000);
            }
        }

        public IEnumerator SyncClientToHost(ReconnectRequestPacket packet, NetPeer peer)
        {
            while (!Singleton<GameWorld>.Instantiated)
            {
                yield return new WaitUntil(() => Singleton<GameWorld>.Instantiated);
            }

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            ClientGameWorld ClientgameWorld = Singleton<GameWorld>.Instance as ClientGameWorld;
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

            ObservedCoopPlayer playerToUse = null;

            foreach (CoopPlayer coopPlayer in Players.Values)
            {
                if (coopPlayer.ProfileId == packet.ProfileId)
                {
                    playerToUse = (ObservedCoopPlayer)coopPlayer;
                }
            }

            if (playerToUse == null)
            {
                logger.LogError($"Player was not found");
            }

            WorldInteractiveObject[] interactiveObjects = [.. coopHandler.ListOfInteractiveObjects.Values];

            WindowBreaker[] windows = ClientgameWorld.Windows.Where(x => x.AvailableToSync && x.IsDamaged).ToArray();

            LampController[] lights = LocationScene.GetAllObjects<LampController>(false).ToArray();

            Throwable[] smokes = ClientgameWorld.Grenades.Where(x => x as SmokeGrenade is not null).ToArray();

            LootItemPositionClass[] items = gameWorld.GetJsonLootItems().Where(x => x as GClass1209 is null).ToArray();

            Profile.GClass1768 health = playerToUse.NetworkHealthController.Store(null);
            GClass2431.GClass2434[] effects = [.. playerToUse.NetworkHealthController.IReadOnlyList_0];

            foreach (GClass2431.GClass2434 effect in effects)
            {
                if (!effect.Active)
                {
                    continue;
                }

                if (health.BodyParts[effect.BodyPart].Effects == null)
                {
                    health.BodyParts[effect.BodyPart].Effects = [];
                }

                if (!health.BodyParts[effect.BodyPart].Effects.ContainsKey(effect.GetType().Name) && effect is GInterface250)
                {
                    health.BodyParts[effect.BodyPart].Effects.Add(effect.GetType().Name, new Profile.GClass1768.GClass1769 { Time = -1f, ExtraData = effect.StoreObj });
                }
            }

            playerToUse.Profile.Health = health;
            playerToUse.Profile.Info.EntryPoint = coopGame.InfiltrationPoint;

            Vector3[] airdropLocations = new Vector3[FikaBackendUtils.OldAirdropBoxes.Count];
            foreach (FikaAirdropBox box in FikaBackendUtils.OldAirdropBoxes)
            {
                airdropLocations.AddItem(box.transform.position);
            }

            ReconnectResponsePacket responsePacket = new(playerToUse.NetId, playerToUse.HealthController.IsAlive, playerToUse.Transform.position,
                playerToUse.Transform.rotation, playerToUse.Pose, playerToUse.PoseLevel, playerToUse.IsInPronePose,
                interactiveObjects, windows, lights, smokes, new FikaSerialization.PlayerInfoPacket() { Profile = playerToUse.Profile },
                items, gameWorld.AllPlayersEverExisted.Count(), FikaBackendUtils.OldAirdropPackets.Count > 0, FikaBackendUtils.OldAirdropPackets.Count,
                [.. FikaBackendUtils.OldAirdropPackets], [.. FikaBackendUtils.OldLootPackets]);

            _dataWriter.Reset();
            SendDataToPeer(peer, _dataWriter, ref responsePacket, DeliveryMethod.ReliableUnordered);
        }
    }
}
