using Fika.Core.Main.GameMode;

namespace Fika.Core.Modding.Events;

public class FikaGameCreatedEvent(IFikaGame game) : FikaEvent
{
    public IFikaGame Game { get; } = game;
}
