// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
	public class BotPacketSender : MonoBehaviour, IPacketSender
	{
		private CoopPlayer player;

		public bool Enabled { get; set; } = true;
		public FikaServer Server { get; set; }
		public FikaClient Client { get; set; }
		public Queue<WeaponPacket> FirearmPackets { get; set; } = new(50);
		public Queue<DamagePacket> DamagePackets { get; set; } = new(50);
		public Queue<ArmorDamagePacket> ArmorDamagePackets { get; set; } = new(50);
		public Queue<InventoryPacket> InventoryPackets { get; set; } = new(50);
		public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; } = new(50);
		public Queue<HealthSyncPacket> HealthSyncPackets { get; set; } = new(50);

		private int updateRate;
		private BotStateManager manager;
		private bool sendPackets;

		protected void Awake()
		{
			player = GetComponent<CoopPlayer>();
			Server = Singleton<FikaServer>.Instance;
			updateRate = Server.SendRate;
		}

		public void Init()
		{

		}

		public void OnEnable()
		{
			sendPackets = true;
		}

		public void OnDisable()
		{
			sendPackets = false;
		}

		public void AssignManager(BotStateManager stateManager)
		{
			manager = stateManager;
			manager.OnUpdate += SendPlayerState;
		}

		public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
		{
			if (Server != null)
			{
				Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
			}
		}

		private void SendPlayerState()
		{
			if (!sendPackets)
			{
				return;
			}

			if (!player.HealthController.IsAlive)
			{
				manager.OnUpdate -= SendPlayerState;
				return;
			}

			BotMover mover = player.AIData.BotOwner.Mover;
			if (mover == null)
			{
				return;
			}

			bool isMoving = mover.IsMoving && !mover.Pause && player.MovementContext.CanWalk;
			Vector2 direction = isMoving ? player.MovementContext.MovementDirection : Vector2.zero;
			PlayerStatePacket playerStatePacket = new(player.NetId, player.Position, player.Rotation, player.HeadRotation, direction,
				player.CurrentManagedState.Name,
				player.MovementContext.IsInMountedState ? player.MovementContext.MountedSmoothedTilt : player.MovementContext.SmoothedTilt,
				player.MovementContext.Step, player.CurrentAnimatorStateIndex, player.MovementContext.SmoothedCharacterMovementSpeed,
				player.IsInPronePose, player.PoseLevel, player.MovementContext.IsSprintEnabled, player.Physical.SerializationStruct,
				player.MovementContext.BlindFire, player.ObservedOverlap, player.LeftStanceDisabled,
				player.MovementContext.IsGrounded, player.HasGround, player.CurrentSurface, NetworkTimeSync.Time);

			Server.SendDataToAll(ref playerStatePacket, DeliveryMethod.Unreliable);
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

					Server.SendDataToAll(ref firearmPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int damagePackets = DamagePackets.Count;
			if (damagePackets > 0)
			{
				for (int i = 0; i < damagePackets; i++)
				{
					DamagePacket damagePacket = DamagePackets.Dequeue();
					damagePacket.NetId = player.NetId;

					Server.SendDataToAll(ref damagePacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int armorDamagePackets = ArmorDamagePackets.Count;
			if (armorDamagePackets > 0)
			{
				for (int i = 0; i < armorDamagePackets; i++)
				{
					ArmorDamagePacket armorDamagePacket = ArmorDamagePackets.Dequeue();
					armorDamagePacket.NetId = player.NetId;

					Server.SendDataToAll(ref armorDamagePacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int inventoryPackets = InventoryPackets.Count;
			if (inventoryPackets > 0)
			{
				for (int i = 0; i < inventoryPackets; i++)
				{
					InventoryPacket inventoryPacket = InventoryPackets.Dequeue();
					inventoryPacket.NetId = player.NetId;

					Server.SendDataToAll(ref inventoryPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int commonPlayerPackets = CommonPlayerPackets.Count;
			if (commonPlayerPackets > 0)
			{
				for (int i = 0; i < commonPlayerPackets; i++)
				{
					CommonPlayerPacket commonPlayerPacket = CommonPlayerPackets.Dequeue();
					commonPlayerPacket.NetId = player.NetId;

					Server.SendDataToAll(ref commonPlayerPacket, DeliveryMethod.ReliableOrdered);
				}
			}
			int healthSyncPackets = HealthSyncPackets.Count;
			if (healthSyncPackets > 0)
			{
				for (int i = 0; i < healthSyncPackets; i++)
				{
					HealthSyncPacket healthSyncPacket = HealthSyncPackets.Dequeue();
					healthSyncPacket.NetId = player.NetId;

					Server.SendDataToAll(ref healthSyncPacket, DeliveryMethod.ReliableOrdered);
				}
			}
		}

		public void DestroyThis()
		{
			manager.OnUpdate -= SendPlayerState;
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
