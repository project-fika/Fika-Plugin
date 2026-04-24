using Fika.Core.Networking;

namespace Fika.Core.Modding.Events;

public sealed class PeerDisconnectedEvent(NetPeer peer, IFikaNetworkManager networkManager) : FikaEvent
{
    public NetPeer Peer { get; } = peer;
    public IFikaNetworkManager NetworkManager { get; } = networkManager;
}