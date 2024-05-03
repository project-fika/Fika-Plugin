// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class PacketReceiver : MonoBehaviour
    {
        private CoopPlayer player;
        private ObservedCoopPlayer observedPlayer;
        public FikaServer Server { get; private set; }
        public FikaClient Client { get; private set; }
        public PlayerStatePacket LastState { get; set; }
        public PlayerStatePacket NewState { get; set; }
        public Queue<WeaponPacket> FirearmPackets { get; private set; } = new(50);
        public Queue<DamagePacket> DamagePackets { get; private set; } = new(50);
        public Queue<InventoryPacket> InventoryPackets { get; private set; } = new(50);
        public Queue<CommonPlayerPacket> CommonPlayerPackets { get; private set; } = new(50);
        public Queue<HealthSyncPacket> HealthSyncPackets { get; private set; } = new(50);

        private void Awake()
        {
            player = GetComponent<CoopPlayer>();
            if (!player.IsYourPlayer)
            {
                observedPlayer = GetComponent<ObservedCoopPlayer>();
            }
        }

        private void Start()
        {
            if (MatchmakerAcceptPatches.IsServer)
            {
                Server = Singleton<FikaServer>.Instance;
            }
            else
            {
                Client = Singleton<FikaClient>.Instance;
            }

            LastState = new(player.NetId, player.Position, player.Rotation, player.HeadRotation,
                            player.LastDirection, player.CurrentManagedState.Name, player.MovementContext.Tilt,
                            player.MovementContext.Step, player.CurrentAnimatorStateIndex, player.MovementContext.SmoothedCharacterMovementSpeed,
                            player.IsInPronePose, player.PoseLevel, player.MovementContext.IsSprintEnabled, player.Physical.SerializationStruct,
                            player.MovementContext.BlindFire, player.observedOverlap, player.leftStanceDisabled, player.MovementContext.IsGrounded,
                            false, 0, Vector3.zero);

            NewState = new(player.NetId, player.Position, player.Rotation, player.HeadRotation,
                            player.LastDirection, player.CurrentManagedState.Name, player.MovementContext.Tilt,
                            player.MovementContext.Step, player.CurrentAnimatorStateIndex, player.MovementContext.SmoothedCharacterMovementSpeed,
                            player.IsInPronePose, player.PoseLevel, player.MovementContext.IsSprintEnabled, player.Physical.SerializationStruct,
                            player.MovementContext.BlindFire, player.observedOverlap, player.leftStanceDisabled, player.MovementContext.IsGrounded,
                            false, 0, Vector3.zero);
        }

        private void Update()
        {
            if (observedPlayer != null)
            {
                LastState = observedPlayer.Interpolate(NewState, LastState);
                int healthSyncPackets = HealthSyncPackets.Count;
                if (healthSyncPackets > 0)
                {
                    for (int i = 0; i < healthSyncPackets; i++)
                    {
                        HealthSyncPacket packet = HealthSyncPackets.Dequeue();
                        if (packet.Packet.SyncType == GStruct346.ESyncType.IsAlive && !packet.Packet.Data.IsAlive.IsAlive)
                        {
                            observedPlayer.SetAggressor(packet.KillerId, packet.KillerWeaponId);
                            observedPlayer.SetInventory(packet.Equipment);
                            observedPlayer.RagdollPacket = packet.RagdollPacket;
                        }
                        observedPlayer.NetworkHealthController.HandleSyncPacket(packet.Packet);
                    }
                }
            }
            if (player == null)
            {
                return;
            }
            int firearmPackets = FirearmPackets.Count;
            if (firearmPackets > 0)
            {
                for (int i = 0; i < firearmPackets; i++)
                {
                    player.HandleWeaponPacket(FirearmPackets.Dequeue());
                }
            }
            int healthPackets = DamagePackets.Count;
            if (healthPackets > 0)
            {
                for (int i = 0; i < healthPackets; i++)
                {
                    player.HandleDamagePacket(DamagePackets.Dequeue());
                }
            }
            int inventoryPackets = InventoryPackets.Count;
            if (inventoryPackets > 0)
            {
                for (int i = 0; i < inventoryPackets; i++)
                {
                    player.HandleInventoryPacket(InventoryPackets.Dequeue());
                }
            }
            int commonPlayerPackets = CommonPlayerPackets.Count;
            if (commonPlayerPackets > 0)
            {
                for (int i = 0; i < commonPlayerPackets; i++)
                {
                    player.HandleCommonPacket(CommonPlayerPackets.Dequeue());
                }
            }
        }
    }
}
