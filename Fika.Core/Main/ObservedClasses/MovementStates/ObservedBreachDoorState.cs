using System;
using EFT;
using EFT.Interactive;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedBreachDoorState(MovementContext movementContext) : BreachDoorState(movementContext)
{
    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        if (NormalizedTime > 0.15f)
        {
            ProcessAnimatorMovement(deltaTime);
        }
        if (NormalizedTime < KickTime)
        {
            return;
        }
        if (_hit)
        {
            return;
        }
        _hit = true;
        if (MovementContext.NextBreachResult)
        {
            _door.KickOpen(MovementContext.TransformPosition, false);
        }
        else
        {
            _door.FailBreach(MovementContext.TransformPosition);
        }
        MovementContext.OnBreach();
    }

    public override void ExecuteDoorInteraction(WorldInteractiveObject interactive, InteractionResult interactionResult, Action callback, Player user)
    {
        // Do nothing
    }
}
