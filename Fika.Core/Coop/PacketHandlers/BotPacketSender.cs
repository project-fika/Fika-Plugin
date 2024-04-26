// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using LiteNetLib;
using LiteNetLib.Utils;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class BotPacketSender : MonoBehaviour, IPacketSender
    {
        private CoopPlayer player;

        public FikaServer Server { get; set; } = Singleton<FikaServer>.Instance;
        public FikaClient Client { get; set; }
        public NetDataWriter Writer { get; set; } = new();
        public Queue<WeaponPacket> FirearmPackets { get; set; } = new(50);
        public Queue<DamagePacket> HealthPackets { get; set; } = new(50);
        public Queue<InventoryPacket> InventoryPackets { get; set; } = new(50);
        public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; } = new(50);
        public Queue<HealthSyncPacket> HealthSyncPackets { get; set; } = new(50);

        private void Awake()
        {
            player = GetComponent<CoopPlayer>();
        }

        private void FixedUpdate()
        {
            if (player == null || Writer == null)
            {
                return;
            }

            PlayerStatePacket playerStatePacket = new(player.ProfileId, player.Position, player.Rotation, player.HeadRotation,
                            player.LastDirection, player.CurrentManagedState.Name, player.MovementContext.SmoothedTilt,
                            player.MovementContext.Step, player.CurrentAnimatorStateIndex, player.MovementContext.SmoothedCharacterMovementSpeed,
                            player.IsInPronePose, player.PoseLevel, player.MovementContext.IsSprintEnabled, player.Physical.SerializationStruct,
                            player.MovementContext.BlindFire, player.observedOverlap, player.leftStanceDisabled, player.MovementContext.IsGrounded,
                            player.hasGround, player.CurrentSurface, player.MovementContext.SurfaceNormal);

            Writer.Reset();
            Server?.SendDataToAll(Writer, ref playerStatePacket, DeliveryMethod.Unreliable);

            player.LastDirection = Vector2.zero; // Bots give a constant input for some odd reason, resetting on FixedUpdate should be ok from my testing and does not cause sliding for clients
        }

        private void Update()
        {
            int firearmPackets = FirearmPackets.Count;
            if (firearmPackets > 0)
            {
                for (int i = 0; i < firearmPackets; i++)
                {
                    WeaponPacket firearmPacket = FirearmPackets.Dequeue();
                    firearmPacket.ProfileId = player.ProfileId;

                    Writer?.Reset();
                    Server?.SendDataToAll(Writer, ref firearmPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int healthPackets = HealthPackets.Count;
            if (healthPackets > 0)
            {
                for (int i = 0; i < healthPackets; i++)
                {
                    DamagePacket healthPacket = HealthPackets.Dequeue();
                    healthPacket.ProfileId = player.ProfileId;

                    Writer?.Reset();
                    Server?.SendDataToAll(Writer, ref healthPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int inventoryPackets = InventoryPackets.Count;
            if (inventoryPackets > 0)
            {
                for (int i = 0; i < inventoryPackets; i++)
                {
                    InventoryPacket inventoryPacket = InventoryPackets.Dequeue();
                    inventoryPacket.ProfileId = player.ProfileId;

                    Writer?.Reset();
                    Server?.SendDataToAll(Writer, ref inventoryPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int commonPlayerPackets = CommonPlayerPackets.Count;
            if (commonPlayerPackets > 0)
            {
                for (int i = 0; i < commonPlayerPackets; i++)
                {
                    CommonPlayerPacket commonPlayerPacket = CommonPlayerPackets.Dequeue();
                    commonPlayerPacket.ProfileId = player.ProfileId;

                    Writer?.Reset();
                    Server?.SendDataToAll(Writer, ref commonPlayerPacket, DeliveryMethod.ReliableOrdered);
                }
            }
            int healthSyncPackets = HealthSyncPackets.Count;
            if (healthSyncPackets > 0)
            {
                for (int i = 0; i < healthSyncPackets; i++)
                {
                    HealthSyncPacket healthSyncPacket = HealthSyncPackets.Dequeue();
                    healthSyncPacket.ProfileId = player.ProfileId;

                    Writer?.Reset();
                    Server?.SendDataToAll(Writer, ref healthSyncPacket, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        public void DestroyThis()
        {
            Writer = null;
            FirearmPackets.Clear();
            HealthPackets.Clear();
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
