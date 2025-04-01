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
        private bool IsMoving
        {
            get
            {
                BotMover mover = player.AIData.BotOwner.Mover;
                if (mover == null)
                {
                    return false;
                }

                return mover.IsMoving && !mover.Pause && player.MovementContext.CanWalk;
            }
        }

        public static BotPacketSender Create(CoopBot bot)
        {
            BotPacketSender sender = bot.gameObject.AddComponent<BotPacketSender>();
            sender.player = bot;
            sender.Server = Singleton<FikaServer>.Instance;
            sender.state = new(bot.NetId);
            sender.Enabled = true;
            return sender;
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

            state.UpdateData(player, IsMoving);
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
