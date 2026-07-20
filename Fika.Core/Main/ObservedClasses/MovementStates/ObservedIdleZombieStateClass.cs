using EFT;
using EFT.ZombieMovementStates;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedIdleZombieStateClass(MovementContext movementContext) : IdleZombieState(movementContext)
{
    public override void Move(Vector2 direction)
    {
        // do nothing
    }
}
