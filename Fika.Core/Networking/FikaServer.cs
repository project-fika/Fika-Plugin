// © 2024 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
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
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Http;
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
using static Fika.Core.Networking.Packets.SubPacket;
using static Fika.Core.Networking.ReconnectPacket;

namespace Fika.Core.Networking
{
	/// <summary>
	/// Server used in P2P connections
	/// </summary>
	public class FikaServer : MonoBehaviour, INetEventListener, INetLogger, INatPunchListener, GInterface241, IFikaNetworkManager
	{
		public int ReadyClients = 0;
		public DateTime TimeSinceLastPeerDisconnected = DateTime.Now.AddDays(1);
		public bool HasHadPeer;
		public bool RaidInitialized;
		public bool HostReady;
		public FikaHostWorld FikaHostWorld { get; set; }
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

		private int sendRate;
		private readonly NetPacketProcessor packetProcessor = new();
		private CoopPlayer MyPlayer;
		private readonly List<string> playersMissing = [];
		private string externalIp = NetUtils.GetLocalIp(LocalAddrType.IPv4);
		private NetManager netServer;
		private DateTime? gameStartTime;
		private readonly NetDataWriter dataWriter = new();
		private int port;
		private CoopHandler coopHandler;
		private readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Server");
		private int currentNetId;
		private FikaChat fikaChat;
		private CancellationTokenSource natIntroduceRoutineCts;
		private int statisticsCounter = 0;
		private Dictionary<Profile, bool> visualProfiles;

		public async Task Init()
		{
			visualProfiles = [];
			if (!FikaBackendUtils.IsDedicated)
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

			// Start at 1 to avoid having 0 and making us think it's working when it's not
			currentNetId = 1;

			packetProcessor.RegisterNestedType(FikaSerializationExtensions.PutRagdollStruct, FikaSerializationExtensions.GetRagdollStruct);
			packetProcessor.RegisterNestedType(FikaSerializationExtensions.PutArtilleryStruct, FikaSerializationExtensions.GetArtilleryStruct);
			packetProcessor.RegisterNestedType(FikaSerializationExtensions.PutGrenadeStruct, FikaSerializationExtensions.GetGrenadeStruct);

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
			packetProcessor.SubscribeNetSerializable<ExfiltrationPacket, NetPeer>(OnExfiltrationPacketReceived);
			packetProcessor.SubscribeNetSerializable<WeatherPacket, NetPeer>(OnWeatherPacketReceived);
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
			packetProcessor.SubscribeNetSerializable<SyncObjectPacket, NetPeer>(OnSyncObjectPacketReceived);
			packetProcessor.SubscribeNetSerializable<SpawnSyncObjectPacket, NetPeer>(OnSpawnSyncObjectPacketReceived);
			packetProcessor.SubscribeNetSerializable<BTRInteractionPacket, NetPeer>(OnBTRInteractionPacketReceived);
			packetProcessor.SubscribeNetSerializable<TraderServicesPacket, NetPeer>(OnTraderServicesPacketReceived);
			packetProcessor.SubscribeNetSerializable<ResyncInventoryIdPacket, NetPeer>(OnResyncInventoryIdPacketReceived);
			packetProcessor.SubscribeNetSerializable<UsableItemPacket, NetPeer>(OnUsableItemPacketReceived);
			packetProcessor.SubscribeNetSerializable<SyncTransitControllersPacket, NetPeer>(OnSyncTransitControllersPacketReceived);
			packetProcessor.SubscribeNetSerializable<TransitInteractPacket, NetPeer>(OnSubscribeNetSerializableReceived);
			packetProcessor.SubscribeNetSerializable<BotStatePacket, NetPeer>(OnBotStatePacketReceived);
			packetProcessor.SubscribeNetSerializable<PingPacket, NetPeer>(OnPingPacketReceived);
			packetProcessor.SubscribeNetSerializable<LootSyncPacket, NetPeer>(OnLootSyncPacketReceived);
			packetProcessor.SubscribeNetSerializable<LoadingProfilePacket, NetPeer>(OnLoadingProfilePacketReceived);
			packetProcessor.SubscribeNetSerializable<SideEffectPacket, NetPeer>(OnSideEffectPacketReceived);

#if DEBUG
			AddDebugPackets();
#endif

			netServer = new(this)
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
				externalIp = FikaPlugin.Instance.WanIP.ToString();
			}

			if (FikaPlugin.UseNatPunching.Value)
			{
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

			NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.SERVER_STARTED.Localized(), netServer.LocalPort),
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

			SetHostRequest body = new(Ips, port, FikaPlugin.UseNatPunching.Value, FikaBackendUtils.IsDedicatedGame);
			FikaRequestHandler.UpdateSetHost(body);
			FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerCreatedEvent(this));
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

			GStruct448<Item> gstruct2 = gameWorld.FindItemById(packet.ItemId);
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

		private void OnSubscribeNetSerializableReceived(TransitInteractPacket packet, NetPeer peer)
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
				transitController.summonedTransits[packet.ProfileId] = new(packet.RaidId, packet.Count, packet.Maps);
				return;
			}

			logger.LogError("OnSyncTransitControllersPacketReceived: TransitController was null!");
		}

		private void AddDebugPackets()
		{
			packetProcessor.SubscribeNetSerializable<SpawnItemPacket, NetPeer>(OnSpawnItemPacketReceived);
		}

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
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			IEnumerable<SynchronizableObject> syncObjects = gameWorld.SynchronizableObjectLogicProcessor.GetSynchronizableObjects();
			foreach (SynchronizableObject obj in syncObjects)
			{
				if (obj.ObjectId == ObjectId)
				{
					LootableContainer container = obj.GetComponentInChildren<LootableContainer>().gameObject.GetComponentInChildren<LootableContainer>();
					if (container != null)
					{
						netId = container.NetId;
						gameWorld.RegisterWorldInteractionObject(container);
						break;
					}
				}
			}

			if (netId == 0)
			{
				logger.LogError("SendAirdropContainerData: Could not find NetId!");
			}

			SpawnSyncObjectPacket packet = new(ObjectId)
			{
				AirdropType = containerType,
				AirdropItem = item,
				ContainerId = item.Id,
				NetId = netId,
				IsStatic = false
			};

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
		}

		public void SendFlareSuccessEvent(string profileId, bool canSendAirdrop)
		{
			FlareSuccessPacket packet = new(profileId, canSendAirdrop);
			SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
		}

		private void OnTraderServicesPacketReceived(TraderServicesPacket packet, NetPeer peer)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				List<TraderServicesClass> services = playerToApply.GetAvailableTraderServices(packet.TraderId).ToList();
				TraderServicesPacket response = new(playerToApply.NetId)
				{
					Services = services
				};

				SendDataToPeer(peer, ref response, DeliveryMethod.ReliableOrdered);
			}
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

		private void OnSpawnSyncObjectPacketReceived(SpawnSyncObjectPacket packet, NetPeer peer)
		{
			// Do nothing
		}

		private void OnSyncObjectPacketReceived(SyncObjectPacket packet, NetPeer peer)
		{
			if (packet.ObjectType == SynchronizableObjectType.Tripwire)
			{
				CoopHostGameWorld gameWorld = (CoopHostGameWorld)Singleton<GameWorld>.Instance;
				TripwireSynchronizableObject tripwire = gameWorld.SynchronizableObjectLogicProcessor.TripwireManager.GetTripwireById(packet.ObjectId);
				if (tripwire != null)
				{
					gameWorld.DeActivateTripwire(tripwire);
				}
				else
				{
					logger.LogError($"OnSyncObjectPacketReceived: Tripwire with id {packet.ObjectId} could not be found!");
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

							SendDataToAll(ref clearEffectsPacket, DeliveryMethod.ReliableUnordered, peer);
						}
					}

					return;
				}

				GameWorld gameWorld = Singleton<GameWorld>.Instance;
				Traverse worldTraverse = Traverse.Create(gameWorld.World_0);

				GClass786<int, Throwable>.GStruct44 grenades = gameWorld.Grenades.GetValuesEnumerator();
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
					ReconnectPacket throwablePacket = new()
					{
						Type = EReconnectDataType.Throwable,
						ThrowableData = smokeData
					};

					SendDataToPeer(peer, ref throwablePacket, DeliveryMethod.ReliableOrdered);
				}

				List<WorldInteractiveObject.GStruct415> interactivesData = [];
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

				GClass786<int, WindowBreaker>.GStruct44 windows = gameWorld.Windows.GetValuesEnumerator();
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
						characterPacket.PlayerInfoPacket.ControllerType = GClass1808.FromController(player.HandsController);
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
			if (Singleton<IFikaGame>.Instance != null && Singleton<IFikaGame>.Instance is CoopGame coopGame)
			{
				WorldLootPacket response = new()
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
							Interactables = (Dictionary<string, int>)world.Interactables
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
			packet.NetId = netId;
			if (packet.PlayerInfoPacket.Profile.ProfileId != MyPlayer.ProfileId)
			{
				coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
					packet.PlayerInfoPacket.ControllerId.Value, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
					packet.PlayerInfoPacket.ControllerType, packet.PlayerInfoPacket.ItemId);
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
				NetworkGame<EftGamePlayerOwner>.Class1513 mineSeeker = new()
				{
					minePosition = packet.MinePositon
				};
				MineDirectional mineDirectional = Singleton<GameWorld>.Instance.MineManager.Mines.FirstOrDefault(mineSeeker.method_0);
				if (mineDirectional == null)
				{
					return;
				}
				mineDirectional.Explosion();
			}
		}

		private void OnWeatherPacketReceived(WeatherPacket packet, NetPeer peer)
		{
			if (packet.IsRequest)
			{
				CoopGame coopGame = CoopHandler.LocalGameInstance;
				if (coopGame != null && coopGame.WeatherReady)
				{
					WeatherPacket response = new()
					{
						IsRequest = false,
						HasData = true,
						Season = coopGame.Season,
						SpringSnowFactor = coopGame.SeasonsSettings != null ? coopGame.SeasonsSettings.SpringSnowFactor : Vector3.zero, // Temp fix
						Amount = coopGame.WeatherClasses.Length,
						WeatherClasses = coopGame.WeatherClasses
					};

					SendDataToPeer(peer, ref response, DeliveryMethod.ReliableUnordered);
				};
			}
		}

		private void OnExfiltrationPacketReceived(ExfiltrationPacket packet, NetPeer peer)
		{
			if (ExfiltrationControllerClass.Instance != null)
			{
				ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;

				if (exfilController.ExfiltrationPoints == null)
				{
					return;
				}

				ExfiltrationPacket exfilPacket = new()
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

		private void OnGenericPacketReceived(GenericPacket packet, NetPeer peer)
		{
			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
			packet.SubPacket.Execute(null);
		}

		private void OnHealthSyncPacketReceived(HealthSyncPacket packet, NetPeer peer)
		{
			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.HealthSyncPackets.Enqueue(packet);
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

			if (HostReady)
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
						requestPacket.PlayerInfoPacket.ControllerType = GClass1808.FromController(pair.Value.HandsController);
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
				AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId);
				SendDataToPeer(peer, ref requestPacket, DeliveryMethod.ReliableOrdered);
			}

			if (!packet.IsRequest && playersMissing.Contains(packet.ProfileId))
			{
				logger.LogInfo($"Received CharacterRequest from client: ProfileID: {packet.PlayerInfoPacket.Profile.ProfileId}, Nickname: {packet.PlayerInfoPacket.Profile.Nickname}");
				if (packet.ProfileId != MyPlayer.ProfileId)
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
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.CommonPlayerPackets?.Enqueue(packet);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnInventoryPacketReceived(InventoryPacket packet, NetPeer peer)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				GClass1193 reader = new(packet.OperationBytes);
				try
				{
					OperationCallbackPacket operationCallbackPacket;
					if (playerToApply.InventoryController is Interface16 inventoryController)
					{
						BaseDescriptorClass descriptor = reader.ReadPolymorph<BaseDescriptorClass>();
						GStruct443 result = inventoryController.CreateOperationFromDescriptor(descriptor);
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
					playerToApply.PacketReceiver.DamagePackets.Enqueue(packet);
					return;
				}

				SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
			}
		}

		private void OnArmorDamagePacketReceived(ArmorDamagePacket packet, NetPeer peer)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.ArmorDamagePackets.Enqueue(packet);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
		}

		private void OnWeaponPacketReceived(WeaponPacket packet, NetPeer peer)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.FirearmPackets.Enqueue(packet);
			}

			SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
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

		public void SendReusableToAll<T>(ref T packet, DeliveryMethod deliveryMethod) where T : class, new()
		{
			dataWriter.Reset();

			packetProcessor.Write(dataWriter, packet);
			netServer.SendToAll(dataWriter, deliveryMethod);
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

			NetworkSettingsPacket packet = new(sendRate);
			SendDataToPeer(peer, ref packet, DeliveryMethod.ReliableOrdered);
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
			NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.PEER_DISCONNECTED.Localized(), [peer.Port, disconnectInfo.Reason]),
				iconType: EFT.Communications.ENotificationIconType.Alert);
			if (netServer.ConnectedPeersCount == 0)
			{
				TimeSinceLastPeerDisconnected = DateTime.Now;
			}

			if (FikaBackendUtils.IsDedicated)
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
							foreach (Profile.ProfileHealthClass.GClass1940 bodyPartHealth in profile.Health.BodyParts.Values)
							{
								bodyPartHealth.Effects.Clear();
								bodyPartHealth.Health.Current = bodyPartHealth.Health.Maximum;
							}
						}
					}

					// End the raid
					Singleton<IFikaGame>.Instance.Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId,
							Singleton<IFikaGame>.Instance.ExitStatus,
							Singleton<IFikaGame>.Instance.ExitLocation, 0);
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

		public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new()
		{
			packetProcessor.SubscribeNetSerializable(handle);
		}

		public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new()
		{
			packetProcessor.SubscribeNetSerializable(handle);
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

		private class InventoryOperationHandler(GStruct443 operationResult, uint operationId, int netId, NetPeer peer, FikaServer server)
		{
			public GStruct443 OperationResult = operationResult;
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
