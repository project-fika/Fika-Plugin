// © 2024 Lacyway All Rights Reserved

using Fika.Core.Networking;
using Fika.Core.Networking.Packets;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Fika.Core.Coop.PacketHandlers
{
    public interface IPacketSender
    {
        public bool Enabled { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }
        public NetDataWriter Writer { get; set; }
        public Queue<WeaponPacket> FirearmPackets { get; set; }
        public Queue<DamagePacket> DamagePackets { get; set; }
        public Queue<ArmorDamagePacket> ArmorDamagePackets { get; set; }
        public Queue<InventoryPacket> InventoryPackets { get; set; }
        public Queue<CommonPlayerPacket> CommonPlayerPackets { get; set; }
        public Queue<HealthSyncPacket> HealthSyncPackets { get; set; }

        public void Init();
        public void SendQuestPacket(ref QuestConditionPacket packet);
        public void SendQuestItemPacket(ref QuestItemPacket packet);
        public void DestroyThis();
    }
}
