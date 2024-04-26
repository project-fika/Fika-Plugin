using EFT;

namespace Fika.Core.Modding.Events
{
    public class AbstractGameCreatedEvent : FikaEvent
    {
        public AbstractGame Game { get; }

        internal AbstractGameCreatedEvent(AbstractGame game)
        {
            this.Game = game;
        }
    }
}
