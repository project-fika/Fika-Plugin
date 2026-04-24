using EFT;

namespace Fika.Core.Modding.Events;

public sealed class AbstractGameCreatedEvent(AbstractGame game) : FikaEvent
{
    public AbstractGame Game { get; } = game;
}
