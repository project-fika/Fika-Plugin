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
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.Utils;
using LiteNetLib;
using LiteNetLib.Utils;
using Open.Nat;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Utils.ColorUtils;

namespace Fika.Core.Networking
{
    public class FikaServer : MonoBehaviour, INetEventListener, INetLogger, INatPunchListener
    {
        public NetPacketProcessor packetProcessor = new();
        public CoopPlayer MyPlayer;
        public Dictionary<int, CoopPlayer> Players => coopHandler.Players;
        public List<string> PlayersMissing = [];
        public string MyExternalIP { get; private set; } = NetUtils.GetLocalIp(LocalAddrType.IPv4);
        public int ReadyClients = 0;
        public NetManager NetServer
        {
            get
            {
                return netServer;
            }
        }
        public DateTime timeSinceLastPeerDisconnected = DateTime.Now.AddDays(1);
        public bool hasHadPeer = false;
        public bool Started
        {
            get
            {
                if (netServer == null)
                {
                    return false;
                }
                return netServer.IsRunning;
            }
        }

        private NetManager netServer;
        public NetDataWriter Writer => dataWriter;
        private readonly NetDataWriter dataWriter = new();
        private int Port => FikaPlugin.UDPPort.Value;
        private CoopHandler coopHandler;
        private readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Server");
        private int currentNetId;
        private FikaChat fikaChat;
        private CancellationTokenSource natIntroduceRoutineCts;
        private int statisticsCounter = 0;

        public async Task Init()
        {
            NetworkGameSession.RTT = 0;
            NetworkGameSession.LossPercent = 0;

            // Start at 1 to avoid having 0 and making us think it's working when it's not
            currentNetId = 1;

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
            packetProcessor.SubscribeNetSerializable<TextMessagePacket, NetPeer>(OnTextMessagePacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestConditionPacket, NetPeer>(OnQuestConditionPacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestItemPacket, NetPeer>(OnQuestItemPacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestDropItemPacket, NetPeer>(OnQuestDropItemPacketReceived);
            packetProcessor.SubscribeNetSerializable<SpawnpointPacket, NetPeer>(OnSpawnPointPacketReceived);
            packetProcessor.SubscribeNetSerializable<InteractableInitPacket, NetPeer>(OnInteractableInitPacketReceived);

            netServer = new NetManager(this)
            {
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true,
                UpdateTime = 15,
                AutoRecycle = true,
                IPv6Enabled = false,
                DisconnectTimeout = FikaPlugin.ConnectionTimeout.Value * 1000,
                UseNativeSockets = FikaPlugin.NativeSockets.Value,
                EnableStatistics = true,
                NatPunchEnabled = true
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
                netServer.NatPunchModule.Init(this);
                netServer.Start();

                natIntroduceRoutineCts = new CancellationTokenSource();

                string natPunchServerIP = FikaPlugin.Instance.NatPunchServerIP;
                int natPunchServerPort = FikaPlugin.Instance.NatPunchServerPort;
                string token = $"server:{RequestHandler.SessionId}";

                Task natIntroduceTask = Task.Run(() => NatIntroduceRoutine(natPunchServerIP, natPunchServerPort, token, natIntroduceRoutineCts.Token));
            }
            else
            {
                if (FikaPlugin.ForceBindIP.Value != "Disabled")
                {
                    netServer.Start(FikaPlugin.ForceBindIP.Value, "", Port);
                }
                else
                {
                    netServer.Start(Port);
                }
            }

            logger.LogInfo("Started Fika Server");

            NotificationManagerClass.DisplayMessageNotification($"Server started on port {netServer.LocalPort}.",
                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);

            string[] Ips = [];

            foreach (string ip in FikaPlugin.Instance.LocalIPs)
            {
                if (ValidateLocalIP(ip))
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

            SetHostRequest body = new(Ips, Port, FikaPlugin.UseNatPunching.Value, FikaBackendUtils.IsDedicatedGame);
            FikaRequestHandler.UpdateSetHost(body);

            FikaEventDispatcher.DispatchEvent(new FikaServerCreatedEvent(this));
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
                            Interactables = world.Interactables
                        };

                        dataWriter.Reset();
                        SendDataToPeer(peer, dataWriter, ref response, DeliveryMethod.ReliableUnordered);
                    }
                }
            }
        }

        private void OnSpawnPointPacketReceived(SpawnpointPacket packet, NetPeer peer)
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
            if (coopGame != null)
            {
                if (packet.IsRequest)
                {
                    SpawnpointPacket response = new(false)
                    {
                        Name = coopGame.GetSpawnpointName()
                    };

                    dataWriter.Reset();
                    SendDataToPeer(peer, dataWriter, ref response, DeliveryMethod.ReliableUnordered);
                }
            }
            else
            {
                logger.LogError("OnSpawnPointPacketReceived: CoopGame was null upon receiving packet!"); ;
            }
        }

        private void OnQuestDropItemPacketReceived(QuestDropItemPacket packet, NetPeer peer)
        {
            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);

            if (MyPlayer.HealthController.IsAlive)
            {
                if (MyPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestDropItemPacket(ref packet);
                }
            }
        }

        private bool ValidateLocalIP(string LocalIP)
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
            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);

            if (MyPlayer.HealthController.IsAlive)
            {
                if (MyPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
                {
                    sharedQuestController.ReceiveQuestItemPacket(ref packet);
                }
            }
        }

        private void OnQuestConditionPacketReceived(QuestConditionPacket packet, NetPeer peer)
        {
            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);

            if (MyPlayer.HealthController.IsAlive)
            {
                if (MyPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
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

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);
        }

        public int PopNetId()
        {
            int netId = currentNetId;
            currentNetId++;

            return netId;
        }

        public void SetupGameVariables(CoopPlayer coopPlayer)
        {
            coopHandler = CoopHandler.CoopHandlerParent.GetComponent<CoopHandler>();
            MyPlayer = coopPlayer;
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

            int netId = PopNetId();
            packet.netId = netId;
            if (packet.PlayerInfo.Profile.ProfileId != MyPlayer.ProfileId)
            {
                coopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.netId, packet.IsAlive, packet.IsAI);
            }

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableUnordered, peer);

            AssignNetIdPacket assignNetIdPacket = new()
            {
                NetId = netId
            };

            dataWriter.Reset();
            packetProcessor.WriteNetSerializable(dataWriter, ref assignNetIdPacket);
            peer.Send(dataWriter, DeliveryMethod.ReliableUnordered);
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

                        dataWriter.Reset();
                        SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        BTRInteractionPacket newPacket = new(packet.NetId)
                        {
                            HasInteractPacket = false
                        };

                        dataWriter.Reset();
                        SendDataToAll(dataWriter, ref newPacket, DeliveryMethod.ReliableOrdered);
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
                    dataWriter.Reset();
                    SendDataToPeer(peer, dataWriter, ref weatherPacket2, DeliveryMethod.ReliableOrdered);
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

                    dataWriter.Reset();
                    SendDataToPeer(peer, dataWriter, ref exfilPacket, DeliveryMethod.ReliableOrdered);
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
                    coopHandler.HumanPlayers.Remove(playerToApply);
                    if (!coopHandler.ExtractedPlayers.Contains(packet.NetId))
                    {
                        coopHandler.ExtractedPlayers.Add(packet.NetId);
                        CoopGame coopGame = coopHandler.LocalGameInstance;
                        coopGame.ExtractedPlayers.Add(packet.NetId);
                        coopGame.ClearHostAI(playerToApply);

                        if (FikaPlugin.ShowNotifications.Value)
                        {
                            string nickname = !string.IsNullOrEmpty(playerToApply.Profile.Info.MainProfileNickname) ? playerToApply.Profile.Info.MainProfileNickname : playerToApply.Profile.Nickname;
                            NotificationManagerClass.DisplayMessageNotification($"Group member {ColorizeText(Colors.GREEN, nickname)} has extracted.",
                                            EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.EntryPoint);
                        }
                    }

                    playerToApply.Dispose();
                    AssetPoolObject.ReturnToPool(playerToApply.gameObject, true);
                }
            }
            else if (packet.PacketType == EPackageType.Ping && FikaPlugin.UsePingSystem.Value)
            {
                PingFactory.ReceivePing(packet.PingLocation, packet.PingType, packet.PingColor, packet.Nickname, packet.LocaleId);
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
            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnHealthSyncPacketReceived(HealthSyncPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.HealthSyncPackets?.Enqueue(packet);
            }

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnInformationPacketReceived(InformationPacket packet, NetPeer peer)
        {
            ReadyClients += packet.ReadyPlayers;

            InformationPacket respondPackage = new(false)
            {
                NumberOfPlayers = netServer.ConnectedPeersCount,
                ReadyPlayers = ReadyClients,
            };

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref respondPackage, DeliveryMethod.ReliableOrdered);
        }

        private void OnAllCharacterRequestPacketReceived(AllCharacterRequestPacket packet, NetPeer peer)
        {
            if (coopHandler == null)
            {
                return;
            }

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
                    dataWriter.Reset();
                    SendDataToPeer(peer, dataWriter, ref requestPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            if (!Players.ContainsKey(packet.NetId) && !PlayersMissing.Contains(packet.ProfileId) && !coopHandler.ExtractedPlayers.Contains(packet.NetId))
            {
                PlayersMissing.Add(packet.ProfileId);
                logger.LogInfo($"Requesting missing player from server.");
                AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId);
                dataWriter.Reset();
                SendDataToPeer(peer, dataWriter, ref requestPacket, DeliveryMethod.ReliableOrdered);
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

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
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
                    if (result.Value is UnloadOperationClass unloadOperation)
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

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnArmorDamagePacketReceived(ArmorDamagePacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.ArmorDamagePackets?.Enqueue(packet);
            }

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnFirearmPacketReceived(WeaponPacket packet, NetPeer peer)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.FirearmPackets?.Enqueue(packet);
            }

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        private void OnGameTimerPacketReceived(GameTimerPacket packet, NetPeer peer)
        {
            if (!packet.IsRequest)
                return;

            CoopGame game = coopHandler.LocalGameInstance;
            if (game != null)
            {
                GameTimerPacket gameTimerPacket = new(false, (game.GameTimer.SessionTime - game.GameTimer.PastTime).Value.Ticks);
                dataWriter.Reset();
                SendDataToPeer(peer, dataWriter, ref gameTimerPacket, DeliveryMethod.ReliableOrdered);
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

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableOrdered, peer);
        }

        protected void Update()
        {
            netServer?.PollEvents();
            netServer?.NatPunchModule?.PollEvents();

            statisticsCounter++;
            if (statisticsCounter > 600)
            {
                statisticsCounter = 0;
                SendStatisticsPacket();
            }
        }

        private void SendStatisticsPacket()
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            StatisticsPacket packet = new(fps);

            dataWriter.Reset();
            SendDataToAll(dataWriter, ref packet, DeliveryMethod.ReliableUnordered);
        }

        protected void OnDestroy()
        {
            netServer?.Stop();

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
                    netServer.SendToAll(writer, deliveryMethod, peerToExclude);
                }
            }
            else
            {
                packetProcessor.WriteNetSerializable(writer, ref packet);
                netServer.SendToAll(writer, deliveryMethod);
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
                netServer.SendUnconnectedMessage(resp, remoteEndPoint);
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
                            netServer.SendUnconnectedMessage(resp, remoteEndPoint);
                            logger.LogInfo("PingingRequest: Correct ping query, sending response");
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
            if (netServer.ConnectedPeersCount == 0)
            {
                timeSinceLastPeerDisconnected = DateTime.Now;
            }

            if (FikaBackendUtils.IsDedicatedGame)
            {
                if (netServer.ConnectedPeersCount == 0)
                {
                    foreach (Profile profile in Singleton<ClientApplication<ISession>>.Instance.Session.AllProfiles)
                    {
                        if (profile is null)
                        {
                            continue;
                        }

                        if (profile.ProfileId == RequestHandler.SessionId)
                        {
                            foreach (Profile.ProfileHealthClass.GClass1770 bodyPartHealth in profile.Health.BodyParts.Values)
                            {
                                bodyPartHealth.Effects.Clear();
                                bodyPartHealth.Health.Current = bodyPartHealth.Health.Maximum;
                            }
                        }
                    }

                    // End the raid
                    Singleton<IFikaGame>.Instance.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId,
                            Singleton<IFikaGame>.Instance.MyExitStatus,
                            Singleton<IFikaGame>.Instance.MyExitLocation, 0);
                }
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
    }
}
