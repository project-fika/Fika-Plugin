﻿using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.GlobalEvents;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.Vehicle;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using GPUInstancer;
using HarmonyLib;
using LiteNetLib;
using SPT.Custom.BTR;
using SPT.Custom.BTR.Utils;
using SPT.SinglePlayer.Utils.TraderServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fika.Core.Coop.BTR
{
	/// <summary>
	/// Based on <see href="https://dev.sp-tarkov.com/SPT/Modules/src/branch/master/project/SPT.Custom/BTR/BTRManager.cs"/>
	/// </summary>
	internal class FikaBTRManager_Host : MonoBehaviour
	{
		private GameWorld gameWorld;
		private BotEventHandler botEventHandler;

		private BotBTRService btrBotService;
		private BTRControllerClass btrController;
		private BTRVehicle btrServerSide;
		private BTRView btrClientSide;
		private BotOwner btrBotShooter;
		private BTRDataPacket btrDataPacket = default;
#pragma warning disable CS0414 // The field 'FikaBTRManager_Host.btrInitialized' is assigned but its value is never used
		private bool btrInitialized = false;
#pragma warning restore CS0414 // The field 'FikaBTRManager_Host.btrInitialized' is assigned but its value is never used
		private bool btrBotShooterInitialized = false;

		private float coverFireTime = 90f;
		private Coroutine _coverFireTimerCoroutine;

		private BTRSide lastInteractedBtrSide;
		public BTRSide LastInteractedBtrSide => lastInteractedBtrSide;

		private Coroutine _shootingTargetCoroutine;
		private BTRTurretServer btrTurretServer;
		private bool isTurretInDefaultRotation;
		private EnemyInfo currentTarget = null;
		private bool isShooting = false;
		private float machineGunAimDelay = 0.4f;
		private Vector2 machineGunBurstCount;
		private Vector2 machineGunRecoveryTime;
		private BulletClass btrMachineGunAmmo;
		private Item btrMachineGunWeapon;
		private Player.FirearmController firearmController;
		private WeaponSoundPlayer weaponSoundPlayer;

		private MethodInfo _updateTaxiPriceMethod;

		private float originalDamageCoeff;

		private FikaServer server;
		Queue<KeyValuePair<Vector3, Vector3>> shotQueue = new(20);
		private Player lastInteractPlayer = null;
		private ManualLogSource btrLogger;

		FikaBTRManager_Host()
		{
			Type btrControllerType = typeof(BTRControllerClass);
			_updateTaxiPriceMethod = AccessTools.GetDeclaredMethods(btrControllerType).Single(IsUpdateTaxiPriceMethod);
			server = Singleton<FikaServer>.Instance;
			btrLogger = BepInEx.Logging.Logger.CreateLogSource("BTR Host");
		}

		public bool CanPlayerEnter(IPlayer player)
		{
			if (btrBotShooter.BotsGroup.Enemies.ContainsKey(player))
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		private async void Awake()
		{
			try
			{
				gameWorld = Singleton<GameWorld>.Instance;
				if (gameWorld == null)
				{
					Destroy(this);
					return;
				}

				if (gameWorld.BtrController == null)
				{
					gameWorld.BtrController = new BTRControllerClass();
				}

				btrController = gameWorld.BtrController;

				await InitBtr();
			}
			catch
			{
				btrLogger.LogError("[SPT-BTR] Unable to spawn BTR. Check logs.");
				Destroy(this);
				throw;
			}
		}

		private IEnumerator SendBotProfileId()
		{
			while (!Singleton<GameWorld>.Instantiated)
			{
				yield return null;
			}

			while (string.IsNullOrEmpty(Singleton<GameWorld>.Instance.MainPlayer.Location))
			{
				yield return null;
			}

			CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;

			while (coopGame.Status != GameStatus.Started && btrController.BotShooterBtr == null)
			{
				yield return null;
			}

			yield return new WaitForSeconds(20);

			BTRPacket packet = new()
			{
				BTRDataPacket = btrDataPacket,
				HasBotProfileId = true,
				BotNetId = ((CoopPlayer)btrBotShooter.GetPlayer).NetId
			};

			server.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
		}

		public void OnPlayerInteractDoor(Player player, PlayerInteractPacket interactPacket)
		{
			bool playerGoIn = interactPacket.InteractionType == EInteractionType.GoIn;
			bool playerGoOut = interactPacket.InteractionType == EInteractionType.GoOut;

			lastInteractPlayer = player;
			player.BtrInteractionSide = btrClientSide.method_9(interactPacket.SideId);
			lastInteractedBtrSide = player.BtrInteractionSide;

			if (!player.IsYourPlayer)
			{
				HandleBtrDoorState(player.BtrState);
			}

			if (interactPacket.SideId == 0 && playerGoIn)
			{
				if (interactPacket.SlotId == 0)
				{
					btrServerSide.LeftSlot0State = 1;
				}
				else if (interactPacket.SlotId == 1)
				{
					btrServerSide.LeftSlot1State = 1;
				}
			}
			else if (interactPacket.SideId == 0 && playerGoOut)
			{
				if (interactPacket.SlotId == 0)
				{
					btrServerSide.LeftSlot0State = 0;
				}
				else if (interactPacket.SlotId == 1)
				{
					btrServerSide.LeftSlot1State = 0;
				}
			}
			else if (interactPacket.SideId == 1 && playerGoIn)
			{
				if (interactPacket.SlotId == 0)
				{
					btrServerSide.RightSlot0State = 1;
				}
				else if (interactPacket.SlotId == 1)
				{
					btrServerSide.RightSlot1State = 1;
				}
			}
			else if (interactPacket.SideId == 1 && playerGoOut)
			{
				if (interactPacket.SlotId == 0)
				{
					btrServerSide.RightSlot0State = 0;
				}
				else if (interactPacket.SlotId == 1)
				{
					btrServerSide.RightSlot1State = 0;
				}
			}

			// If the player is going into the BTR, store their damage coefficient
			// and set it to 0, so they don't die while inside the BTR
			if (interactPacket.InteractionType == EInteractionType.GoIn && player.IsYourPlayer)
			{
				originalDamageCoeff = player.ActiveHealthController.DamageCoeff;
				player.ActiveHealthController.SetDamageCoeff(0f);

			}
			// Otherwise restore the damage coefficient
			else if (interactPacket.InteractionType == EInteractionType.GoOut && player.IsYourPlayer)
			{
				player.ActiveHealthController.SetDamageCoeff(originalDamageCoeff);
			}
		}

		// Find `BTRControllerClass.method_9(PathDestination currentDestinationPoint, bool lastRoutePoint)`
		private bool IsUpdateTaxiPriceMethod(MethodInfo method)
		{
			return method.GetParameters().Length == 2 && method.GetParameters()[0].ParameterType == typeof(PathDestination);
		}

		private void Update()
		{
			btrController.SyncBTRVehicleFromServer(UpdateDataPacket());

			if (btrController.BotShooterBtr == null) return;

			// BotShooterBtr doesn't get assigned to BtrController immediately so we check this in Update
			if (!btrBotShooterInitialized)
			{
				InitBtrBotService();
				btrBotShooterInitialized = true;
			}

			UpdateTarget();

			if (HasTarget())
			{
				SetAim();

				if (!isShooting && CanShoot())
				{
					StartShooting();
				}
			}
			else if (!isTurretInDefaultRotation)
			{
				btrTurretServer.DisableAiming();
			}
		}

		private async Task InitBtr()
		{
			// Initial setup
			await btrController.InitBtrController();

			botEventHandler = Singleton<BotEventHandler>.Instance;
			BotsController botsController = Singleton<IBotGame>.Instance.BotsController;
			btrBotService = botsController.BotTradersServices.BTRServices;
			btrController.method_3(); // spawns server-side BTR game object
			botsController.BotSpawner.SpawnBotBTR(); // spawns the scav bot which controls the BTR's turret

			// Initial BTR configuration
			btrServerSide = btrController.BtrVehicle;
			btrClientSide = btrController.BtrView;
			btrServerSide.transform.Find("KillBox").gameObject.AddComponent<BTRRoadKillTrigger>();

			// Get config from server and initialise respective settings
			ConfigureSettingsFromServer();

			MapPathConfig btrMapConfig = btrController.MapPathsConfiguration;
			if (btrMapConfig == null)
			{
				ConsoleScreen.LogError($"{nameof(btrController.MapPathsConfiguration)}");
				return;
			}
			btrServerSide.CurrentPathConfig = btrMapConfig.PathsConfiguration.pathsConfigurations.RandomElement();
			btrServerSide.Initialization(btrMapConfig);
			btrController.method_14(); // creates and assigns the BTR a fake stash

			DisableServerSideObjects();

			gameWorld.MainPlayer.OnBtrStateChanged += HandleBtrDoorState;

			btrServerSide.MoveEnable();
			btrServerSide.IncomingToDestinationEvent += ToDestinationEvent;

			// Sync initial position and rotation
			UpdateDataPacket();
			btrClientSide.transform.position = btrDataPacket.position;
			btrClientSide.transform.rotation = btrDataPacket.rotation;

			// Initialise turret variables
			btrTurretServer = btrServerSide.BTRTurret;
			Transform btrTurretDefaultTargetTransform = (Transform)AccessTools.Field(btrTurretServer.GetType(), "defaultTargetTransform").GetValue(btrTurretServer);
			isTurretInDefaultRotation = btrTurretServer.targetTransform == btrTurretDefaultTargetTransform
				&& btrTurretServer.targetPosition == btrTurretServer.defaultAimingPosition;
			btrMachineGunAmmo = (BulletClass)BTRUtil.CreateItem(BTRUtil.BTRMachineGunAmmoTplId);
			btrMachineGunWeapon = BTRUtil.CreateItem(BTRUtil.BTRMachineGunWeaponTplId);

			// Pull services data for the BTR from the server
			TraderServicesManager.Instance.GetTraderServicesDataFromServer(BTRUtil.BTRTraderId);

			btrInitialized = true;
			StartCoroutine(SendBotProfileId());
		}


		private void ConfigureSettingsFromServer()
		{
			SPT.Custom.BTR.Models.BTRConfigModel serverConfig = BTRUtil.GetConfigFromServer();

			btrServerSide.moveSpeed = serverConfig.MoveSpeed;
			btrServerSide.pauseDurationRange.x = serverConfig.PointWaitTime.Min;
			btrServerSide.pauseDurationRange.y = serverConfig.PointWaitTime.Max;
			btrServerSide.readyToDeparture = serverConfig.TaxiWaitTime;
			coverFireTime = serverConfig.CoverFireTime;
			machineGunAimDelay = serverConfig.MachineGunAimDelay;
			machineGunBurstCount = new Vector2(serverConfig.MachineGunBurstCount.Min, serverConfig.MachineGunBurstCount.Max);
			machineGunRecoveryTime = new Vector2(serverConfig.MachineGunRecoveryTime.Min, serverConfig.MachineGunRecoveryTime.Max);
		}

		private void InitBtrBotService()
		{
			btrBotShooter = btrController.BotShooterBtr;
			btrBotShooter.GetPlayer.GetComponent<Rigidbody>().detectCollisions = false; // disable rigidbody collisions with BTR bot
			firearmController = btrBotShooter.GetComponent<Player.FirearmController>();
			WeaponPrefab weaponPrefab = (WeaponPrefab)AccessTools.Field(firearmController.GetType(), "weaponPrefab_0").GetValue(firearmController);
			weaponSoundPlayer = weaponPrefab.GetComponent<WeaponSoundPlayer>();

			btrBotService.Reset(); // Player will be added to Neutrals list and removed from Enemies list
			TraderServicesManager.Instance.OnTraderServicePurchased += BtrTraderServicePurchased;
		}

		/**
         * BTR has arrived at a destination, re-calculate taxi prices and remove purchased taxi service
         */
		private void ToDestinationEvent(PathDestination destinationPoint, bool isFirst, bool isFinal, bool isLastRoutePoint)
		{
			// Remove purchased taxi service
			TraderServicesManager.Instance.RemovePurchasedService(ETraderServiceType.PlayerTaxi, BTRUtil.BTRTraderId);

			// Update the prices for the taxi service
			_updateTaxiPriceMethod.Invoke(btrController, [destinationPoint, isFinal]);

			// Update the UI
			TraderServicesManager.Instance.GetTraderServicesDataFromServer(BTRUtil.BTRTraderId);
		}

		private bool IsBtrService(ETraderServiceType serviceType)
		{
			if (serviceType == ETraderServiceType.BtrItemsDelivery || serviceType == ETraderServiceType.PlayerTaxi || serviceType == ETraderServiceType.BtrBotCover)
			{
				return true;
			}

			return false;
		}

		private void BtrTraderServicePurchased(ETraderServiceType serviceType, string subserviceId)
		{
			if (!IsBtrService(serviceType))
			{
				return;
			}

			List<Player> passengers = gameWorld.AllAlivePlayersList.Where(x => x.BtrState == EPlayerBtrState.Inside).ToList();
			List<int> playersToNotify = passengers.Select(x => x.Id).ToList();
			btrController.method_6(playersToNotify, serviceType); // notify BTR passengers that a service has been purchased

			switch (serviceType)
			{
				case ETraderServiceType.BtrBotCover:
					botEventHandler.ApplyTraderServiceBtrSupport(passengers);
					StartCoverFireTimer(coverFireTime);
					break;
				case ETraderServiceType.PlayerTaxi:
					btrController.BtrVehicle.IsPaid = true;
					btrController.BtrVehicle.MoveToDestination(subserviceId);
					break;
			}

			GenericPacket responsePacket = new(EPackageType.TraderServiceNotification)
			{
				NetId = ((CoopPlayer)gameWorld.MainPlayer).NetId,
				TraderServiceType = serviceType
			};

			server.SendDataToAll(ref responsePacket, DeliveryMethod.ReliableUnordered);
		}

		public void NetworkBtrTraderServicePurchased(BTRServicePacket packet)
		{
			if (!IsBtrService(packet.TraderServiceType))
			{
				return;
			}

			List<Player> passengers = gameWorld.AllAlivePlayersList.Where(x => x.BtrState == EPlayerBtrState.Inside).ToList();
			List<int> playersToNotify = passengers.Select(x => x.Id).ToList();
			btrController.method_6(playersToNotify, packet.TraderServiceType); // notify BTR passengers that a service has been purchased

			switch (packet.TraderServiceType)
			{
				case ETraderServiceType.BtrBotCover:
					botEventHandler.ApplyTraderServiceBtrSupport(passengers);
					StartCoverFireTimer(coverFireTime);
					break;
				case ETraderServiceType.PlayerTaxi:
					btrController.BtrVehicle.IsPaid = true;
					btrController.BtrVehicle.MoveToDestination(packet.SubserviceId);
					break;
			}

			GenericPacket responsePacket = new(EPackageType.TraderServiceNotification)
			{
				NetId = ((CoopPlayer)gameWorld.MainPlayer).NetId,
				TraderServiceType = packet.TraderServiceType
			};

			server.SendDataToAll(ref responsePacket, DeliveryMethod.ReliableUnordered);
		}

		private void StartCoverFireTimer(float time)
		{
			_coverFireTimerCoroutine = StaticManager.BeginCoroutine(CoverFireTimer(time));
		}

		private IEnumerator CoverFireTimer(float time)
		{
			yield return new WaitForSeconds(time);
			botEventHandler.StopTraderServiceBtrSupport();
		}

		private void HandleBtrDoorState(EPlayerBtrState playerBtrState)
		{
			if (playerBtrState == EPlayerBtrState.GoIn || playerBtrState == EPlayerBtrState.GoOut)
			{
				// Open Door
				UpdateBTRSideDoorState(1);
			}
			else if (playerBtrState == EPlayerBtrState.Inside || playerBtrState == EPlayerBtrState.Outside)
			{
				// Close Door
				UpdateBTRSideDoorState(0);
			}
		}

		private void UpdateBTRSideDoorState(byte state)
		{
			try
			{
				BTRSide btrSide = lastInteractPlayer.BtrInteractionSide != null ? lastInteractPlayer.BtrInteractionSide : lastInteractedBtrSide;
				byte sideId = btrClientSide.GetSideId(btrSide);
				switch (sideId)
				{
					case 0:
						btrServerSide.LeftSideState = state;
						break;
					case 1:
						btrServerSide.RightSideState = state;
						break;
				}
			}
			catch
			{
				btrLogger.LogError("[SPT-BTR] lastInteractedBtrSide is null when it shouldn't be. Check logs.");
				throw;
			}
		}

		private BTRDataPacket UpdateDataPacket()
		{
			btrDataPacket.position = btrServerSide.transform.position;
			btrDataPacket.rotation = btrServerSide.transform.rotation;
			if (btrTurretServer != null && btrTurretServer.gunsBlockRoot != null)
			{
				btrDataPacket.turretRotation = btrTurretServer.transform.localEulerAngles.y;
				btrDataPacket.gunsBlockRotation = btrTurretServer.gunsBlockRoot.localEulerAngles.x;
			}
			btrDataPacket.State = (byte)btrServerSide.BtrState;
			btrDataPacket.RouteState = (byte)btrServerSide.VehicleRouteState;
			btrDataPacket.LeftSideState = btrServerSide.LeftSideState;
			btrDataPacket.LeftSlot0State = btrServerSide.LeftSlot0State;
			btrDataPacket.LeftSlot1State = btrServerSide.LeftSlot1State;
			btrDataPacket.RightSideState = btrServerSide.RightSideState;
			btrDataPacket.RightSlot0State = btrServerSide.RightSlot0State;
			btrDataPacket.RightSlot1State = btrServerSide.RightSlot1State;
			btrDataPacket.currentSpeed = btrServerSide.currentSpeed;
			btrDataPacket.timeToEndPause = btrServerSide.timeToEndPause;
			btrDataPacket.moveDirection = (byte)btrServerSide.VehicleMoveDirection;
			btrDataPacket.MoveSpeed = btrServerSide.moveSpeed;
			if (btrController != null && btrController.BotShooterBtr != null)
			{
				btrDataPacket.BtrBotId = btrController.BotShooterBtr.Id;
			}

			BTRPacket packet = new()
			{
				BTRDataPacket = btrDataPacket,
			};

			if (shotQueue.Count > 0)
			{
				packet.HasShot = true;
				KeyValuePair<Vector3, Vector3> shotInfo = shotQueue.Dequeue();
				packet.ShotPosition = shotInfo.Key;
				packet.ShotDirection = shotInfo.Value;
			}

			server.SendDataToAll(ref packet, DeliveryMethod.Unreliable);

			return btrDataPacket;
		}

		private void DisableServerSideObjects()
		{
			MeshRenderer[] meshRenderers = btrServerSide.transform.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer renderer in meshRenderers)
			{
				renderer.enabled = false;
			}

			btrServerSide.turnCheckerObject.GetComponent<Renderer>().enabled = false; // Disables the red debug sphere

			// For some reason the client BTR collider is disabled but the server collider is enabled.
			// Initially we assumed there was a reason for this so it was left as is.
			// Turns out disabling the server collider in favour of the client collider fixes the "BTR doing a wheelie" bug,
			// while preventing the player from walking through the BTR.
			// We also need to change the client collider's layer to HighPolyCollider due to unknown collisions that occur
			// when going down a steep slope.

			// Add collision debugger component to log collisions in the EFT Console
			Collider[] clientColliders = btrClientSide.GetComponentsInChildren<Collider>(true);
			//foreach (var collider in clientColliders)
			//{
			//    collider.gameObject.AddComponent<CollisionDebugger>();
			//}

			Collider[] serverColliders = btrServerSide.GetComponentsInChildren<Collider>(true);
			//foreach (var collider in serverColliders)
			//{
			//    collider.gameObject.AddComponent<CollisionDebugger>();
			//}

			Collider clientRootCollider = clientColliders.First(x => x.gameObject.name == "Root");

			// Retrieve all TerrainColliders
			List<TerrainCollider> terrainColliders = new();

			foreach (GPUInstancerManager manager in GPUInstancerManager.activeManagerList)
			{
				if (manager.GetType() != typeof(GPUInstancerDetailManager))
				{
					continue;
				}

				GPUInstancerDetailManager detailManager = (GPUInstancerDetailManager)manager;
				if (detailManager.terrain == null)
				{
					continue;
				}

				terrainColliders.Add(detailManager.terrain.GetComponent<TerrainCollider>());
			}

			// Make the Root collider ignore collisions with TerrainColliders
			foreach (Collider collider in terrainColliders)
			{
				Physics.IgnoreCollision(clientRootCollider, collider);
			}

			// Retrieve all wheel colliders on the serverside BTR
			const string wheelColliderParentName = "BTR_82_wheel";
			const string wheelColliderName = "Cylinder";

			Collider[] serverWheelColliders = serverColliders
				.Where(x => x.transform.parent.name.StartsWith(wheelColliderParentName) && x.gameObject.name.StartsWith(wheelColliderName))
				.ToArray();

			// Make the Root collider ignore collisions with the serverside BTR wheels
			foreach (Collider collider in serverWheelColliders)
			{
				Physics.IgnoreCollision(clientRootCollider, collider);
			}

			// Enable clientside BTR collider and disable serverside BTR collider
			const string exteriorColliderName = "BTR_82_exterior_COLLIDER";
			Collider serverExteriorCollider = serverColliders
				.First(x => x.gameObject.name == exteriorColliderName);
			Collider clientExteriorCollider = clientColliders
				.First(x => x.gameObject.name == exteriorColliderName);

			serverExteriorCollider.gameObject.SetActive(false);
			clientExteriorCollider.gameObject.SetActive(true);
			clientExteriorCollider.gameObject.layer = LayerMask.NameToLayer("HighPolyCollider");
		}

		private void UpdateTarget()
		{
			currentTarget = btrBotShooter.Memory.GoalEnemy;
		}

		private bool HasTarget()
		{
			if (currentTarget != null)
			{
				return true;
			}

			return false;
		}

		private void SetAim()
		{
			if (currentTarget.IsVisible)
			{
				Vector3 targetPos = currentTarget.CurrPosition;
				Transform targetTransform = currentTarget.Person.Transform.Original;
				if (btrTurretServer.CheckPositionInAimingZone(targetPos) && btrTurretServer.targetTransform != targetTransform)
				{
					btrTurretServer.EnableAimingObject(targetTransform);
				}
			}
			else
			{
				Vector3 targetLastPos = currentTarget.EnemyLastPositionReal;
				if (btrTurretServer.CheckPositionInAimingZone(targetLastPos)
					&& Time.time - currentTarget.PersonalLastSeenTime < 3f
					&& btrTurretServer.targetPosition != targetLastPos)
				{
					btrTurretServer.EnableAimingPosition(targetLastPos);

				}
				else if (Time.time - currentTarget.PersonalLastSeenTime >= 3f && !isTurretInDefaultRotation)
				{
					btrTurretServer.DisableAiming();
				}
			}
		}

		private bool CanShoot()
		{
			if (currentTarget.IsVisible && btrBotShooter.BotBtrData.CanShoot())
			{
				return true;
			}

			return false;
		}

		private void StartShooting()
		{
			_shootingTargetCoroutine = StaticManager.BeginCoroutine(ShootMachineGun());
		}

		/// <summary>
		/// Custom method to make the BTR coaxial machine gun shoot.
		/// </summary>
		private IEnumerator ShootMachineGun()
		{
			isShooting = true;

			yield return new WaitForSeconds(machineGunAimDelay);
			if (currentTarget?.Person == null || currentTarget?.IsVisible == false || !btrBotShooter.BotBtrData.CanShoot())
			{
				isShooting = false;
				yield break;
			}

			Transform machineGunMuzzle = btrTurretServer.machineGunLaunchPoint;
			ISharedBallisticsCalculator ballisticCalculator = gameWorld.SharedBallisticsCalculator;

			int burstMin = Mathf.FloorToInt(machineGunBurstCount.x);
			int burstMax = Mathf.FloorToInt(machineGunBurstCount.y);
			int burstCount = Random.Range(burstMin, burstMax + 1);
			Vector3 targetHeadPos = currentTarget.Person.PlayerBones.Head.position;
			while (burstCount > 0)
			{
				// Only update shooting position if the target isn't null
				if (currentTarget?.Person != null)
				{
					targetHeadPos = currentTarget.Person.PlayerBones.Head.position;
				}
				Vector3 aimDirection = Vector3.Normalize(targetHeadPos - machineGunMuzzle.position);
				ballisticCalculator.Shoot(btrMachineGunAmmo, machineGunMuzzle.position, aimDirection, btrBotShooter.ProfileId, btrMachineGunWeapon, 1f, 0);
				firearmController.method_54(weaponSoundPlayer, btrMachineGunAmmo, machineGunMuzzle.position, aimDirection, false);
				burstCount--;
				shotQueue.Enqueue(new(machineGunMuzzle.position, aimDirection));
				yield return new WaitForSeconds(0.092308f); // 650 RPM
			}

			float waitTime = Random.Range(machineGunRecoveryTime.x, machineGunRecoveryTime.y);
			yield return new WaitForSeconds(waitTime);

			isShooting = false;
		}

		public bool HostInteraction(Player player, PlayerInteractPacket packet)
		{
			GameWorld gameWorld = Singleton<GameWorld>.Instance;

			// Prevent player from entering BTR when blacklisted
			BotOwner btrBot = gameWorld.BtrController.BotShooterBtr;
			if (btrBot.BotsGroup.Enemies.ContainsKey(player))
			{
				// Notify player they are blacklisted from entering BTR
				GlobalEventHandlerClass.CreateEvent<BtrNotificationInteractionMessageEvent>().Invoke(player.Id, EBtrInteractionStatus.Blacklisted);
				return false;
			}

			if (packet.HasInteraction)
			{
				BTRView btrView = gameWorld.BtrController.BtrView;
				if (btrView == null)
				{
					btrLogger.LogError("[SPT-BTR] BTRInteractionPatch - btrView is null");
					return false;
				}

				OnPlayerInteractDoor(player, packet);
				btrView.Interaction(player, packet);

				return true;
			}
			return false;
		}

		public void HostObservedInteraction(Player player, PlayerInteractPacket packet)
		{
			if (packet.HasInteraction)
			{
				BTRView btrView = gameWorld.BtrController.BtrView;
				if (btrView == null)
				{
					btrLogger.LogError("[SPT-BTR] BTRInteractionPatch - btrView is null");
					return;
				}

				OnPlayerInteractDoor(player, packet);
				ObservedBTRInteraction(player, packet);
			}
		}

		private async void ObservedBTRInteraction(Player player, PlayerInteractPacket packet)
		{
			BTRSide side = btrClientSide.method_9(packet.SideId);

			if (packet.InteractionType == EInteractionType.GoIn)
			{
				lastInteractedBtrSide = side;
				player.BtrInteractionSide = side;
				UpdateBTRSideDoorState(1);
				player.BtrState = EPlayerBtrState.Approach;
				btrClientSide.method_18(player);
				player.BtrState = EPlayerBtrState.GoIn;
				//side.AddPassenger(player, packet.SlotId);
				player.MovementContext.PlayerAnimator.SetBtrLayerEnabled(true);
				player.MovementContext.PlayerAnimator.SetBtrGoIn(packet.Fast);
				player.BtrState = EPlayerBtrState.Inside;
				await Task.Delay(2200);
				UpdateBTRSideDoorState(0);
			}
			else if (packet.InteractionType == EInteractionType.GoOut)
			{
				lastInteractedBtrSide = side;
				player.BtrInteractionSide = side;
				UpdateBTRSideDoorState(1);
				player.BtrState = EPlayerBtrState.GoOut;
				player.MovementContext.PlayerAnimator.SetBtrGoOut(packet.Fast);
				player.MovementContext.PlayerAnimator.SetBtrLayerEnabled(false);
				(Vector3 start, Vector3 target) points = side.GoOutPoints();
				side.ApplyPlayerRotation(player.MovementContext, points.start, points.target + Vector3.up * 1.9f);
				player.BtrState = EPlayerBtrState.Outside;
				//side.RemovePassenger(player);
				btrClientSide.method_19(player);
				await Task.Delay(2200);
				UpdateBTRSideDoorState(0);
			}
		}

		private void OnDestroy()
		{
			if (gameWorld == null)
			{
				return;
			}

			StaticManager.KillCoroutine(ref _shootingTargetCoroutine);
			StaticManager.KillCoroutine(ref _coverFireTimerCoroutine);

			if (TraderServicesManager.Instance != null)
			{
				TraderServicesManager.Instance.OnTraderServicePurchased -= BtrTraderServicePurchased;
			}

			if (gameWorld.MainPlayer != null)
			{
				gameWorld.MainPlayer.OnBtrStateChanged -= HandleBtrDoorState;
			}

			if (btrClientSide != null)
			{
				Debug.LogWarning("[SPT-BTR] BTRManager - Destroying btrClientSide");
				Destroy(btrClientSide.gameObject);
			}

			if (btrServerSide != null)
			{
				Debug.LogWarning("[SPT-BTR] BTRManager - Destroying btrServerSide");
				Destroy(btrServerSide.gameObject);
			}
		}
	}
}