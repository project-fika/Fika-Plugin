using Fika.Core.Networking;
using LiteNetLib;

namespace Fika.Core.Modding.Events
{
    public class PeerConnectedEvent : FikaEvent
    {
        public NetPeer Peer { get; }
        public IFikaNetworkManager NetworkManager { get; }

        internal PeerConnectedEvent(NetPeer peer, IFikaNetworkManager networkManager)
        {
            Peer = peer;
            NetworkManager = networkManager;
        }
    }
}
