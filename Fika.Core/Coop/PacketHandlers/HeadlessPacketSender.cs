// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class HeadlessPacketSender : MonoBehaviour, IPacketSender
    {
        private CoopPlayer player;

        public bool Enabled { get; set; } = false;
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }

        protected void Awake()
        {
            player = GetComponent<CoopPlayer>();
            Server = Singleton<FikaServer>.Instance;
            enabled = false;
        }

        public void Init()
        {
            enabled = true;
            Enabled = true;
        }

        public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
        {
            Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
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
