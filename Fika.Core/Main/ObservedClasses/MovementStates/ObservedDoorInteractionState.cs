using System;
using EFT;
using EFT.Interactive;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedDoorInteractionState : DoorInteractState
{
    public ObservedDoorInteractionState(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedDoorInteractionState(MovementContext movementContext) : base(Il2CppInjection.Allocate<ObservedDoorInteractionState>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<DoorInteractState>(this, movementContext);
    }

    public override void Enter(bool isFromSameState)
    {
        MovementContext.RestoreDefaultAlignment(1f);
        base.Enter(isFromSameState);
    }

    public override void ExecuteInteraction()
    {
        Door.SetUser(MovementContext._player);
        Door.Interact(MovementContext.InteractionInfo.Result);
    }

    public override void ExecuteDoorInteraction(WorldInteractiveObject door, InteractionResult interactionResult, Il2CppSystem.Action callback, Player user)
    {
        // Do nothing
    }
}
