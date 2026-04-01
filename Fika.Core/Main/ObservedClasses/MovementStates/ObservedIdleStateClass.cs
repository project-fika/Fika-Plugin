using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedIdleStateClass(MovementContext movementContext) : IdleStateClass(movementContext)
{
    public override void Move(Vector2 direction)
    {
        // do nothing
    }
}
