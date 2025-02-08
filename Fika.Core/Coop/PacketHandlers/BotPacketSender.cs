// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class BotPacketSender : MonoBehaviour, IPacketSender
    {
        private CoopPlayer player;

        public bool Enabled { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public Queue<IQueuePacket> PacketQueue { get; set; }

        private BotStateManager manager;
        private bool sendPackets;

        protected void Awake()
        {
            player = GetComponent<CoopPlayer>();
            Server = Singleton<FikaServer>.Instance;
            Enabled = true;
            PacketQueue = new();
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

        protected void LateUpdate()
        {
            int packetAmount = PacketQueue.Count;
            for (int i = 0; i < packetAmount; i++)
            {
                IQueuePacket packet = PacketQueue.Dequeue();
                packet.NetId = player.NetId;
                Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        public void DestroyThis()
        {
            manager.OnUpdate -= SendPlayerState;
            PacketQueue.Clear();
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
