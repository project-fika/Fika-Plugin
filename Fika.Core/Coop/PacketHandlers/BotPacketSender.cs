// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class BotPacketSender : MonoBehaviour, IPacketSender
    {
        public bool Enabled { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }

        private CoopPlayer player;
        private BotStateManager manager;
        private bool sendPackets;
        private PlayerStatePacket state;

        protected void Awake()
        {
            player = GetComponent<CoopPlayer>();
            Server = Singleton<FikaServer>.Instance;
            state = new(player.NetId);
            Enabled = true;
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
            state.UpdateData(player, isMoving);
            Server.SendDataToAll(ref state, DeliveryMethod.Unreliable);
        }

        public void DestroyThis()
        {
            manager.OnUpdate -= SendPlayerState;
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
