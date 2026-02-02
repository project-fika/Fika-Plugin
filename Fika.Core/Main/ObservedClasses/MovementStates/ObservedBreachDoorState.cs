using System;
using EFT;
using EFT.Interactive;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedBreachDoorState(MovementContext movementContext) : BreachDoorStateClass(movementContext)
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
        if (Bool_0)
        {
            return;
        }
        Bool_0 = true;
        if (MovementContext.NextBreachResult)
        {
            Door_0.KickOpen(MovementContext.TransformPosition, false);
        }
        else
        {
            Door_0.FailBreach(MovementContext.TransformPosition);
        }
        MovementContext.OnBreach();
    }

    public override void ExecuteDoorInteraction(WorldInteractiveObject interactive, InteractionResult interactionResult, Action callback, Player user)
    {
        // Do nothing
    }
}
