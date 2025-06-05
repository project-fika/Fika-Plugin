using EFT;

namespace Fika.Core.Modding.Events
{
    public class FikaGameEndedEvent(bool isServer, ExitStatus exitStatus, string exitName) : FikaEvent
    {
        public bool IsServer { get; } = isServer;
        public ExitStatus ExitStatus { get; } = exitStatus;
        public string ExitName { get; } = exitName;
    }
}
