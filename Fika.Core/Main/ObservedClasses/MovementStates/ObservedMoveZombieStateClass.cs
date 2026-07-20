using EFT;
using EFT.ZombieMovementStates;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedMoveZombieStateClass(MovementContext movementContext) : MoveZombieState(movementContext)
{
    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        if (Direction != Vector2.zero)
        {
            Direction = Vector2.zero;
            _timeWithoutInput = 0f;
        }
        _timeWithoutInput += deltaTime;
    }

    public override void UpdateRotationAndPosition(float deltaTime)
    {
        // do nothing
    }
}
