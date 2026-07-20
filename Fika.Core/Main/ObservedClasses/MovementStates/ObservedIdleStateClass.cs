using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedIdleStateClass(MovementContext movementContext) : IdlePlayerState(movementContext)
{
    public override void Move(Vector2 direction)
    {
        // do nothing
    }
}
