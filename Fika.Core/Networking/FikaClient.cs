// © 2024 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.AssetsManager;
using EFT.Communications;
using EFT.GlobalEvents;
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
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
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

namespace Fika.Core.Networking
{
	/// <summary>
	/// Client used in P2P connections
	/// </summary>
	public class FikaClient : MonoBehaviour, INetEventListener, IFikaNetworkManager
	{
		public CoopPlayer MyPlayer;
		public int Ping = 0;
		public int ServerFPS = 0;
		public int ReadyClients = 0;
		public bool HostReady = false;
		public bool HostLoaded = false;
		public bool ReconnectDone = false;
		public NetPeer ServerConnection { get; private set; }
		public bool ExfilPointsReceived { get; private set; } = false;
		public NetManager NetClient
		{
			get
			{
				return netClient;
			}
		}
		public NetDataWriter Writer
		{
			get
			{
				return dataWriter;
			}
		}
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
		public FikaClientWorld FikaClientWorld { get; set; }

		private NetPacketProcessor packetProcessor = new();
		private int sendRate;
		private NetManager netClient;
		private CoopHandler coopHandler;
		private readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("Fika.Client");
		private readonly NetDataWriter dataWriter = new();
		private FikaChat fikaChat;
		private string myProfileId;

		public async void Init()
		{
			NetworkGameSession.Rtt = 0;
			NetworkGameSession.LossPercent = 0;

			myProfileId = FikaBackendUtils.Profile.ProfileId;

			packetProcessor.SubscribeNetSerializable<PlayerStatePacket>(OnPlayerStatePacketReceived);
			packetProcessor.SubscribeNetSerializable<WeaponPacket>(OnWeaponPacketReceived);
			packetProcessor.SubscribeNetSerializable<DamagePacket>(OnDamagePacketReceived);
			packetProcessor.SubscribeNetSerializable<ArmorDamagePacket>(OnArmorDamagePacketReceived);
			packetProcessor.SubscribeNetSerializable<InventoryPacket>(OnInventoryPacketReceived);
			packetProcessor.SubscribeNetSerializable<CommonPlayerPacket>(OnCommonPlayerPacketReceived);
			packetProcessor.SubscribeNetSerializable<AllCharacterRequestPacket>(OnAllCharacterRequestPacketReceived);
			packetProcessor.SubscribeNetSerializable<InformationPacket>(OnInformationPacketReceived);
			packetProcessor.SubscribeNetSerializable<HealthSyncPacket>(OnHealthSyncPacketReceived);
			packetProcessor.SubscribeNetSerializable<GenericPacket>(OnGenericPacketReceived);
			packetProcessor.SubscribeNetSerializable<ExfiltrationPacket>(OnExfiltrationPacketReceived);
			packetProcessor.SubscribeNetSerializable<WeatherPacket>(OnWeatherPacketReceived);
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
			packetProcessor.SubscribeNetSerializable<ThrowablePacket>(OnThrowablePacketReceived);
			packetProcessor.SubscribeNetSerializable<WorldLootPacket>(OnWorldLootPacketReceived);
			packetProcessor.SubscribeNetSerializable<ReconnectPacket>(OnReconnectPacketReceived);
			packetProcessor.SubscribeNetSerializable<SyncObjectPacket>(OnSyncObjectPacketReceived);
			packetProcessor.SubscribeNetSerializable<SpawnSyncObjectPacket>(OnSpawnSyncObjectPacketReceived);
			packetProcessor.SubscribeNetSerializable<BTRPacket>(OnBTRPacketReceived);
			packetProcessor.SubscribeNetSerializable<BTRInteractionPacket>(OnBTRInteractionPacketReceived);
			packetProcessor.SubscribeNetSerializable<TraderServicesPacket>(OnTraderServicesPacketReceived);
			packetProcessor.SubscribeNetSerializable<FlareSuccessPacket>(OnFlareSuccessPacketReceived);
			packetProcessor.SubscribeNetSerializable<BufferZonePacket>(OnBufferZonePacketReceived);
			packetProcessor.SubscribeNetSerializable<ResyncInventoryIdPacket>(OnResyncInventoryIdPacketReceived);
			packetProcessor.SubscribeNetSerializable<UsableItemPacket>(OnUsableItemPacketReceived);
			packetProcessor.SubscribeNetSerializable<NetworkSettingsPacket>(OnNetworkSettingsPacketReceived);
			packetProcessor.SubscribeNetSerializable<ArtilleryPacket>(OnArtilleryPacketReceived);
			packetProcessor.SubscribeNetSerializable<SyncTransitControllersPacket>(OnSyncTransitControllersPacketReceived);
			packetProcessor.SubscribeNetSerializable<TransitEventPacket>(OnTransitEventPacketReceived);
			packetProcessor.SubscribeNetSerializable<BotStatePacket>(OnBotStatePacketReceived);
			packetProcessor.SubscribeNetSerializable<PingPacket>(OnPingPacketReceived);
			packetProcessor.SubscribeNetSerializable<LootSyncPacket>(OnLootSyncPacketReceived);
			packetProcessor.SubscribeNetSerializable<LoadingProfilePacket>(OnLoadingProfilePacketReceived);
			packetProcessor.SubscribeNetSerializable<CorpsePositionPacket>(OnCorpsePositionPacketReceived);
			packetProcessor.SubscribeNetSerializable<SideEffectPacket>(OnSideEffectPacketReceived);

#if DEBUG
			AddDebugPackets();
#endif

			netClient = new(this)
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

			await NetManagerUtils.CreateCoopHandler();

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

			FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerCreatedEvent(this));
		}

		private void OnSideEffectPacketReceived(SideEffectPacket packet)
		{
#if DEBUG
			logger.LogWarning("OnSideEffectPacketReceived: Received"); 
#endif
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
				sideEffectComponent.Value = packet.Value;
				item.RaiseRefreshEvent(false, false);
				return;
			}
			logger.LogError("OnSideEffectPacketReceived: SideEffectComponent was not found!");
		}

		private void OnCorpsePositionPacketReceived(CorpsePositionPacket packet)
		{
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			if (gameWorld != null)
			{
				if (gameWorld.ObservedPlayersCorpses.TryGetValue(packet.Data.Id, out ObservedCorpse corpse))
				{
					corpse.ApplyNetPacket(packet.Data);
					return;
				}
			}
		}

		private void OnLoadingProfilePacketReceived(LoadingProfilePacket packet)
		{
			if (packet.Profiles != null)
			{
#if DEBUG
				logger.LogWarning($"OnLoadingProfilePacketReceived: Received {packet.Profiles.Count} profiles");
#endif
				FikaBackendUtils.AddPartyMembers(packet.Profiles);
				return;
			}

			logger.LogWarning("OnLoadingProfilePacketReceived: Profiles was null!");
		}

		private void OnLootSyncPacketReceived(LootSyncPacket packet)
		{
			if (FikaClientWorld != null)
			{
				FikaClientWorld.LootSyncPackets.Add(packet.Data);
			}
		}

		private void OnPingPacketReceived(PingPacket packet)
		{
			if (FikaPlugin.UsePingSystem.Value)
			{
				PingFactory.ReceivePing(packet.PingLocation, packet.PingType, packet.PingColor, packet.Nickname, packet.LocaleId);
			}
		}

		private void OnBotStatePacketReceived(BotStatePacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer bot))
			{
				switch (packet.Type)
				{
					case BotStatePacket.EStateType.DisposeBot:
						{
							if (!bot.gameObject.activeSelf)
							{
								bot.gameObject.SetActive(true);
							}

							if (coopHandler.Players.Remove(packet.NetId))
							{
								bot.Dispose();
								AssetPoolObject.ReturnToPool(bot.gameObject, true);
#if DEBUG
								logger.LogInfo("Disposing bot: " + packet.NetId);
#endif
							}
							else
							{
								logger.LogWarning("Unable to dispose of bot: " + packet.NetId);
							}
						}
						break;
					case BotStatePacket.EStateType.EnableBot:
						{
							if (!bot.gameObject.activeSelf)
							{
#if DEBUG
								logger.LogWarning("Enabling " + packet.NetId);
#endif
								bot.gameObject.SetActive(true);
							}
							else
							{
								logger.LogWarning($"Received packet to enable {bot.ProfileId}, netId {packet.NetId} but the bot was already enabled!");
							}
						}
						break;
					case BotStatePacket.EStateType.DisableBot:
						{
							if (bot.gameObject.activeSelf)
							{
#if DEBUG
								logger.LogWarning("Disabling " + packet.NetId);
#endif
								bot.gameObject.SetActive(false);
							}
							else
							{
								logger.LogWarning($"Received packet to disable {bot.ProfileId}, netId {packet.NetId} but the bot was already disabled!");
							}
						}
						break;
				}
			}
		}

		private void OnTransitEventPacketReceived(TransitEventPacket packet)
		{
			if (!(packet.EventType is TransitEventPacket.ETransitEventType.Init or TransitEventPacket.ETransitEventType.Extract))
			{
				packet.TransitEvent.Invoke();
				return;
			}

			if (packet.EventType is TransitEventPacket.ETransitEventType.Init)
			{
				if (coopHandler.LocalGameInstance.GameWorld_0.TransitController is FikaClientTransitController transitController)
				{
					transitController.Init();
					return;
				}
			}

			if (packet.EventType is TransitEventPacket.ETransitEventType.Extract)
			{
				if (coopHandler.LocalGameInstance.GameWorld_0.TransitController is FikaClientTransitController transitController)
				{
					transitController.HandleClientExtract(packet.TransitId, packet.PlayerId);
					return;
				}
			}

			logger.LogError("OnTransitEventPacketReceived: TransitController was not FikaClientTransitController!");
		}

		private void OnSyncTransitControllersPacketReceived(SyncTransitControllersPacket packet)
		{
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
			packetProcessor.SubscribeNetSerializable<SpawnItemPacket>(OnSpawnItemPacketReceived);
		}

		private void OnSpawnItemPacketReceived(SpawnItemPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				FikaGlobals.SpawnItemInWorld(packet.Item, playerToApply);
			}
		}
		private void OnArtilleryPacketReceived(ArtilleryPacket packet)
		{
			Singleton<GameWorld>.Instance.ClientShellingController.SyncProjectilesStates(ref packet.Data);
		}

		private void OnNetworkSettingsPacketReceived(NetworkSettingsPacket packet)
		{
			logger.LogInfo($"Received settings from server. SendRate: {packet.SendRate}");
			sendRate = packet.SendRate;
		}

		private void OnUsableItemPacketReceived(UsableItemPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.HandleUsableItemPacket(packet);
			}
		}

		private void OnResyncInventoryIdPacketReceived(ResyncInventoryIdPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				if (playerToApply is ObservedCoopPlayer observedPlayer)
				{
					if (observedPlayer.InventoryController is ObservedInventoryController observedController)
					{
						observedController.SetNewID(packet.MongoId.Value);
					}
					return;
				}

				if (playerToApply.IsYourPlayer)
				{
					ResyncInventoryIdPacket response = new(playerToApply.NetId)
					{
						MongoId = playerToApply.InventoryController.CurrentId
					};
					SendData(ref response, DeliveryMethod.ReliableOrdered);
				}
			}
		}

		private void OnBufferZonePacketReceived(BufferZonePacket packet)
		{
			switch (packet.Status)
			{
				case EFT.BufferZone.EBufferZoneData.Availability:
				case EFT.BufferZone.EBufferZoneData.DisableByZryachiyDead:
				case EFT.BufferZone.EBufferZoneData.DisableByPlayerDead:
					{
						BufferZoneControllerClass.Instance.SetInnerZoneAvailabilityStatus(packet.Available, packet.Status);
					}
					break;
				case EFT.BufferZone.EBufferZoneData.PlayerAccessStatus:
					{
						BufferZoneControllerClass.Instance.SetPlayerAccessStatus(packet.ProfileId, packet.Available);
					}
					break;
				case EFT.BufferZone.EBufferZoneData.PlayerInZoneStatusChange:
					{
						BufferZoneControllerClass.Instance.SetPlayerInZoneStatus(packet.ProfileId, packet.Available);
					}
					break;
				default:
					break;
			}
		}

		private void OnFlareSuccessPacketReceived(FlareSuccessPacket packet)
		{
			if (Singleton<GameWorld>.Instance.MainPlayer.ProfileId == packet.ProfileId)
			{
				if (!packet.Success)
				{
					NotificationManagerClass.DisplayNotification(new GClass2269("AirplaneDelayMessage".Localized(null),
								ENotificationDurationType.Default, ENotificationIconType.Default, null));
				}
			}
		}

		private void OnTraderServicesPacketReceived(TraderServicesPacket packet)
		{
			if (packet.Services.Count < 1)
			{
				logger.LogWarning("OnTraderServicesPacketReceived: Services was 0, but might be intentional. Skipping...");
				return;
			}

			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.method_145(packet.Services);
			}
		}

		private void OnBTRInteractionPacketReceived(BTRInteractionPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				GameWorld gameWorld = Singleton<GameWorld>.Instance;
				if (gameWorld.BtrController != null && gameWorld.BtrController.BtrView != null)
				{
					if (packet.IsResponse && packet.Status != EBtrInteractionStatus.Confirmed)
					{
						if (packet.Status - EBtrInteractionStatus.Blacklisted <= 2 && playerToApply.IsYourPlayer)
						{
							GlobalEventHandlerClass.CreateEvent<BtrNotificationInteractionMessageEvent>().Invoke(playerToApply.PlayerId, packet.Status);
							return;
						}
					}
					gameWorld.BtrController.BtrView.Interaction(playerToApply, packet.Data);
				}
			}
		}

		private void OnSpawnSyncObjectPacketReceived(SpawnSyncObjectPacket packet)
		{
			GClass2400 processor = Singleton<GameWorld>.Instance.SynchronizableObjectLogicProcessor;
			if (processor == null)
			{
				return;
			}

			switch (packet.ObjectType)
			{
				case SynchronizableObjectType.AirDrop:
					{
						AirdropSynchronizableObject syncObject = (AirdropSynchronizableObject)processor.TakeFromPool(SynchronizableObjectType.AirDrop);
						syncObject.ObjectId = packet.ObjectId;
						syncObject.AirdropType = packet.AirdropType;
						LootableContainer container = syncObject.GetComponentInChildren<LootableContainer>().gameObject.GetComponentInChildren<LootableContainer>();
						container.enabled = true;
						container.Id = packet.ContainerId;
						if (packet.NetId > 0)
						{
							container.NetId = packet.NetId;
						}
						Singleton<GameWorld>.Instance.RegisterWorldInteractionObject(container);
						LootItem.CreateLootContainer(container, packet.AirdropItem, packet.AirdropItem.ShortName.Localized(null),
								Singleton<GameWorld>.Instance, null);
						if (!syncObject.IsStatic)
						{
							processor.InitSyncObject(syncObject, syncObject.transform.position, syncObject.transform.rotation.eulerAngles, syncObject.ObjectId);
							return;
						}
						processor.InitStaticObject(syncObject);
					}
					break;
				case SynchronizableObjectType.AirPlane:
					{
						AirplaneSynchronizableObject syncObject = (AirplaneSynchronizableObject)processor.TakeFromPool(SynchronizableObjectType.AirPlane);
						syncObject.ObjectId = packet.ObjectId;
						syncObject.transform.SetPositionAndRotation(packet.Position, packet.Rotation);
						processor.InitSyncObject(syncObject, packet.Position, packet.Rotation.eulerAngles, packet.ObjectId);
					}
					break;
				case SynchronizableObjectType.Tripwire:
					{
						if (Singleton<ItemFactoryClass>.Instance.CreateItem(packet.GrenadeId, packet.GrenadeTemplate, null) is not ThrowWeapItemClass grenadeClass)
						{
							logger.LogError("OnSpawnSyncObjectPacketReceived: Item with id " + packet.GrenadeId + " is not a grenade!");
							return;
						}

						TripwireSynchronizableObject syncObject = (TripwireSynchronizableObject)processor.TakeFromPool(packet.ObjectType);
						syncObject.ObjectId = packet.ObjectId;
						syncObject.IsStatic = packet.IsStatic;
						syncObject.transform.SetPositionAndRotation(packet.Position, packet.Rotation);
						processor.InitSyncObject(syncObject, syncObject.transform.position, syncObject.transform.rotation.eulerAngles, syncObject.ObjectId);

						syncObject.SetupGrenade(grenadeClass, packet.ProfileId, packet.Position, packet.ToPosition);
						//processor.TripwireManager.AddTripwire(syncObject);
					}
					break;
				default:
					break;
			}
		}

		private void OnSyncObjectPacketReceived(SyncObjectPacket packet)
		{
			CoopClientGameWorld gameWorld = (CoopClientGameWorld)Singleton<GameWorld>.Instance;
			List<AirplaneDataPacketStruct> structs = [packet.Data];
			gameWorld.ClientSynchronizableObjectLogicProcessor?.ProcessSyncObjectPackets(structs);
		}

		private void OnReconnectPacketReceived(ReconnectPacket packet)
		{
			if (!packet.IsRequest)
			{
				CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
				if (coopGame == null)
				{
					return;
				}

				switch (packet.Type)
				{
					case ReconnectPacket.EReconnectDataType.Throwable:
						if (packet.ThrowableData != null)
						{
#if DEBUG
							logger.LogWarning("Received reconnect packet for throwables: " + packet.ThrowableData.Count);
#endif
							string localizedString = LocaleUtils.UI_SYNC_THROWABLES.Localized();
							coopGame.SetMatchmakerStatus(localizedString);
							Singleton<GameWorld>.Instance.OnSmokeGrenadesDeserialized(packet.ThrowableData);
						}
						break;
					case ReconnectPacket.EReconnectDataType.Interactives:
						{
							if (packet.InteractivesData != null)
							{
#if DEBUG
								logger.LogWarning("Received reconnect packet for interactives: " + packet.InteractivesData.Count);
#endif
								string localizedString = LocaleUtils.UI_SYNC_INTERACTABLES.Localized();
								WorldInteractiveObject[] worldInteractiveObjects = Traverse.Create(Singleton<GameWorld>.Instance.World_0).Field<WorldInteractiveObject[]>("worldInteractiveObject_0").Value;
								Dictionary<int, WorldInteractiveObject.GStruct415> netIdDictionary = [];
								{
									foreach (WorldInteractiveObject.GStruct415 data in packet.InteractivesData)
									{
										netIdDictionary.Add(data.NetId, data);
									}
								}

								float total = packet.InteractivesData.Count;
								float progress = 0f;
								foreach (WorldInteractiveObject item in worldInteractiveObjects)
								{
									if (netIdDictionary.TryGetValue(item.NetId, out WorldInteractiveObject.GStruct415 value))
									{
										progress++;
										coopGame.SetMatchmakerStatus(localizedString, progress / total);
										item.SetInitialSyncState(value);
									}
								}
							}
							break;
						}
					case ReconnectPacket.EReconnectDataType.LampControllers:
						{
							if (packet.LampStates != null)
							{
#if DEBUG
								logger.LogWarning("Received reconnect packet for lampStates: " + packet.LampStates.Count);
#endif
								string localizedString = LocaleUtils.UI_SYNC_LAMP_STATES.Localized();
								Dictionary<int, LampController> lampControllerDictionary = LocationScene.GetAllObjects<LampController>(true)
														.Where(FikaGlobals.LampControllerNetIdNot0)
														.ToDictionary(FikaGlobals.LampControllerGetNetId);

								float total = packet.LampStates.Count;
								float progress = 0f;
								foreach (KeyValuePair<int, byte> lampState in packet.LampStates)
								{
									progress++;
									coopGame.SetMatchmakerStatus(localizedString, progress / total);
									if (lampControllerDictionary.TryGetValue(lampState.Key, out LampController lampController))
									{
										if (lampController.LampState != (Turnable.EState)lampState.Value)
										{
											lampController.Switch((Turnable.EState)lampState.Value);
										}
									}
								}
							}
							break;
						}
					case ReconnectPacket.EReconnectDataType.Windows:
						{
#if DEBUG
							logger.LogWarning("Received reconnect packet for windowBreakers: " + packet.WindowBreakerStates.Count);
#endif
							if (packet.WindowBreakerStates != null)
							{
								Dictionary<int, Vector3> windowBreakerStates = packet.WindowBreakerStates;
								string localizedString = LocaleUtils.UI_SYNC_WINDOWS.Localized();

								float total = packet.WindowBreakerStates.Count;
								float progress = 0f;
								foreach (WindowBreaker windowBreaker in LocationScene.GetAllObjects<WindowBreaker>(true)
									.Where(FikaGlobals.WindowBreakerAvailableToSync))
								{
									if (windowBreakerStates.TryGetValue(windowBreaker.NetId, out Vector3 hitPosition))
									{
										progress++;

										coopGame.SetMatchmakerStatus(localizedString, progress / total);
										try
										{
											DamageInfoStruct damageInfo = default;
											damageInfo.HitPoint = hitPosition;
											windowBreaker.MakeHit(in damageInfo, true);
										}
										catch (Exception ex)
										{
											logger.LogError("OnReconnectPacketReceived: Exception caught while setting up WindowBreakers: " + ex.Message);
										}
									}
								}
							}
							break;
						}
					case ReconnectPacket.EReconnectDataType.OwnCharacter:
#if DEBUG
						logger.LogWarning("Received reconnect packet for own player");
#endif
						coopGame.SetMatchmakerStatus(LocaleUtils.UI_RECEIVE_OWN_PLAYERS.Localized());
						coopHandler.LocalGameInstance.Profile_0 = packet.Profile;
						coopHandler.LocalGameInstance.Profile_0.Health = packet.ProfileHealthClass;
						FikaBackendUtils.ReconnectPosition = packet.PlayerPosition;
						NetworkTimeSync.Start(packet.TimeOffset);
						break;
					case ReconnectPacket.EReconnectDataType.Finished:
						coopGame.SetMatchmakerStatus(LocaleUtils.UI_FINISH_RECONNECT.Localized());
						ReconnectDone = true;
						break;
					default:
						break;
				}
			}
		}

		private void OnWorldLootPacketReceived(WorldLootPacket packet)
		{
			if (Singleton<IFikaGame>.Instance != null && Singleton<IFikaGame>.Instance is CoopGame coopGame)
			{
				GClass1315 lootItems = SimpleZlib.Decompress(packet.Data).ParseJsonTo<GClass1315>();
				if (lootItems.Count < 1)
				{
					throw new NullReferenceException("LootItems length was less than 1! Something probably went very wrong");
				}
				coopGame.LootItems = lootItems;
				coopGame.HasReceivedLoot = true;
			}
		}

		private void OnThrowablePacketReceived(ThrowablePacket packet)
		{
			GClass786<int, Throwable> grenades = Singleton<GameWorld>.Instance.Grenades;
			foreach (GStruct131 grenadeData in packet.Data)
			{
				if (grenades.TryGetByKey(grenadeData.Id, out Throwable throwable))
				{
					throwable.ApplyNetPacket(grenadeData);
				}
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
					world.method_0(packet.Interactables);
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
			MyPlayer = coopPlayer;
			if (FikaPlugin.EnableChat.Value)
			{
				fikaChat = gameObject.AddComponent<FikaChat>();
			}
		}

		private void OnOperationCallbackPacketReceived(OperationCallbackPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player) && player.IsYourPlayer)
			{
				player.HandleCallbackFromServer(packet);
			}
		}

		private void OnSyncNetIdPacketReceived(SyncNetIdPacket packet)
		{
			Dictionary<int, CoopPlayer> newPlayers = coopHandler.Players;
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer player))
			{
				if (player.ProfileId != packet.ProfileId)
				{
					FikaPlugin.Instance.FikaLogger.LogWarning($"OnSyncNetIdPacketReceived: {packet.ProfileId} had the wrong NetId: {coopHandler.Players[packet.NetId].NetId}, should be {packet.NetId}");
					for (int i = 0; i < coopHandler.Players.Count; i++)
					{
						KeyValuePair<int, CoopPlayer> playerToReorganize = coopHandler.Players.Where(x => x.Value.ProfileId == packet.ProfileId).First();
						coopHandler.Players.Remove(playerToReorganize.Key);
						coopHandler.Players[packet.NetId] = playerToReorganize.Value;
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
			MyPlayer.PlayerId = packet.NetId;
			int i = -1;
			foreach (KeyValuePair<int, CoopPlayer> player in coopHandler.Players)
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

			coopHandler.Players.Remove(i);
			coopHandler.Players[packet.NetId] = MyPlayer;
		}

		private void OnSendCharacterPacketReceived(SendCharacterPacket packet)
		{
			if (coopHandler == null)
			{
				return;
			}

			if (packet.PlayerInfoPacket.Profile.ProfileId != myProfileId)
			{
				coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
							 packet.PlayerInfoPacket.ControllerId.Value, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
							 packet.PlayerInfoPacket.ControllerType, packet.PlayerInfoPacket.ItemId);
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
			NetworkGame<EftGamePlayerOwner>.Class1513 mineSeeker = new()
			{
				minePosition = packet.MinePositon
			};
			MineDirectional mineDirectional = MineDirectional.Mines.FirstOrDefault(mineSeeker.method_0);
			if (mineDirectional == null)
			{
				logger.LogError($"OnMinePacketReceived: Could not find mine at position {packet.MinePositon}");
				return;
			}
			mineDirectional.Explosion();
		}

		private void OnWeatherPacketReceived(WeatherPacket packet)
		{
			if (!packet.IsRequest)
			{
				if (CoopHandler.LocalGameInstance != null)
				{
					CoopHandler.LocalGameInstance.WeatherClasses = packet.WeatherClasses;
					CoopHandler.LocalGameInstance.Season = packet.Season;
					CoopHandler.LocalGameInstance.SeasonsSettings = new()
					{
						SpringSnowFactor = packet.SpringSnowFactor
					};
					return;
				}

				logger.LogError("OnWeatherPacketReceived: LocalGameInstance was null!");
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
					{
						return;
					}

					CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
					if (coopGame == null)
					{
#if DEBUG
						logger.LogError("OnExfiltrationPacketReceived: coopGame was null!");
#endif
						return;
					}

					CarExtraction carExtraction = FindObjectOfType<CarExtraction>();

					int index = 0;
					foreach (KeyValuePair<string, EExfiltrationStatus> exfilPoint in packet.ExfiltrationPoints)
					{
						ExfiltrationPoint point = exfilController.ExfiltrationPoints.Where(x => x.Settings.Name == exfilPoint.Key).FirstOrDefault();
						if (point != null || point != default)
						{
							point.Settings.StartTime = packet.StartTimes[index];
							index++;
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
						}
						else
						{
							logger.LogWarning($"ExfiltrationPacketPacketReceived::ExfiltrationPoints: Could not find exfil point with name '{exfilPoint.Key}'");
						}
					}

					if (coopGame.RaidSettings.Side == ESideType.Savage && exfilController.ScavExfiltrationPoints != null && packet.HasScavExfils)
					{
						int scavIndex = 0;
						foreach (KeyValuePair<string, EExfiltrationStatus> scavExfilPoint in packet.ScavExfiltrationPoints)
						{
							ScavExfiltrationPoint scavPoint = exfilController.ScavExfiltrationPoints.Where(x => x.Settings.Name == scavExfilPoint.Key).FirstOrDefault();
							if (scavPoint != null || scavPoint != default)
							{
								scavPoint.Settings.StartTime = packet.ScavStartTimes[scavIndex];
								scavIndex++;
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
					}

					ExfilPointsReceived = true;
				}
				else
				{
					logger.LogWarning($"ExfiltrationPacketPacketReceived: ExfiltrationController was null");
				}
			}
		}

		private void OnGenericPacketReceived(GenericPacket packet)
		{
			packet.SubPacket.Execute(null);
		}

		private void OnHealthSyncPacketReceived(HealthSyncPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.HealthSyncPackets.Enqueue(packet);
			}
		}

		private void OnInformationPacketReceived(InformationPacket packet)
		{
			CoopGame coopGame = CoopHandler.LocalGameInstance;
			if (coopGame != null)
			{
				coopGame.RaidStarted = packet.RaidStarted;
			}
			ReadyClients = packet.ReadyPlayers;
			HostReady = packet.HostReady;
			HostLoaded = packet.HostLoaded;
			if (packet.AmountOfPeers > 0)
			{
				FikaBackendUtils.HostExpectedNumberOfPlayers = packet.AmountOfPeers;
			}

			if (packet.HostReady)
			{
				coopGame.SetClientTime(packet.GameTime, packet.SessionTime);
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
#if DEBUG
				logger.LogInfo($"Received CharacterRequest! ProfileID: {packet.PlayerInfoPacket.Profile.ProfileId}, Nickname: {packet.PlayerInfoPacket.Profile.Nickname}");
#endif
				if (packet.ProfileId != MyPlayer.ProfileId)
				{
					coopHandler.QueueProfile(packet.PlayerInfoPacket.Profile, packet.PlayerInfoPacket.HealthByteArray, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI,
						packet.PlayerInfoPacket.ControllerId.Value, packet.PlayerInfoPacket.FirstOperationId, packet.PlayerInfoPacket.IsZombie,
						packet.PlayerInfoPacket.ControllerType, packet.PlayerInfoPacket.ItemId);
				}
			}
			else if (packet.IsRequest)
			{
#if DEBUG
				logger.LogInfo($"Received CharacterRequest from server, send my Profile.");
#endif
				AllCharacterRequestPacket requestPacket = new(MyPlayer.ProfileId)
				{
					IsRequest = false,
					PlayerInfoPacket = new()
					{
						Profile = MyPlayer.Profile,
						ControllerId = MyPlayer.InventoryController.CurrentId,
						FirstOperationId = MyPlayer.InventoryController.NextOperationId
					},
					IsAlive = MyPlayer.ActiveHealthController.IsAlive,
					IsAI = MyPlayer.IsAI,
					Position = MyPlayer.Transform.position,
					NetId = MyPlayer.NetId
				};

				if (MyPlayer.ActiveHealthController != null)
				{
					requestPacket.PlayerInfoPacket.HealthByteArray = MyPlayer.ActiveHealthController.SerializeState();
				}

				if (MyPlayer.HandsController != null)
				{
					requestPacket.PlayerInfoPacket.ControllerType = GClass1808.FromController(MyPlayer.HandsController);
					requestPacket.PlayerInfoPacket.ItemId = MyPlayer.HandsController.Item.Id;
					requestPacket.PlayerInfoPacket.IsStationary = MyPlayer.MovementContext.IsStationaryWeaponInHands;
				}

				SendData(ref requestPacket, DeliveryMethod.ReliableOrdered);
			}
		}

		private void OnCommonPlayerPacketReceived(CommonPlayerPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.CommonPlayerPackets.Enqueue(packet);
			}
		}

		private void OnInventoryPacketReceived(InventoryPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.InventoryPackets.Enqueue(packet);
			}
		}

		private void OnDamagePacketReceived(DamagePacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply) && playerToApply.IsYourPlayer)
			{
				playerToApply.PacketReceiver.DamagePackets.Enqueue(packet);
			}
		}

		private void OnArmorDamagePacketReceived(ArmorDamagePacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.ArmorDamagePackets.Enqueue(packet);
			}
		}

		private void OnWeaponPacketReceived(WeaponPacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.PacketReceiver.FirearmPackets.Enqueue(packet);
			}
		}

		private void OnHalloweenEventPacketReceived(HalloweenEventPacket packet)
		{
			HalloweenEventControllerClass controller = HalloweenEventControllerClass.Instance;

			if (controller == null)
			{
				logger.LogError("OnHalloweenEventPacketReceived: controller was null!");
				return;
			}

			if (packet.SyncEvent == null)
			{
				logger.LogError("OnHalloweenEventPacketReceived: event was null!");
				return;
			}

			packet.SyncEvent.Invoke();
		}

		private void OnBTRPacketReceived(BTRPacket packet)
		{
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			if (gameWorld != null)
			{
				BTRControllerClass btrController = gameWorld.BtrController;
				if (btrController != null)
				{
					btrController.SyncBTRVehicleFromServer(packet.Data);
				}
			}
		}

		private void OnPlayerStatePacketReceived(PlayerStatePacket packet)
		{
			if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				playerToApply.Snapshotter.Insert(packet);
			}
		}

		protected void Update()
		{
			netClient?.PollEvents();
		}

		protected void OnDestroy()
		{
			netClient?.Stop();

			if (fikaChat != null)
			{
				Destroy(fikaChat);
			}

			FikaEventDispatcher.DispatchEvent(new FikaNetworkManagerDestroyedEvent(this));
		}

		public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
		{
			dataWriter.Reset();
			packetProcessor.WriteNetSerializable(dataWriter, ref packet);
			netClient.FirstPeer.Send(dataWriter, deliveryMethod);
		}

		public void OnPeerConnected(NetPeer peer)
		{
			NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.CONNECTED_TO_SERVER.Localized(), peer.Port),
				ENotificationDurationType.Default, ENotificationIconType.Friend);

			if (FikaBackendUtils.Profile == null)
			{
				logger.LogError("OnPeerConnected: Own profile was null!");
				return;
			}

			Dictionary<Profile, bool> profiles = [];
			profiles.Add(FikaBackendUtils.Profile, false);
			LoadingProfilePacket profilePacket = new()
			{
				Profiles = profiles
			};
			SendData(ref profilePacket, DeliveryMethod.ReliableOrdered);
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
			NetworkGameSession.Rtt = peer.RoundTripTime;
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
				NotificationManagerClass.DisplayWarningNotification(LocaleUtils.LOST_CONNECTION.Localized());
				Destroy(MyPlayer.PacketReceiver);
				MyPlayer.PacketSender.DestroyThis();
				Destroy(this);
				Singleton<FikaClient>.Release(this);
			}
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
			logger.LogInfo("..:: Fika Client Session Statistics ::..");
			logger.LogInfo($"Sent packets: {netClient.Statistics.PacketsSent}");
			logger.LogInfo($"Sent data: {FikaGlobals.FormatFileSize(netClient.Statistics.BytesSent)}");
			logger.LogInfo($"Received packets: {netClient.Statistics.PacketsReceived}");
			logger.LogInfo($"Received data: {FikaGlobals.FormatFileSize(netClient.Statistics.BytesReceived)}");
			logger.LogInfo($"Packet loss: {netClient.Statistics.PacketLossPercent}%");
		}
	}
}
