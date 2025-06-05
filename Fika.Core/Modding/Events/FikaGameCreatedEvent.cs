using Fika.Core.Coop.GameMode;

namespace Fika.Core.Modding.Events
{
    public class FikaGameCreatedEvent(IFikaGame game) : FikaEvent
    {
        public IFikaGame Game { get; } = game;
    }
}
