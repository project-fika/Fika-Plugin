using EFT;

namespace Fika.Core.Modding.Events
{
    public class FikaGameEndedEvent : FikaEvent
    {
        public bool IsServer { get; }
        public ExitStatus ExitStatus { get; }
        public string ExitName { get; }

        internal FikaGameEndedEvent(bool isServer, ExitStatus exitStatus, string exitName)
        {
            IsServer = isServer;
            ExitStatus = exitStatus;
            ExitName = exitName;
        }
    }
}
