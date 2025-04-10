// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
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
        private bool sendPackets;
        private PlayerStatePacket state;
        private bool IsMoving
        {
            get
            {
                return player.CurrentManagedState.Name is not (EPlayerState.Idle
                    or EPlayerState.IdleWeaponMounting
                    or EPlayerState.ProneIdle);
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

        public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
        {
            if (Server != null)
            {
                Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendPlayerState()
        {
            if (!sendPackets)
            {
                return;
            }

            state.UpdateData(player, IsMoving);
            Server.SendDataToAll(ref state, DeliveryMethod.Unreliable);
        }

        public void DestroyThis()
        {
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
