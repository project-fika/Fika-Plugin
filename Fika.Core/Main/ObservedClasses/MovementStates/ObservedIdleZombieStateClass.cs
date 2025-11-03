using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedIdleZombieStateClass(MovementContext movementContext) : IdleZombieStateClass(movementContext)
{
    public override void Move(Vector2 direction)
    {
        // do nothing
    }
}
