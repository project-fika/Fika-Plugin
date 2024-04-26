using EFT;

namespace Fika.Core.Modding.Events
{
    public class GameWorldStartedEvent : FikaEvent
    {
        public GameWorld GameWorld { get; }

        internal GameWorldStartedEvent(GameWorld gameWorld)
        {
            this.GameWorld = gameWorld;
        }
    }
}
