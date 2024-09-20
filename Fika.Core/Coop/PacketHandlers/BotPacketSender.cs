// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using Fika.Core.Coop.ObservedClasses.Snapshotter;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
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
		private float frameCounter;

		protected void Awake()
		{
			player = GetComponent<CoopPlayer>();
			Server = Singleton<FikaServer>.Instance;
			updateRate = Server.SendRate;
			frameCounter = 0;
		}

		public void Init()
		{

		}

		public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
		{
			if (Server != null)
			{
				Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
			}
		}

		protected void FixedUpdate()
		{
			if (player == null)
			{
				return;
			}

			if (player.AIData?.BotOwner == null)
			{
				return;
			}

			BotMover mover = player.AIData.BotOwner.Mover;
			if (mover == null)
			{
				return;
			}

			float dur = 1f / updateRate;
			frameCounter += Time.fixedDeltaTime;
			while (frameCounter >= dur)
			{
				frameCounter -= dur;
				SendPlayerState(mover);
			}
		}

		private void SendPlayerState(BotMover mover)
		{
			PlayerStatePacket playerStatePacket = new(player.NetId, player.Position, player.Rotation, player.HeadRotation, player.LastDirection,
				player.CurrentManagedState.Name,
				player.MovementContext.IsInMountedState ? player.MovementContext.MountedSmoothedTilt : player.MovementContext.SmoothedTilt,
				player.MovementContext.Step, player.CurrentAnimatorStateIndex, player.MovementContext.SmoothedCharacterMovementSpeed,
				player.IsInPronePose, player.PoseLevel, player.MovementContext.IsSprintEnabled, player.Physical.SerializationStruct,
				player.MovementContext.BlindFire, player.observedOverlap, player.leftStanceDisabled,
				player.MovementContext.IsGrounded, player.hasGround, player.CurrentSurface, player.MovementContext.SurfaceNormal,
				NetworkTimeSync.Time);

			Server.SendDataToAll(ref playerStatePacket, DeliveryMethod.Unreliable);

			if (!mover.IsMoving || mover.Pause || !player.MovementContext.CanWalk)
			{
				player.LastDirection = Vector2.zero;
			}
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
