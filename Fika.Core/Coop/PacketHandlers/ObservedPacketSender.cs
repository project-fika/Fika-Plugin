// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Coop.Matchmaker;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
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
        public bool Enabled { get; set; } = true;
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public NetDataWriter Writer { get; set; } = new();
        public Queue<WeaponPacket> FirearmPackets { get; set; } = new(50);
        public Queue<DamagePacket> DamagePackets { get; set; } = new(50);
        public Queue<InventoryPacket> InventoryPackets { get; set; } = new(50);
        public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; } = new(50);
        public Queue<HealthSyncPacket> HealthSyncPackets { get; set; } = new(50);

        protected void Awake()
        {
            player = GetComponent<ObservedCoopPlayer>();
            isServer = MatchmakerAcceptPatches.IsServer;
            if (isServer)
            {
                Server = Singleton<FikaServer>.Instance;
            }
            else
            {
                Client = Singleton<FikaClient>.Instance;
            }
        }

        protected void Update()
        {
            if (player == null || Writer == null)
            {
                return;
            }

            if (DamagePackets.Count > 0)
            {
                if (isServer)
                {
                    int healthPackets = DamagePackets.Count;
                    for (int i = 0; i < healthPackets; i++)
                    {
                        DamagePacket healthPacket = DamagePackets.Dequeue();
                        healthPacket.NetId = player.NetId;

                        Writer.Reset();
                        Server.SendDataToAll(Writer, ref healthPacket, DeliveryMethod.ReliableOrdered);
                    }
                }
                else
                {
                    int healthPackets = DamagePackets.Count;
                    for (int i = 0; i < healthPackets; i++)
                    {
                        DamagePacket healthPacket = DamagePackets.Dequeue();
                        healthPacket.NetId = player.NetId;

                        Writer.Reset();
                        Client.SendData(Writer, ref healthPacket, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        public void DestroyThis()
        {
            Writer = null;
            FirearmPackets.Clear();
            DamagePackets.Clear();
            InventoryPackets.Clear();
            CommonPlayerPackets.Clear();
            HealthSyncPackets.Clear();
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
