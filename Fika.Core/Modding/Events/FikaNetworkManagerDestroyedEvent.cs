using Fika.Core.Networking;

namespace Fika.Core.Modding.Events;

public class FikaNetworkManagerDestroyedEvent(IFikaNetworkManager server) : FikaEvent
{
    public IFikaNetworkManager Manager { get; } = server;
}
