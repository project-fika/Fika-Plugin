// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Factories;
using Fika.Core.Coop.FreeCamera;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
	public class ClientPacketSender : MonoBehaviour, IPacketSender
	{
		private CoopPlayer player;

		public bool Enabled { get; set; } = false;
		public FikaServer Server { get; set; }
		public FikaClient Client { get; set; }
		public Queue<WeaponPacket> FirearmPackets { get; set; } = new(50);
		public Queue<DamagePacket> DamagePackets { get; set; } = new(50);
		public Queue<ArmorDamagePacket> ArmorDamagePackets { get; set; } = new(50);
		public Queue<InventoryPacket> InventoryPackets { get; set; } = new(50);
		public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; } = new(50);
		public Queue<HealthSyncPacket> HealthSyncPackets { get; set; } = new(50);

		private bool CanPing
		{
			get
			{
				return FikaPlugin.UsePingSystem.Value && player.IsYourPlayer && Input.GetKey(FikaPlugin.PingButton.Value.MainKey)
					&& FikaPlugin.PingButton.Value.Modifiers.All(Input.GetKey) && !MonoBehaviourSingleton<PreloaderUI>.Instance.Console.IsConsoleVisible
					&& lastPingTime < DateTime.Now.AddSeconds(-3) && Singleton<IFikaGame>.Instance is CoopGame coopGame && coopGame.Status is GameStatus.Started
					&& !player.IsInventoryOpened;
			}
		}

		private DateTime lastPingTime;
		private int updateRate;
		private int fixedUpdateCount;
		private int fixedUpdatesPerTick;

		protected void Awake()
		{
			player = GetComponent<CoopPlayer>();
			Client = Singleton<FikaClient>.Instance;
			enabled = false;
			lastPingTime = DateTime.Now;
			updateRate = Client.SendRate;
			fixedUpdateCount = 0;
			fixedUpdatesPerTick = Mathf.FloorToInt(60f / updateRate);
		}

		public void Init()
		{
			enabled = true;
			Enabled = true;
			if (player.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController)
			{
				sharedQuestController.LateInit();
			}
		}

		public void SendPacket<T>(ref T packet, bool forced = false) where T : INetSerializable
		{
			if (!Enabled && !forced)
			{
				return;
			}

			Client.SendData(ref packet, DeliveryMethod.ReliableUnordered);
		}

		protected void FixedUpdate()
		{
			if (player == null || Client == null || !Enabled)
			{
				return;
			}

			fixedUpdateCount++;
			if (fixedUpdateCount >= fixedUpdatesPerTick)
			{
				SendPlayerState();
				fixedUpdateCount = 0;
			}
		}

		private void SendPlayerState()
		{
			Vector2 movementDirection = player.MovementContext.IsInMountedState ? Vector2.zero : player.MovementContext.MovementDirection;
			PlayerStatePacket playerStatePacket = new(player.NetId, player.Position, player.Rotation, player.HeadRotation, movementDirection,
				player.CurrentManagedState.Name,
				player.MovementContext.IsInMountedState ? player.MovementContext.MountedSmoothedTilt : player.MovementContext.SmoothedTilt,
				player.MovementContext.Step, player.CurrentAnimatorStateIndex, player.MovementContext.SmoothedCharacterMovementSpeed,
				player.IsInPronePose, player.PoseLevel, player.MovementContext.IsSprintEnabled, player.Physical.SerializationStruct,
				player.MovementContext.BlindFire, player.ObservedOverlap, player.LeftStanceDisabled,
				player.MovementContext.IsGrounded, player.HasGround, player.CurrentSurface, NetworkTimeSync.Time);

			Client.SendData(ref playerStatePacket, DeliveryMethod.Unreliable);
		}

		protected void Update()
		{
			int firearmPackets = FirearmPackets.Count;
			if (firearmPackets > 0)
			{
				for (int i = 0; i < firearmPackets; i++)
				{
					WeaponPacket firearmPacket = FirearmPackets.Dequeue();
					firearmPacket.NetId = player.NetId;

					Client.SendData(ref firearmPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int damagePackets = DamagePackets.Count;
			if (damagePackets > 0)
			{
				for (int i = 0; i < damagePackets; i++)
				{
					DamagePacket damagePacket = DamagePackets.Dequeue();
					damagePacket.NetId = player.NetId;

					Client.SendData(ref damagePacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int armorDamagePackets = ArmorDamagePackets.Count;
			if (armorDamagePackets > 0)
			{
				for (int i = 0; i < armorDamagePackets; i++)
				{
					ArmorDamagePacket armorDamagePacket = ArmorDamagePackets.Dequeue();
					armorDamagePacket.NetId = player.NetId;

					Client.SendData(ref armorDamagePacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int inventoryPackets = InventoryPackets.Count;
			if (inventoryPackets > 0)
			{
				for (int i = 0; i < inventoryPackets; i++)
				{
					InventoryPacket inventoryPacket = InventoryPackets.Dequeue();
					inventoryPacket.NetId = player.NetId;

					Client.SendData(ref inventoryPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int commonPlayerPackets = CommonPlayerPackets.Count;
			if (commonPlayerPackets > 0)
			{
				for (int i = 0; i < commonPlayerPackets; i++)
				{
					CommonPlayerPacket commonPlayerPacket = CommonPlayerPackets.Dequeue();
					commonPlayerPacket.NetId = player.NetId;

					Client.SendData(ref commonPlayerPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int healthSyncPackets = HealthSyncPackets.Count;
			if (healthSyncPackets > 0)
			{
				for (int i = 0; i < healthSyncPackets; i++)
				{
					HealthSyncPacket healthSyncPacket = HealthSyncPackets.Dequeue();
					healthSyncPacket.NetId = player.NetId;

					Client.SendData(ref healthSyncPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			if (CanPing)
			{
				SendPing();
			}
		}

		private void SendPing()
		{
			Transform originTransform;
			Ray sourceRaycast;
			FreeCameraController freeCamController = Singleton<FreeCameraController>.Instance;
			if (freeCamController != null && freeCamController.IsScriptActive)
			{
				originTransform = freeCamController.CameraMain.gameObject.transform;
				sourceRaycast = new(originTransform.position + originTransform.forward / 2f, originTransform.forward);
			}
			else if (player.HealthController.IsAlive)
			{
				if (player.HandsController is CoopClientFirearmController controller && controller.IsAiming)
				{
					sourceRaycast = new(controller.FireportPosition, controller.WeaponDirection);
				}
				else
				{
					originTransform = player.CameraPosition;
					sourceRaycast = new(originTransform.position + originTransform.forward / 2f, player.LookDirection);
				}
			}
			else
			{
				return;
			}
			int layer = LayerMask.GetMask(["HighPolyCollider", "Interactive", "Deadbody", "Player", "Loot", "Terrain"]);
			if (Physics.Raycast(sourceRaycast, out RaycastHit hit, FikaGlobals.PingRange, layer))
			{
				lastPingTime = DateTime.Now;
				//GameObject gameObject = new("Ping", typeof(FikaPing));
				//gameObject.transform.localPosition = hit.point;
				Singleton<GUISounds>.Instance.PlayUISound(PingFactory.GetPingSound());
				GameObject hitGameObject = hit.collider.gameObject;
				int hitLayer = hitGameObject.layer;

				PingFactory.EPingType pingType = PingFactory.EPingType.Point;
				object userData = null;
				string localeId = null;

#if DEBUG
				ConsoleScreen.Log(statement: $"{hit.collider.GetFullPath()}: {LayerMask.LayerToName(hitLayer)}/{hitGameObject.name}");
#endif

				if (LayerMask.LayerToName(hitLayer) == "Player")
				{
					if (hitGameObject.TryGetComponent(out Player player))
					{
						pingType = PingFactory.EPingType.Player;
						userData = player;
					}
				}
				else if (LayerMask.LayerToName(hitLayer) == "Deadbody")
				{
					pingType = PingFactory.EPingType.DeadBody;
					userData = hitGameObject;
				}
				else if (hitGameObject.TryGetComponent(out LootableContainer container))
				{
					pingType = PingFactory.EPingType.LootContainer;
					userData = container;
					localeId = container.ItemOwner.Name;
				}
				else if (hitGameObject.TryGetComponent(out LootItem lootItem))
				{
					pingType = PingFactory.EPingType.LootItem;
					userData = lootItem;
					localeId = lootItem.Item.ShortName;
				}
				else if (hitGameObject.TryGetComponent(out Door door))
				{
					pingType = PingFactory.EPingType.Door;
					userData = door;
				}
				else if (hitGameObject.TryGetComponent(out InteractableObject interactable))
				{
					pingType = PingFactory.EPingType.Interactable;
					userData = interactable;
				}

				GameObject basePingPrefab = PingFactory.AbstractPing.pingBundle.LoadAsset<GameObject>("BasePingPrefab");
				GameObject basePing = GameObject.Instantiate(basePingPrefab);
				Vector3 hitPoint = hit.point;
				PingFactory.AbstractPing abstractPing = PingFactory.FromPingType(pingType, basePing);
				Color pingColor = FikaPlugin.PingColor.Value;
				pingColor = new(pingColor.r, pingColor.g, pingColor.b, 1);
				// ref so that we can mutate it if we want to, ex: if I ping a switch I want it at the switch.gameObject.position + Vector3.up
				abstractPing.Initialize(ref hitPoint, userData, pingColor);

				PingPacket packet = new()
				{
					PingLocation = hitPoint,
					PingType = pingType,
					PingColor = pingColor,
					Nickname = player.Profile.Info.MainProfileNickname,
					LocaleId = string.IsNullOrEmpty(localeId) ? string.Empty : localeId
				};

				SendPacket(ref packet, true);

				if (FikaPlugin.PlayPingAnimation.Value && player.HealthController.IsAlive)
				{
					player.vmethod_6(EInteraction.ThereGesture);
				}
			}
		}

		public void DestroyThis()
		{
			FirearmPackets.Clear();
			DamagePackets.Clear();
			InventoryPackets.Clear();
			CommonPlayerPackets.Clear();
			HealthSyncPackets.Clear();
			if (Server != null)
			{
				Server = null;
			}
			if (Client != null)
			{
				Client = null;
			}
			Destroy(this);
		}
	}
}
