// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;
using LiteNetLib;
using UnityEngine;

namespace Fika.Core.Main.PacketHandlers
{
    public class BotPacketSender : MonoBehaviour, IPacketSender
    {
        public bool SendState { get; set; }
        public IFikaNetworkManager NetworkManager { get; set; }

        private FikaPlayer _player;
        private bool _sendPackets;
        private PlayerStatePacket _state;
        private int _animHash;
        private bool IsMoving
        {
            get
            {
                return _player.MovementContext.PlayerAnimator.Animator.GetBool(_animHash);
            }
        }

        public static BotPacketSender Create(FikaBot bot)
        {
            BotPacketSender sender = bot.gameObject.AddComponent<BotPacketSender>();
            sender._player = bot;
            sender.NetworkManager = Singleton<FikaServer>.Instance;
            sender._state = new(bot.NetId);
            sender._animHash = PlayerAnimator.INERT_PARAM_HASH;
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

        public void SendPlayerState()
        {
            if (!_sendPackets)
            {
                return;
            }

            _state.UpdateData(_player, IsMoving);
            NetworkManager.SendData(ref _state, DeliveryMethod.Unreliable);
        }

        public void DestroyThis()
        {
            NetworkManager = null;
            Destroy(this);
        }
    }
}
