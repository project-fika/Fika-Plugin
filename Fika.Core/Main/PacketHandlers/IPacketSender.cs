// © 2025 Lacyway All Rights Reserved

using Fika.Core.Networking;
using LiteNetLib.Utils;

namespace Fika.Core.Main.PacketHandlers
{
    public interface IPacketSender
    {
        public bool Enabled { get; set; }
        public bool SendState { get; set; }
        public FikaServer Server { get; set; }
        public FikaClient Client { get; set; }

        public void Init();
        public void SendPacket<T>(ref T packet, bool force = false) where T : INetSerializable;
        public void DestroyThis();
    }
}
