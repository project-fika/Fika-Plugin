// © 2025 Lacyway All Rights Reserved

using Fika.Core.Networking;

namespace Fika.Core.Main.PacketHandlers
{
    public interface IPacketSender
    {
        public bool SendState { get; set; }
        public IFikaNetworkManager NetworkManager { get; set; }

        public void Init();
        public void DestroyThis();
    }
}
