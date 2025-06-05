using EFT;

namespace Fika.Core.Modding.Events
{
    public class GameWorldStartedEvent(GameWorld gameWorld) : FikaEvent
    {
        public GameWorld GameWorld { get; } = gameWorld;
    }
}
