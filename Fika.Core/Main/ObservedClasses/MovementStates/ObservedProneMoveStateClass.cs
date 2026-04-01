using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedProneMoveStateClass(MovementContext movementContext) : ObservedRunState(movementContext)
{
    public override void Rotate(Vector2 deltaRotation, bool ignoreClamp = false)
    {
        if (!ignoreClamp)
        {
            deltaRotation = ClampRotation(deltaRotation);
        }
        MovementContext.Rotation += deltaRotation;
    }

    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        base.ManualAnimatorMoveUpdate(deltaTime);
        MovementContext.AlignToSurface(deltaTime, null);
    }
}