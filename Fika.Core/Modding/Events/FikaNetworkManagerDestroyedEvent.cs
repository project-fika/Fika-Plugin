using Fika.Core.Networking;

namespace Fika.Core.Modding.Events;

public sealed class FikaNetworkManagerDestroyedEvent(IFikaNetworkManager server) : FikaEvent
{
    public IFikaNetworkManager Manager { get; } = server;
}
