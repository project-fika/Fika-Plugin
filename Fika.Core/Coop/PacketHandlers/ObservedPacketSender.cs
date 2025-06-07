// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class ObservedPacketSender : MonoBehaviour, IPacketSender
    {
        private CoopPlayer _player;
        private bool _isServer;
        public bool Enabled { get; set; }
        public bool SendState { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }

        protected void Awake()
        {
            _player = GetComponent<ObservedCoopPlayer>();
            _isServer = FikaBackendUtils.IsServer;
            if (_isServer)
            {
                Server = Singleton<FikaServer>.Instance;
            }
            else
            {
                Client = Singleton<FikaClient>.Instance;
            }
            Enabled = true;
        }

        public void Init()
        {

        }

        public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
        {
            if (!enabled)
            {
                return;
            }

            if (_isServer)
            {
                Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                return;
            }

            Client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
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
