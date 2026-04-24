using EFT;

namespace Fika.Core.Modding.Events;

public sealed class GameWorldStartedEvent(GameWorld gameWorld) : FikaEvent
{
    public GameWorld GameWorld { get; } = gameWorld;
}
