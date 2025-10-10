using EFT;

namespace Fika.Core.Modding.Events;

public class AbstractGameCreatedEvent(AbstractGame game) : FikaEvent
{
    public AbstractGame Game { get; } = game;
}
