// © 2024 Lacyway All Rights Reserved

using LiteNetLib.Utils;
using Fika.Core.Networking;
using System.Collections.Generic;

namespace Fika.Core.Coop.PacketHandlers
{
    public interface IPacketSender
    {
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public NetDataWriter Writer { get; set; }
        public Queue<WeaponPacket> FirearmPackets { get; set; }
        public Queue<DamagePacket> HealthPackets { get; set; }
        public Queue<InventoryPacket> InventoryPackets { get; set; }
        public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; }
        public Queue<HealthSyncPacket> HealthSyncPackets { get; set; }

        public void DestroyThis();
    }
}
