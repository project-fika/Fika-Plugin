// © 2024 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using Coop.Airdrops;
using EFT;
using EFT.AssetsManager;
using EFT.Interactive;
using EFT.MovingPlatforms;
using EFT.UI;
using EFT.Weather;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
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
using UnityEngine;
using static Fika.Core.Utils.ColorUtils;

namespace Fika.Core.Networking
{
    public class FikaClient : MonoBehaviour, INetEventListener
    {
        public NetDataWriter Writer => dataWriter;
        public CoopPlayer MyPlayer;
        public Dictionary<int, CoopPlayer> Players => coopHandler.Players;
        public NetPacketProcessor packetProcessor = new();
        public int Ping = 0;
        public int ServerFPS = 0;
        public int ConnectedClients = 0;
        public int ReadyClients = 0;
        public bool HostReady = false;
        public NetManager NetClient
        {
            get
            {
                return netClient;
            }
        }
        public NetPeer ServerConnection { get; private set; }
        public bool SpawnPointsReceived { get; private set; } = false;
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

        private NetManager netClient;
        private CoopHandler coopHandler;
        private readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Client");
        private NetDataWriter dataWriter = new();
        private FikaChat fikaChat;

        public async void Init()
        {
            NetworkGameSession.RTT = 0;
            NetworkGameSession.LossPercent = 0;

            packetProcessor.SubscribeNetSerializable<PlayerStatePacket>(OnPlayerStatePacketReceived);
            packetProcessor.SubscribeNetSerializable<GameTimerPacket>(OnGameTimerPacketReceived);
            packetProcessor.SubscribeNetSerializable<WeaponPacket>(OnFirearmPacketReceived);
            packetProcessor.SubscribeNetSerializable<DamagePacket>(OnDamagePacketReceived);
            packetProcessor.SubscribeNetSerializable<ArmorDamagePacket>(OnArmorDamagePacketReceived);
            packetProcessor.SubscribeNetSerializable<InventoryPacket>(OnInventoryPacketReceived);
            packetProcessor.SubscribeNetSerializable<CommonPlayerPacket>(OnCommonPlayerPacketReceived);
            packetProcessor.SubscribeNetSerializable<AllCharacterRequestPacket>(OnAllCharacterRequestPacketReceived);
            packetProcessor.SubscribeNetSerializable<InformationPacket>(OnInformationPacketReceived);
            packetProcessor.SubscribeNetSerializable<AirdropPacket>(OnAirdropPacketReceived);
            packetProcessor.SubscribeNetSerializable<AirdropLootPacket>(OnAirdropLootPacketReceived);
            packetProcessor.SubscribeNetSerializable<HealthSyncPacket>(OnHealthSyncPacketReceived);
            packetProcessor.SubscribeNetSerializable<GenericPacket>(OnGenericPacketReceived);
            packetProcessor.SubscribeNetSerializable<ExfiltrationPacket>(OnExfiltrationPacketReceived);
            packetProcessor.SubscribeNetSerializable<WeatherPacket>(OnWeatherPacketReceived);
            packetProcessor.SubscribeNetSerializable<BTRPacket>(OnBTRPacketReceived);
            packetProcessor.SubscribeNetSerializable<BTRInteractionPacket>(OnBTRInteractionPacketReceived);
            packetProcessor.SubscribeNetSerializable<MinePacket>(OnMinePacketReceived);
            packetProcessor.SubscribeNetSerializable<BorderZonePacket>(OnBorderZonePacketReceived);
            packetProcessor.SubscribeNetSerializable<SendCharacterPacket>(OnSendCharacterPacketReceived);
            packetProcessor.SubscribeNetSerializable<AssignNetIdPacket>(OnAssignNetIdPacketReceived);
            packetProcessor.SubscribeNetSerializable<SyncNetIdPacket>(OnSyncNetIdPacketReceived);
            packetProcessor.SubscribeNetSerializable<OperationCallbackPacket>(OnOperationCallbackPacketReceived);
            packetProcessor.SubscribeNetSerializable<TextMessagePacket>(OnTextMessagePacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestConditionPacket>(OnQuestConditionPacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestItemPacket>(OnQuestItemPacketReceived);
            packetProcessor.SubscribeNetSerializable<QuestDropItemPacket>(OnQuestDropItemPacketReceived);
            packetProcessor.SubscribeNetSerializable<SpawnpointPacket>(OnSpawnPointPacketReceived);
            packetProcessor.SubscribeNetSerializable<HalloweenEventPacket>(OnHalloweenEventPacketReceived);
            packetProcessor.SubscribeNetSerializable<InteractableInitPacket>(OnInteractableInitPacketReceived);
            packetProcessor.SubscribeNetSerializable<StatisticsPacket>(OnStatisticsPacketReceived);
            packetProcessor.SubscribeNetSerializable<ThrowablePacket>(OnStatisticsPacketReceived);

            netClient = new NetManager(this)
            {
                UnconnectedMessagesEnabled = true,
                UpdateTime = 15,
                NatPunchEnabled = false,
                IPv6Enabled = false,
                DisconnectTimeout = FikaPlugin.ConnectionTimeout.Value * 1000,
                UseNativeSockets = FikaPlugin.NativeSockets.Value,
                EnableStatistics = true,
                MaxConnectAttempts = 5,
                ReconnectDelay = 1 * 1000
            };

            if (FikaBackendUtils.IsHostNatPunch)
            {
                NetManagerUtils.DestroyPingingClient();
            }

            netClient.Start(FikaBackendUtils.LocalPort);

            string ip = FikaBackendUtils.RemoteIp;
            int port = FikaBackendUtils.RemotePort;

            if (string.IsNullOrEmpty(ip))
            {
                Singleton<PreloaderUI>.Instance.ShowErrorScreen("Network Error", "Unable to connect to the raid server. IP and/or Port was empty when requesting data!");
            }
            else
            {
                ServerConnection = netClient.Connect(ip, port, "fika.core");
            };

            while (ServerConnection.ConnectionState != ConnectionState.Connected)
            {
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogWarning("FikaClient was not able to connect in time!");
#endif
                await Task.Delay(1 * 6000);
                ServerConnection = netClient.Connect(ip, port, "fika.core");
            }

            FikaEventDispatcher.DispatchEvent(new FikaClientCreatedEvent(this));
        }

        private void OnStatisticsPacketReceived(ThrowablePacket packet)
        {
            GClass724<int, Throwable> grenades = Singleton<GameWorld>.Instance.Grenades;
            if (grenades.TryGetByKey(packet.Data.Id, out Throwable throwable))
            {
                throwable.ApplyNetPacket(packet.Data);
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
                    world.RegisterNetworkInteractionObjects(packet.Interactables);
                    CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
                    if (coopGame != null)
                    {
                        coopGame.InteractablesInitialized = true;
                    }
                }
            }
        }

        private void OnSpawnPointPacketReceived(SpawnpointPacket packet)
        {
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
            if (coopGame != null)
            {
                if (!packet.IsRequest && !string.IsNullOrEmpty(packet.Name))
                {
                    coopGame.SpawnId = packet.Name;
                }
            }
            else
            {
                logger.LogError("OnSpawnPointPacketReceived: CoopGame was null upon receiving packet!"); ;
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
            coopHandler = CoopHandler.CoopHandlerParent.GetComponent<CoopHandler>();
            MyPlayer = coopPlayer;
            if (FikaPlugin.EnableChat.Value)
            {
                fikaChat = gameObject.AddComponent<FikaChat>();
            }
        }

        private void OnOperationCallbackPacketReceived(OperationCallbackPacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer player) && player.IsYourPlayer)
            {
                player.HandleCallbackFromServer(in packet);
            }
        }

        private void OnSyncNetIdPacketReceived(SyncNetIdPacket packet)
        {
            Dictionary<int, CoopPlayer> newPlayers = Players;
            if (Players.TryGetValue(packet.NetId, out CoopPlayer player))
            {
                if (player.ProfileId != packet.ProfileId)
                {
                    FikaPlugin.Instance.FikaLogger.LogWarning($"OnSyncNetIdPacketReceived: {packet.ProfileId} had the wrong NetId: {Players[packet.NetId].NetId}, should be {packet.NetId}");
                    for (int i = 0; i < Players.Count; i++)
                    {
                        KeyValuePair<int, CoopPlayer> playerToReorganize = Players.Where(x => x.Value.ProfileId == packet.ProfileId).First();
                        Players.Remove(playerToReorganize.Key);
                        Players[packet.NetId] = playerToReorganize.Value;
                    }
                }
            }
            else
            {
                FikaPlugin.Instance.FikaLogger.LogError($"OnSyncNetIdPacketReceived: Could not find NetId {packet.NetId} in player list!");
                string allPlayers = "";
                foreach (KeyValuePair<int, CoopPlayer> kvp in coopHandler.Players)
                {
                    string toAdd = $"Key: {kvp.Key}, Nickname: {kvp.Value.Profile.Nickname}, NetId: {kvp.Value.NetId}";
                    allPlayers = string.Join(", ", allPlayers + toAdd);
                }
                FikaPlugin.Instance.FikaLogger.LogError(allPlayers);
            }
        }

        private void OnAssignNetIdPacketReceived(AssignNetIdPacket packet)
        {
            FikaPlugin.Instance.FikaLogger.LogInfo($"OnAssignNetIdPacketReceived: Assigned NetId {packet.NetId} to my own client.");
            MyPlayer.NetId = packet.NetId;
            int i = -1;
            foreach (KeyValuePair<int, CoopPlayer> player in Players)
            {
                if (player.Value == MyPlayer)
                {
                    i = player.Key;

                    break;
                }
            }

            if (i == -1)
            {
                FikaPlugin.Instance.FikaLogger.LogError("OnAssignNetIdPacketReceived: Could not find own player among players list");
                return;
            }

            Players.Remove(i);
            Players[packet.NetId] = MyPlayer;
        }

        private void OnSendCharacterPacketReceived(SendCharacterPacket packet)
        {
            if (coopHandler == null)
            {
                return;
            }

            if (packet.PlayerInfo.Profile.ProfileId != MyPlayer.ProfileId)
            {
                coopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.netId, packet.IsAlive, packet.IsAI);
            }
        }

        private void OnBorderZonePacketReceived(BorderZonePacket packet)
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                BorderZone[] borderZones = Singleton<GameWorld>.Instance.BorderZones;
                if (borderZones != null && borderZones.Length > 0)
                {
                    foreach (BorderZone borderZone in borderZones)
                    {
                        if (borderZone.Id == packet.ZoneId)
                        {
                            List<IPlayer> players = Singleton<GameWorld>.Instance.RegisteredPlayers;
                            foreach (IPlayer player in players)
                            {
                                if (player.ProfileId == packet.ProfileId)
                                {
                                    IPlayerOwner playerBridge = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(player.ProfileId);
                                    borderZone.ProcessIncomingPacket(playerBridge, true);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnMinePacketReceived(MinePacket packet)
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

        private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer player))
            {
                player.ProcessInteractWithBTR(packet);
            }
        }

        private void OnBTRPacketReceived(BTRPacket packet)
        {
            if (coopHandler.clientBTR != null)
            {
                coopHandler.clientBTR.btrPackets.Enqueue(packet);
            }
        }

        private void OnWeatherPacketReceived(WeatherPacket packet)
        {
            if (!packet.IsRequest)
            {
                if (WeatherController.Instance != null)
                {
                    WeatherController.Instance.method_0(packet.WeatherClasses);
                }
                else
                {
                    logger.LogWarning("WeatherPacket2Received: WeatherControll was null!");
                }
            }
        }

        private void OnExfiltrationPacketReceived(ExfiltrationPacket packet)
        {
            if (!packet.IsRequest)
            {
                if (ExfiltrationControllerClass.Instance != null)
                {
                    ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

                    if (exfilController.ExfiltrationPoints == null)
                        return;

                    CoopGame coopGame = coopHandler.LocalGameInstance;

                    CarExtraction carExtraction = FindObjectOfType<CarExtraction>();

                    foreach (KeyValuePair<string, EExfiltrationStatus> exfilPoint in packet.ExfiltrationPoints)
                    {
                        ExfiltrationPoint point = exfilController.ExfiltrationPoints.Where(x => x.Settings.Name == exfilPoint.Key).FirstOrDefault();
                        if (point != null || point != default)
                        {
                            if (point.Status != exfilPoint.Value && (exfilPoint.Value == EExfiltrationStatus.RegularMode || exfilPoint.Value == EExfiltrationStatus.UncompleteRequirements))
                            {
                                point.Enable();
                                point.Status = exfilPoint.Value;
                            }
                            else if (point.Status != exfilPoint.Value && exfilPoint.Value == EExfiltrationStatus.NotPresent || exfilPoint.Value == EExfiltrationStatus.Pending)
                            {
                                point.Disable();
                                point.Status = exfilPoint.Value;

                                if (carExtraction != null)
                                {
                                    if (carExtraction.Subscribee == point)
                                    {
                                        carExtraction.Play(true);
                                    }
                                }
                            }
                            coopGame.UpdateExfiltrationUi(point, false, true);
                        }
                        else
                        {
                            logger.LogWarning($"ExfiltrationPacketPacketReceived::ExfiltrationPoints: Could not find exfil point with name '{exfilPoint.Key}'");
                        }
                    }

                    if (MyPlayer.Side == EPlayerSide.Savage && exfilController.ScavExfiltrationPoints != null && packet.HasScavExfils)
                    {
                        foreach (KeyValuePair<string, EExfiltrationStatus> scavExfilPoint in packet.ScavExfiltrationPoints)
                        {
                            ScavExfiltrationPoint scavPoint = exfilController.ScavExfiltrationPoints.Where(x => x.Settings.Name == scavExfilPoint.Key).FirstOrDefault();
                            if (scavPoint != null || scavPoint != default)
                            {
                                if (scavPoint.Status != scavExfilPoint.Value && scavExfilPoint.Value == EExfiltrationStatus.RegularMode)
                                {
                                    scavPoint.Enable();
                                    scavPoint.EligibleIds.Add(MyPlayer.ProfileId);
                                    scavPoint.Status = scavExfilPoint.Value;
                                    coopGame.UpdateExfilPointFromServer(scavPoint, true);
                                }
                                else if (scavPoint.Status != scavExfilPoint.Value && (scavExfilPoint.Value == EExfiltrationStatus.NotPresent || scavExfilPoint.Value == EExfiltrationStatus.Pending))
                                {
                                    scavPoint.Disable();
                                    scavPoint.EligibleIds.Remove(MyPlayer.ProfileId);
                                    scavPoint.Status = scavExfilPoint.Value;
                                    coopGame.UpdateExfilPointFromServer(scavPoint, false);
                                }
                            }
                            else
                            {
                                logger.LogWarning($"ExfiltrationPacketPacketReceived::ScavExfiltrationPoints: Could not find exfil point with name '{scavExfilPoint.Key}'");
                            }
                        }

                        ExfiltrationPoint[] points = exfilController.ScavExfiltrationPoints.Where(x => x.Status == EExfiltrationStatus.RegularMode).ToArray();
                        coopGame.ResetExfilPointsFromServer(points);
                    }

                    SpawnPointsReceived = true;
                }
                else
                {
                    logger.LogWarning($"ExfiltrationPacketPacketReceived: ExfiltrationController was null");
                }
            }
        }

        private void OnGenericPacketReceived(GenericPacket packet)
        {
            switch (packet.PacketType)
            {
                case EPackageType.ClientExtract:
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
                    break;
                case EPackageType.Ping:
                    {
                        PingFactory.ReceivePing(packet.PingLocation, packet.PingType, packet.PingColor, packet.Nickname, packet.LocaleId);
                    }
                    break;
                case EPackageType.TrainSync:
                    {
                        Locomotive locomotive = FindObjectOfType<Locomotive>();
                        if (locomotive != null)
                        {
                            DateTime depart = new(packet.DepartureTime);
                            Traverse.Create(locomotive).Field("_depart").SetValue(depart);
                        }
                        else
                        {
                            logger.LogWarning("GenericPacketReceived: Could not find locomotive!");
                        }
                    }
                    break;
                case EPackageType.ExfilCountdown:
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
                    break;
                case EPackageType.TraderServiceNotification:
                    {
                        if (coopHandler.clientBTR)
                        {
                            coopHandler.clientBTR.DisplayNetworkNotification(packet.TraderServiceType);
                        }
                    }
                    break;
                case EPackageType.DisposeBot:
                    {
                        if (coopHandler.Players.TryGetValue(packet.BotNetId, out CoopPlayer botToDispose))
                        {
                            if (!botToDispose.gameObject.activeSelf)
                            {
                                botToDispose.gameObject.SetActive(true);
                            }

                            if (coopHandler.Players.Remove(packet.BotNetId))
                            {
                                botToDispose.Dispose();
                                AssetPoolObject.ReturnToPool(botToDispose.gameObject, true);
                                logger.LogInfo("Disposing bot: " + packet.BotNetId);
                            }
                            else
                            {
                                logger.LogWarning("Unable to dispose of bot: " + packet.BotNetId);
                            }
                        }
                        else
                        {
                            logger.LogWarning("Unable to dispose of bot: " + packet.BotNetId + ", unable to find GameObject");
                        }
                    }
                    break;
                case EPackageType.RemoveAirdropManager:
                    {
                        if (Singleton<FikaAirdropsManager>.Instance != null)
                        {
                            Destroy(Singleton<FikaAirdropsManager>.Instance);
                        }
                    }
                    break;
                case EPackageType.EnableBot:
                    {
                        if (coopHandler.Players.TryGetValue(packet.BotNetId, out CoopPlayer botToEnable))
                        {
                            if (!botToEnable.gameObject.activeSelf)
                            {
#if DEBUG
                                logger.LogWarning("Enabling " + packet.BotNetId);
#endif
                                botToEnable.gameObject.SetActive(true);
                            }
                            else
                            {
                                logger.LogWarning($"Received packet to enable {botToEnable.ProfileId}, netId {packet.BotNetId} but the bot was already enabled!");
                            }
                        }
                    }
                    break;
                case EPackageType.DisableBot:
                    {
                        if (coopHandler.Players.TryGetValue(packet.BotNetId, out CoopPlayer botToEnable))
                        {
                            if (botToEnable.gameObject.activeSelf)
                            {
#if DEBUG
                                logger.LogWarning("Disabling " + packet.BotNetId);
#endif
                                botToEnable.gameObject.SetActive(false);
                            }
                            else
                            {
                                logger.LogWarning($"Received packet to disable {botToEnable.ProfileId}, netId {packet.BotNetId} but the bot was already disabled!");
                            }
                        }
                    }
                    break;
            }
        }

        private void OnHealthSyncPacketReceived(HealthSyncPacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.HealthSyncPackets?.Enqueue(packet);
            }
        }

        private void OnAirdropLootPacketReceived(AirdropLootPacket packet)
        {
            if (Singleton<FikaAirdropsManager>.Instance != null)
            {
                Singleton<FikaAirdropsManager>.Instance.ReceiveBuildLootContainer(packet);
            }
            else
            {
                logger.LogError("OnAirdropLootPacketReceived: Received loot package but manager is not instantiated!");
            }
        }

        private void OnAirdropPacketReceived(AirdropPacket packet)
        {
            if (Singleton<FikaAirdropsManager>.Instance != null)
            {
                Singleton<FikaAirdropsManager>.Instance.AirdropParameters = new()
                {
                    Config = packet.Config,
                    AirdropAvailable = packet.AirdropAvailable,
                    PlaneSpawned = packet.PlaneSpawned,
                    BoxSpawned = packet.BoxSpawned,
                    DistanceTraveled = packet.DistanceTraveled,
                    DistanceToTravel = packet.DistanceToTravel,
                    DistanceToDrop = packet.DistanceToDrop,
                    Timer = packet.Timer,
                    DropHeight = packet.DropHeight,
                    TimeToStart = packet.TimeToStart,
                    RandomAirdropPoint = packet.BoxPoint,
                    SpawnPoint = packet.SpawnPoint,
                    LookPoint = packet.LookPoint
                };
            }
            else
            {
                logger.LogError("OnAirdropPacketReceived: Received package but manager is not instantiated!");
            }
        }

        private void OnInformationPacketReceived(InformationPacket packet)
        {
            if (!packet.IsRequest)
            {
                ConnectedClients = packet.NumberOfPlayers;
                ReadyClients = packet.ReadyPlayers;
                HostReady = packet.HostReady;
            }
        }

        private void OnAllCharacterRequestPacketReceived(AllCharacterRequestPacket packet)
        {
            if (coopHandler == null)
            {
                return;
            }

            if (!packet.IsRequest)
            {
                logger.LogInfo($"Received CharacterRequest! ProfileID: {packet.PlayerInfo.Profile.ProfileId}, Nickname: {packet.PlayerInfo.Profile.Nickname}");
                if (packet.ProfileId != MyPlayer.ProfileId)
                {
                    coopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI);
                }
            }
            else if (packet.IsRequest)
            {
                logger.LogInfo($"Received CharacterRequest from server, send my Profile.");
                AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId)
                {
                    IsRequest = false,
                    PlayerInfo = new()
                    {
                        Profile = MyPlayer.Profile
                    },
                    IsAlive = MyPlayer.ActiveHealthController.IsAlive,
                    IsAI = MyPlayer.IsAI,
                    Position = MyPlayer.Transform.position,
                    NetId = MyPlayer.NetId
                };
                dataWriter.Reset();
                SendData(dataWriter, ref requestPacket, DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.CommonPlayerPackets?.Enqueue(packet);
            }
        }

        private void OnInventoryPacketReceived(InventoryPacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.InventoryPackets?.Enqueue(packet);
            }
        }

        private void OnDamagePacketReceived(DamagePacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.DamagePackets?.Enqueue(packet);
            }
        }

        private void OnArmorDamagePacketReceived(ArmorDamagePacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.ArmorDamagePackets?.Enqueue(packet);
            }
        }

        private void OnFirearmPacketReceived(WeaponPacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.FirearmPackets?.Enqueue(packet);
            }
        }

        private void OnGameTimerPacketReceived(GameTimerPacket packet)
        {
            TimeSpan sessionTime = new(packet.Tick);

            CoopGame coopGame = coopHandler.LocalGameInstance;

            GameTimerClass gameTimer = coopGame.GameTimer;
            if (gameTimer.StartDateTime.HasValue && gameTimer.SessionTime.HasValue)
            {
                if (gameTimer.PastTime.TotalSeconds < 3)
                {
                    return;
                }

                TimeSpan timeRemain = gameTimer.PastTime + sessionTime;

                gameTimer.ChangeSessionTime(timeRemain);

                Traverse.Create(coopGame.GameUi.TimerPanel).Field("dateTime_0").SetValue(gameTimer.StartDateTime.Value);
            }
        }

        private void OnHalloweenEventPacketReceived(HalloweenEventPacket packet)
        {
            HalloweenEventControllerClass controller = HalloweenEventControllerClass.Instance;

            if (controller == null)
            {
                return;
            }

            switch (packet.PacketType)
            {
                case EHalloweenPacketType.Summon:
                    controller.method_5(new EFT.GlobalEvents.HalloweenSummonStartedEvent() { PointPosition = packet.SummonPosition });
                    break;
                case EHalloweenPacketType.Sync:
                    controller.SetEventState(packet.EventState, false);
                    break;
                case EHalloweenPacketType.Exit:
                    controller.method_3(packet.Exit);
                    break;
            }
        }

        private void OnPlayerStatePacketReceived(PlayerStatePacket packet)
        {
            if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
            {
                playerToApply.PacketReceiver.NewState = packet;
            }
        }

        protected void Update()
        {
            netClient?.PollEvents();

            /*if (_netClient.FirstPeer == null)
            {
                _netClient.SendBroadcast([1], Port);
            }*/
        }

        protected void OnDestroy()
        {
            netClient?.Stop();

            if (fikaChat != null)
            {
                Destroy(fikaChat);
            }

            FikaEventDispatcher.DispatchEvent(new FikaClientDestroyedEvent(this));
        }

        public void SendData<T>(NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            packetProcessor.WriteNetSerializable(writer, ref packet);
            netClient.FirstPeer.Send(writer, deliveryMethod);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            NotificationManagerClass.DisplayMessageNotification($"Connected to server on port {peer.Port}.",
                EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.Friend);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            logger.LogInfo("[CLIENT] We received error " + socketErrorCode);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            packetProcessor.ReadAllPackets(reader, peer);
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
            NetworkGameSession.RTT = peer.RoundTripTime;
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
                NotificationManagerClass.DisplayWarningNotification("Lost connection to host!");
                Destroy(MyPlayer.PacketReceiver);
                MyPlayer.PacketSender.DestroyThis();
                Destroy(this);
                Singleton<FikaClient>.Release(this);
            }
        }
    }
}
