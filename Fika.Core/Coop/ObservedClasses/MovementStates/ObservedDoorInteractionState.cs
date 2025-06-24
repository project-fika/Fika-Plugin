using EFT;
using EFT.Interactive;
using System;

namespace Fika.Core.Coop.ObservedClasses.MovementStates
{
    public class ObservedDoorInteractionState(MovementContext movementContext) : DoorInteractionStateClass(movementContext)
    {
        public override void Enter(bool isFromSameState)
        {
            MovementContext.RestoreDefaultAlignment(1f);
            base.Enter(isFromSameState);
        }

        public override void ExecuteInteraction()
        {
            Door.Interact(MovementContext.InteractionInfo.Result);
        }

        public override void ExecuteDoorInteraction(WorldInteractiveObject door, InteractionResult interactionResult, Action callback, Player user)
        {
            // Do nothing
        }
    }
}
