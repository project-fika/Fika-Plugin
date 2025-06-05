namespace Fika.Core.Modding.Events
{
    public class FikaRaidStartedEvent(bool isServer) : FikaEvent
    {
        public bool IsServer = isServer;
    }
}
