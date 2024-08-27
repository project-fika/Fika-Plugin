// © 2024 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.Interactive;
using EFT.UI;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
using Fika.Core.Networking.Http.Models;
using Fika.Core.Networking.Packets.GameWorld;
using Fika.Core.Utils;
using HarmonyLib;
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
using static Fika.Core.Networking.Packets.GameWorld.ReconnectPacket;
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
		public bool HasHadPeer = false;
		public bool RaidInitialized = false;
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

		private NetManager netServer;
		public NetDataWriter Writer => dataWriter;
		private DateTime? gameStartTime;
		private readonly NetDataWriter dataWriter = new();
		private int Port => FikaPlugin.UDPPort.Value;

		private CoopHandler coopHandler;
		private readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Server");
		private int currentNetId;
		private FikaChat fikaChat;
		private CancellationTokenSource natIntroduceRoutineCts;
		private int statisticsCounter = 0;

#if DEBUG
		private bool simulateFail = false;
#endif

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
			packetProcessor.SubscribeNetSerializable<WorldLootPacket, NetPeer>(OnWorldLootPacketReceived);
			packetProcessor.SubscribeNetSerializable<ReconnectPacket, NetPeer>(OnReconnectPacketReceived);
			packetProcessor.SubscribeNetSerializable<ResyncInventoryPacket, NetPeer>(OnResyncInventoryPacketReceived);

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

			NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.SERVER_STARTED.Localized(), netServer.LocalPort),
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
				NotificationManagerClass.DisplayMessageNotification(LocaleUtils.NO_VALID_IP.Localized(),
					iconType: EFT.Communications.ENotificationIconType.Alert);
			}

			SetHostRequest body = new(Ips, Port, FikaPlugin.UseNatPunching.Value, FikaBackendUtils.IsDedicatedGame);
			FikaRequestHandler.UpdateSetHost(body);

			FikaEventDispatcher.DispatchEvent(new FikaServerCreatedEvent(this));
		}

		private void OnResyncInventoryPacketReceived(ResyncInventoryPacket packet, NetPeer peer)
		{
			if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				if (playerToApply is ObservedCoopPlayer observedPlayer)
				{
					SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
					if (observedPlayer.InventoryControllerClass is ObservedInventoryController observedController)
					{
						observedController.SetNewID(new(packet.MongoId));
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
							ReconnectPacket ownCharacterPacket = new(false)
							{
								Type = EReconnectDataType.OwnCharacter,
								Profile = observedCoopPlayer.Profile,
								ProfileHealthClass = observedCoopPlayer.NetworkHealthController.Store(),
								PlayerPosition = observedCoopPlayer.Position
							};

							SendDataToPeer(peer, ref ownCharacterPacket, DeliveryMethod.ReliableOrdered);

							observedCoopPlayer.HealthBar.ClearEffects();
							GenericPacket clearEffectsPacket = new(EPackageType.ClearEffects)
							{
								NetId = observedCoopPlayer.NetId
							};

							SendDataToAll(ref clearEffectsPacket, DeliveryMethod.ReliableUnordered, peer);
						}
					}

					return;
				}

				GameWorld gameWorld = Singleton<GameWorld>.Instance;
				Traverse worldTraverse = Traverse.Create(gameWorld.World_0);

				GClass724<int, Throwable>.GStruct43 grenades = gameWorld.Grenades.GetValuesEnumerator();
				List<GStruct35> smokeData = [];
				foreach (Throwable item in grenades)
				{
					if (item is SmokeGrenade smokeGrenade)
					{
						smokeData.Add(smokeGrenade.NetworkData);
					}
				}

				if (smokeData.Count > 0)
				{
					ReconnectPacket throwablePacket = new(false)
					{
						Type = EReconnectDataType.Throwable,
						ThrowableData = smokeData
					};

					SendDataToPeer(peer, ref throwablePacket, DeliveryMethod.ReliableOrdered);
				}

				List<WorldInteractiveObject.GStruct384> interactivesData = [];
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
					ReconnectPacket interactivePacket = new(false)
					{
						Type = EReconnectDataType.Interactives,
						InteractivesData = interactivesData
					};

					dataWriter.Reset();
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
					ReconnectPacket lampPacket = new(false)
					{
						Type = EReconnectDataType.LampControllers,
						LampStates = lampStates
					};

					dataWriter.Reset();
					SendDataToPeer(peer, ref lampPacket, DeliveryMethod.ReliableOrdered);
				}

				GClass724<int, WindowBreaker>.GStruct43 windows = gameWorld.Windows.GetValuesEnumerator();
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
					ReconnectPacket windowPacket = new(false)
					{
						Type = EReconnectDataType.Windows,
						WindowBreakerStates = windowData
					};

					dataWriter.Reset();
					SendDataToPeer(peer, ref windowPacket, DeliveryMethod.ReliableOrdered);
				}

				foreach (CoopPlayer player in coopHandler.Players.Values)
				{
					SendCharacterPacket characterPacket = new(new FikaSerialization.PlayerInfoPacket(player.Profile, player.InventoryControllerClass.CurrentId),
						player.HealthController.IsAlive, player.IsAI, player.Position, player.NetId);

					dataWriter.Reset();
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

						dataWriter.Reset();
						SendDataToPeer(peer, ref assignPacket, DeliveryMethod.ReliableOrdered);
					}
				}

				ReconnectPacket finishPacket = new(false)
				{
					Type = EReconnectDataType.Finished
				};

				SendDataToPeer(peer, ref finishPacket, DeliveryMethod.ReliableOrdered);
			}
		}

		private void OnWorldLootPacketReceived(WorldLootPacket packet, NetPeer peer)
		{
			if (Singleton<IFikaGame>.Instance != null && Singleton<IFikaGame>.Instance is CoopGame coopGame)
			{
				WorldLootPacket response = new(false)
				{
					Data = coopGame.GetHostLootItems()
				};

				SendDataToPeer(peer, ref response, DeliveryMethod.ReliableUnordered);
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
							Interactables = world.Interactables
						};

						SendDataToPeer(peer, ref response, DeliveryMethod.ReliableUnordered);
					}
				}
			}
		}

		private void OnSpawnPointPacketReceived(SpawnpointPacket packet, NetPeer peer)
		{
			if (Singleton<IFikaGame>.Instance != null && Singleton<IFikaGame>.Instance is CoopGame coopGame)
			{
				if (packet.IsRequest)
				{
					SpawnpointPacket response = new(false)
					{
						Name = coopGame.GetSpawnpointName()
					};


					SendDataToPeer(peer, ref response, DeliveryMethod.ReliableUnordered);
				}
			}
		}

		private void OnQuestDropItemPacketReceived(QuestDropItemPacket packet, NetPeer peer)
		{
			SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered, peer);

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
			SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered, peer);

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
			SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered, peer);

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

			SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered, peer);
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
				coopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.netId, packet.IsAlive, packet.IsAI, packet.PlayerInfo.ControllerId.Value);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered, peer);

			AssignNetIdPacket assignNetIdPacket = new()
			{
				NetId = netId
			};

			SendDataToPeer(peer, ref assignNetIdPacket, DeliveryMethod.ReliableUnordered);
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

						SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
					}
					else
					{
						BTRInteractionPacket newPacket = new(packet.NetId)
						{
							HasInteractPacket = false
						};

						SendDataToAll(ref newPacket, DeliveryMethod.ReliableOrdered);
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

					SendDataToPeer(peer, ref weatherPacket2, DeliveryMethod.ReliableOrdered);
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
					{
						return;
					}

					ExfiltrationPacket exfilPacket = new(false)
					{
						ExfiltrationAmount = exfilController.ExfiltrationPoints.Length,
						ExfiltrationPoints = [],
						StartTimes = []
					};

					foreach (ExfiltrationPoint exfilPoint in exfilController.ExfiltrationPoints)
					{
						exfilPacket.ExfiltrationPoints.Add(exfilPoint.Settings.Name, exfilPoint.Status);
						exfilPacket.StartTimes.Add(exfilPoint.Settings.StartTime);
					}

					if (MyPlayer.Side == EPlayerSide.Savage && exfilController.ScavExfiltrationPoints != null)
					{
						exfilPacket.HasScavExfils = true;
						exfilPacket.ScavExfiltrationAmount = exfilController.ScavExfiltrationPoints.Length;
						exfilPacket.ScavExfiltrationPoints = [];
						exfilPacket.ScavStartTimes = [];

						foreach (ScavExfiltrationPoint scavExfilPoint in exfilController.ScavExfiltrationPoints)
						{
							exfilPacket.ScavExfiltrationPoints.Add(scavExfilPoint.Settings.Name, scavExfilPoint.Status);
							exfilPacket.ScavStartTimes.Add(scavExfilPoint.Settings.StartTime);
						}
					}

					SendDataToPeer(peer, ref exfilPacket, DeliveryMethod.ReliableOrdered);
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
							NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_EXTRACTED.Localized(),
								ColorizeText(Colors.GREEN, nickname)),
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

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnHealthSyncPacketReceived(HealthSyncPacket packet, NetPeer peer)
		{
			if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.HealthSyncPackets?.Enqueue(packet);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnInformationPacketReceived(InformationPacket packet, NetPeer peer)
		{
			ReadyClients += packet.ReadyPlayers;

			bool hostReady = coopHandler != null && coopHandler.LocalGameInstance.Status == GameStatus.Started;

			InformationPacket respondPackage = new(false)
			{
				NumberOfPlayers = netServer.ConnectedPeersCount,
				ReadyPlayers = ReadyClients,
				HostReady = hostReady,
				HostLoaded = RaidInitialized
			};

			if (hostReady)
			{
				respondPackage.GameTime = gameStartTime.Value;
				GameTimerClass gameTimer = coopHandler.LocalGameInstance.GameTimer;
				respondPackage.SessionTime = gameTimer.SessionTime.Value;
			}

			SendDataToAll(ref respondPackage, DeliveryMethod.ReliableUnordered);
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
						PlayerInfo = new(player.Profile, player.InventoryControllerClass.CurrentId),
						IsAlive = player.HealthController.IsAlive,
						IsAI = player is CoopBot,
						Position = player.Transform.position,
						NetId = player.NetId
					};
					SendDataToPeer(peer, ref requestPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			if (!Players.ContainsKey(packet.NetId) && !PlayersMissing.Contains(packet.ProfileId) && !coopHandler.ExtractedPlayers.Contains(packet.NetId))
			{
				PlayersMissing.Add(packet.ProfileId);
				logger.LogInfo($"Requesting missing player from server.");
				AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId);
				SendDataToPeer(peer, ref requestPacket, DeliveryMethod.ReliableOrdered);
			}
			if (!packet.IsRequest && PlayersMissing.Contains(packet.ProfileId))
			{
				logger.LogInfo($"Received CharacterRequest from client: ProfileID: {packet.PlayerInfo.Profile.ProfileId}, Nickname: {packet.PlayerInfo.Profile.Nickname}");
				if (packet.ProfileId != MyPlayer.ProfileId)
				{
					coopHandler.QueueProfile(packet.PlayerInfo.Profile, new Vector3(packet.Position.x, packet.Position.y + 0.5f, packet.Position.y),
						packet.NetId, packet.IsAlive, packet.IsAI, packet.PlayerInfo.ControllerId.Value);
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

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnInventoryPacketReceived(InventoryPacket packet, NetPeer peer)
		{
			if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				using MemoryStream memoryStream = new(packet.ItemControllerExecutePacket.OperationBytes);
				using BinaryReader binaryReader = new(memoryStream);
				try
				{
					OperationCallbackPacket operationCallbackPacket;
					GStruct411 result = playerToApply.ToInventoryOperation(binaryReader.ReadPolymorph<GClass1543>());
					if (!result.Succeeded)
					{
						logger.LogError($"Inventory operation {packet.ItemControllerExecutePacket.CallbackId} was rejected from {playerToApply.Profile.Info.MainProfileNickname}. Reason: {result.Error}");
						operationCallbackPacket = new(playerToApply.NetId, packet.ItemControllerExecutePacket.CallbackId, EOperationStatus.Failed)
						{
							Error = result.Error.ToString()
						};
						SendDataToPeer(peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

						ResyncInventoryPacket resyncPacket = new(packet.NetId);
						SendDataToPeer(peer, ref resyncPacket, DeliveryMethod.ReliableOrdered);
						return;
					}

					InventoryOperationHandler opHandler = new()
					{
						opResult = result,
						operationId = packet.ItemControllerExecutePacket.CallbackId,
						netId = playerToApply.NetId,
						peer = peer,
						server = this
					};
#if DEBUG
					operationCallbackPacket = new(playerToApply.NetId, packet.ItemControllerExecutePacket.CallbackId,
						simulateFail ? EOperationStatus.Failed : EOperationStatus.Started);
#else
					operationCallbackPacket = new(playerToApply.NetId, packet.ItemControllerExecutePacket.CallbackId, EOperationStatus.Started);
#endif
					SendDataToPeer(peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

					opHandler.opResult.Value.vmethod_0(new Callback(opHandler.HandleResult), false);
				}
				catch (Exception exception)
				{
					logger.LogError($"ItemControllerExecutePacket::Exception thrown on netId {playerToApply.NetId}, {playerToApply.Profile.Info.MainProfileNickname}: {exception}");
					OperationCallbackPacket callbackPacket = new(playerToApply.NetId, packet.ItemControllerExecutePacket.CallbackId, EOperationStatus.Failed)
					{
						Error = exception.Message
					};
					SendDataToAll(ref callbackPacket, DeliveryMethod.ReliableOrdered);

					ResyncInventoryPacket resyncPacket = new(packet.NetId);
					SendDataToPeer(peer, ref resyncPacket, DeliveryMethod.ReliableOrdered);
				}
			}
		}

		private void OnDamagePacketReceived(DamagePacket packet, NetPeer peer)
		{
			if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.DamagePackets?.Enqueue(packet);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnArmorDamagePacketReceived(ArmorDamagePacket packet, NetPeer peer)
		{
			if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.ArmorDamagePackets?.Enqueue(packet);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnFirearmPacketReceived(WeaponPacket packet, NetPeer peer)
		{
			if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.FirearmPackets?.Enqueue(packet);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnGameTimerPacketReceived(GameTimerPacket packet, NetPeer peer)
		{
			if (!packet.IsRequest)
				return;

			CoopGame game = coopHandler.LocalGameInstance;
			if (game != null)
			{
				GameTimerPacket gameTimerPacket = new(false, (game.GameTimer.SessionTime - game.GameTimer.PastTime).Value.Ticks, game.GameTimer.StartDateTime.Value.Ticks);
				SendDataToPeer(peer, ref gameTimerPacket, DeliveryMethod.ReliableOrdered);
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

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
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

			SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
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

		[Obsolete("SendDataToAll with a NetDataWriter specified is deprecated and will be removed in newer versions of Fika, please use SendDataToALl without a writer.")]
		public void SendDataToAll<T>(NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod, NetPeer peerToExclude = null) where T : INetSerializable
		{
			SendDataToAll(ref packet, deliveryMethod, peerToExclude);
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
			}
			else
			{
				packetProcessor.WriteNetSerializable(dataWriter, ref packet);
				netServer.SendToAll(dataWriter, deliveryMethod);
			}
		}

		[Obsolete("SendDataToPeer with a NetDataWriter specified is deprecated and will be removed in newer versions of Fika, please use SendDataToPeer without a writer.")]
		public void SendDataToPeer<T>(NetPeer peer, NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
		{
			SendDataToPeer(peer, ref packet, deliveryMethod);
		}

		public void SendDataToPeer<T>(NetPeer peer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
		{
			dataWriter.Reset();
			packetProcessor.WriteNetSerializable(dataWriter, ref packet);
			peer.Send(dataWriter, deliveryMethod);
		}

		public void OnPeerConnected(NetPeer peer)
		{
			NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_CONNECTED.Localized(), peer.Port),
				iconType: EFT.Communications.ENotificationIconType.Friend);
			logger.LogInfo($"Connection established with {peer.Address}:{peer.Port}, id: {peer.Id}.");

			HasHadPeer = true;
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
			NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_DISCONNECTED.Localized(), [peer.Port, disconnectInfo.Reason]),
				iconType: EFT.Communications.ENotificationIconType.Alert);
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
				OperationCallbackPacket operationCallbackPacket;

				if (!result.Succeed)
				{
					FikaPlugin.Instance.FikaLogger.LogError($"Error in operation: {result.Error ?? "An unknown error has occured"}");
					operationCallbackPacket = new(netId, operationId, EOperationStatus.Failed, result.Error ?? "An unknown error has occured");
					server.SendDataToPeer(peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);

					ResyncInventoryPacket resyncPacket = new(netId);
					server.SendDataToPeer(peer, ref resyncPacket, DeliveryMethod.ReliableOrdered);

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

				server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);

				operationCallbackPacket = new(netId, operationId, EOperationStatus.Finished);
				server.SendDataToPeer(peer, ref operationCallbackPacket, DeliveryMethod.ReliableOrdered);
			}
		}
	}
}
