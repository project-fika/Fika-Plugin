using Fika.Core.Coop.GameMode;

namespace Fika.Core.Modding.Events
{
    public class FikaGameCreatedEvent : FikaEvent
    {
        public IFikaGame Game { get; }

        internal FikaGameCreatedEvent(IFikaGame game)
        {
            this.Game = game;
        }
    }
}
