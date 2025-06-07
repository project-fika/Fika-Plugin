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
        public bool SendState { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }

        private CoopPlayer _player;
        private bool _sendPackets;
        private PlayerStatePacket _state;
        private bool IsMoving
        {
            get
            {
                return _player.CurrentManagedState.Name is not (EPlayerState.Idle
                    or EPlayerState.IdleWeaponMounting
                    or EPlayerState.ProneIdle);
            }
        }

        public static BotPacketSender Create(CoopBot bot)
        {
            BotPacketSender sender = bot.gameObject.AddComponent<BotPacketSender>();
            sender._player = bot;
            sender.Server = Singleton<FikaServer>.Instance;
            sender._state = new(bot.NetId);
            sender.Enabled = true;
            sender.SendState = true;
            return sender;
        }

        public void Init()
        {

        }

        public void OnEnable()
        {
            _sendPackets = true;
        }

        public void OnDisable()
        {
            _sendPackets = false;
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
            if (!_sendPackets)
            {
                return;
            }

            _state.UpdateData(_player, IsMoving);
            Server.SendDataToAll(ref _state, DeliveryMethod.Unreliable);
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
