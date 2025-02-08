// © 2025 Lacyway All Rights Reserved

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
        public Queue<IQueuePacket> PacketQueue { get; set; }

        public void Init();
        public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable;
        public void DestroyThis();
    }
}
