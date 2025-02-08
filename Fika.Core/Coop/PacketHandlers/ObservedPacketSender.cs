// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.PacketHandlers
{
    public class ObservedPacketSender : MonoBehaviour, IPacketSender
    {
        private CoopPlayer player;
        private bool isServer;
        public bool Enabled { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public Queue<IQueuePacket> PacketQueue { get; set; }

        protected void Awake()
        {
            player = GetComponent<ObservedCoopPlayer>();
            isServer = FikaBackendUtils.IsServer;
            if (isServer)
            {
                Server = Singleton<FikaServer>.Instance;
            }
            else
            {
                Client = Singleton<FikaClient>.Instance;
            }
            Enabled = true;
            PacketQueue = new();
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

            if (isServer)
            {
                Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                return;
            }

            Client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }

        protected void LateUpdate()
        {
            if (player == null)
            {
                return;
            }

            if (isServer)
            {
                int packetAmountServer = PacketQueue.Count;
                for (int i = 0; i < packetAmountServer; i++)
                {
                    IQueuePacket packet = PacketQueue.Dequeue();
                    packet.NetId = player.NetId;
                    Server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                }
                return;
            }

            int packetAmount = PacketQueue.Count;
            for (int i = 0; i < packetAmount; i++)
            {
                IQueuePacket packet = PacketQueue.Dequeue();
                packet.NetId = player.NetId;
                Client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
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
