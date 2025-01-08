namespace Fika.Core.Modding.Events
{
    public class FikaGameEndedEvent : FikaEvent
    {
        public bool IsServer { get; }

        internal FikaGameEndedEvent(bool isServer)
        {
            IsServer = isServer;
        }
    }
}
