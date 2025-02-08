// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class HeadlessPacketSender : MonoBehaviour, IPacketSender
    {
        private CoopPlayer player;

        public bool Enabled { get; set; } = false;
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public Queue<IQueuePacket> PacketQueue { get; set; }

        protected void Awake()
        {
            player = GetComponent<CoopPlayer>();
            Server = Singleton<FikaServer>.Instance;
            enabled = false;
            PacketQueue = new();
        }

        public void Init()
        {
            enabled = true;
            Enabled = true;
        }

        public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable
        {
            Server.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
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
