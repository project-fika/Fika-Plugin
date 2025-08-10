using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
    public class PeerConnectedEvent(NetPeer peer, IFikaNetworkManager networkManager) : FikaEvent
    {
        public NetPeer Peer { get; } = peer;
        public IFikaNetworkManager NetworkManager { get; } = networkManager;
    }
}
