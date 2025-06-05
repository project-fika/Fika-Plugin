using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
    public class FikaNetworkManagerCreatedEvent(IFikaNetworkManager manager) : FikaEvent
    {
        public IFikaNetworkManager Manager { get; } = manager;
    }
}
