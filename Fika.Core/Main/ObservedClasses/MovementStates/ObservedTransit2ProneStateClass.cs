using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

internal class ObservedTransit2ProneStateClass(MovementContext movementContext) : ObservedIdleStateClass(movementContext)
{
    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        // do nothing
    }

    public override void ProcessAnimatorMovement(float deltaTime)
    {
        // do nothing
    }
}
