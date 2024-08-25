// © 2024 Lacyway All Rights Reserved

using BepInEx.Logging;
using Comfort.Common;
using ComponentAce.Compression.Libs.zlib;
using Coop.Airdrops;
using EFT;
using EFT.AssetsManager;
using EFT.Interactive;
using EFT.MovingPlatforms;
using EFT.UI;
using EFT.UI.BattleTimer;
using EFT.Weather;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Custom;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Lighthouse;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking.Packets.GameWorld;
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
		public bool HostLoaded = false;
		public bool ReconnectDone = false;
		public NetManager NetClient
		{
			get
			{
				return netClient;
			}
		}
		public NetPeer ServerConnection { get; private set; }
		public bool ExfilPointsReceived { get; private set; } = false;
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
		private string myProfileId;

		public async void Init()
		{
			NetworkGameSession.RTT = 0;
			NetworkGameSession.LossPercent = 0;

			myProfileId = FikaBackendUtils.Profile.ProfileId;

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
			packetProcessor.SubscribeNetSerializable<WorldLootPacket>(OnWorldLootPacketReceived);
			packetProcessor.SubscribeNetSerializable<ReconnectPacket>(OnReconnectPacketReceived);
			packetProcessor.SubscribeNetSerializable<LightkeeperGuardDeathPacket>(OnLightkeeperGuardDeathPacketReceived);
			packetProcessor.SubscribeNetSerializable<ResyncInventoryPacket>(OnResyncInventoryPacketReceived);

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

			await NetManagerUtils.CreateCoopHandler();
			coopHandler = CoopHandler.CoopHandlerParent.GetComponent<CoopHandler>();

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

		private void OnResyncInventoryPacketReceived(ResyncInventoryPacket packet)
		{
			if (Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
			{
				if (playerToApply is ObservedCoopPlayer observedPlayer)
				{
					if (observedPlayer.InventoryControllerClass is ObservedInventoryController observedController)
					{
						observedController.SetNewID(new(packet.MongoId));
					}
					return;
				}
				if (playerToApply.IsYourPlayer)
				{
					ResyncInventoryPacket response = new(playerToApply.NetId)
					{
						MongoId = playerToApply.InventoryControllerClass.CurrentId.ToString()
					};
					SendData(ref response, DeliveryMethod.ReliableOrdered);
				}
			}
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
								Dictionary<int, WorldInteractiveObject.GStruct384> netIdDictionary = [];
								{
									foreach (WorldInteractiveObject.GStruct384 data in packet.InteractivesData)
									{
										netIdDictionary.Add(data.NetId, data);
									}
								}

								float total = packet.InteractivesData.Count;
								float progress = 0f;
								foreach (WorldInteractiveObject item in worldInteractiveObjects)
								{
									if (netIdDictionary.TryGetValue(item.NetId, out WorldInteractiveObject.GStruct384 value))
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
														.Where(new Func<LampController, bool>(ClientWorld.Class1231.class1231_0.method_0))
														.ToDictionary(new Func<LampController, int>(ClientWorld.Class1231.class1231_0.method_1));

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
									.Where(new Func<WindowBreaker, bool>(ClientWorld.Class1231.class1231_0.method_2)))
								{
									if (windowBreakerStates.TryGetValue(windowBreaker.NetId, out Vector3 hitPosition))
									{
										progress++;

										coopGame.SetMatchmakerStatus(localizedString, progress / total);
										try
										{
											DamageInfo damageInfo = default;
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
				GClass1211 lootItems = SimpleZlib.Decompress(packet.Data).ParseJsonTo<GClass1211>();
				coopGame.LootItems = lootItems;
				coopGame.HasReceivedLoot = true;
			}
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
			MyPlayer.PlayerId = packet.NetId;
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

			if (packet.PlayerInfo.Profile.ProfileId != myProfileId)
			{
				coopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.netId, packet.IsAlive, packet.IsAI, packet.PlayerInfo.ControllerId.Value);
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
									NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.GROUP_MEMBER_EXTRACTED.Localized(),
										ColorizeText(Colors.GREEN, nickname)),
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
						MovingPlatform.GClass2952 adapter = Singleton<GameWorld>.Instance.PlatformAdapters[0];
						if (adapter != null)
						{
							GStruct129 data = new()
							{
								Id = packet.PlatformId,
								Position = packet.PlatformPosition
							};
							adapter.StoreNetPacket(data);
							adapter.ApplyStoredPackets();
						}
						/*Locomotive locomotive = FindObjectOfType<Locomotive>();
						if (locomotive != null)
						{
							DateTime depart = new(packet.DepartureTime);
							Traverse.Create(locomotive).Field("_depart").SetValue(depart);
						}
						else
						{
							logger.LogWarning("GenericPacketReceived: Could not find locomotive!");
						}*/
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
				case EPackageType.ClearEffects:
					{
						if (coopHandler.Players.TryGetValue(packet.NetId, out CoopPlayer playerToApply))
						{
							if (playerToApply is ObservedCoopPlayer observedPlayer)
							{
								observedPlayer.HealthBar.ClearEffects();
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
				HostLoaded = packet.HostLoaded;

				if (packet.HostReady)
				{
					CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
					coopGame.SetClientTime(packet.GameTime, packet.SessionTime);
				}
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
					coopHandler.QueueProfile(packet.PlayerInfo.Profile, packet.Position, packet.NetId, packet.IsAlive, packet.IsAI, packet.PlayerInfo.ControllerId.Value);
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

				SendData(ref requestPacket, DeliveryMethod.ReliableOrdered);
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
				TimeSpan timeRemain = gameTimer.PastTime + sessionTime;

				gameTimer.ChangeSessionTime(timeRemain);

				Traverse timerPanel = Traverse.Create(coopGame.GameUi.TimerPanel);
				timerPanel.Field("dateTime_0").SetValue(gameTimer.StartDateTime.Value.AddSeconds(timeRemain.TotalSeconds));

				MainTimerPanel mainTimerPanel = timerPanel.Field<MainTimerPanel>("_mainTimerPanel").Value;
				if (mainTimerPanel != null)
				{
					Traverse.Create(mainTimerPanel).Field<DateTime>("dateTime_0").Value = gameTimer.StartDateTime.Value.AddSeconds(timeRemain.TotalSeconds);
					mainTimerPanel.UpdateTimer();
				}

				Traverse.Create(gameTimer).Field<DateTime?>("nullable_0").Value = new DateTime(packet.StartTime);
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

		private void OnLightkeeperGuardDeathPacketReceived(LightkeeperGuardDeathPacket packet)
		{
			FikaLighthouseProgressionClass Component = Singleton<GameWorld>.Instance.gameObject.GetComponent<FikaLighthouseProgressionClass>();

			if (Component != null)
			{
				Component.HandlePacket(packet);
			}
			else
			{
				logger.LogError("OnLightkeeperGuardDeathPacketReceived: Received packet but manager is not initialized");
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


		[Obsolete("SendData with a NetDataWriter specified is deprecated and will be removed in newer versions of Fika, please use SendData without a writer.")]
		public void SendData<T>(NetDataWriter writer, ref T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
		{
			packetProcessor.WriteNetSerializable(writer, ref packet);
			netClient.FirstPeer.Send(writer, deliveryMethod);
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
				NotificationManagerClass.DisplayWarningNotification(LocaleUtils.LOST_CONNECTION.Localized());
				Destroy(MyPlayer.PacketReceiver);
				MyPlayer.PacketSender.DestroyThis();
				Destroy(this);
				Singleton<FikaClient>.Release(this);
			}
		}
	}
}
