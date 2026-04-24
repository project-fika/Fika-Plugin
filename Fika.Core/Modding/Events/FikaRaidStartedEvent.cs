namespace Fika.Core.Modding.Events;

public sealed class FikaRaidStartedEvent(bool isServer) : FikaEvent
{
    public bool IsServer = isServer;
}
