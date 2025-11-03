using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedMoveZombieStateClass(MovementContext movementContext) : MoveZombieStateClass(movementContext)
{
    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        if (Direction != Vector2.zero)
        {
            Direction = Vector2.zero;
            Float_0 = 0f;
        }
        Float_0 += deltaTime;
    }

    public override void UpdateRotationAndPosition(float deltaTime)
    {
        // do nothing
    }
}
