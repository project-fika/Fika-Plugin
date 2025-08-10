// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Fika.Core.Networking;

namespace Fika.Core.Main.PacketHandlers
{
    public class ObservedPacketSender : MonoBehaviour, IPacketSender
    {
        public bool SendState { get; set; }
        public IFikaNetworkManager NetworkManager { get; set; }

        protected void Awake()
        {
            NetworkManager = Singleton<IFikaNetworkManager>.Instance;
        }

        public void Init()
        {

        }

        public void DestroyThis()
        {
            NetworkManager = null;
            Destroy(this);
        }
    }
}
