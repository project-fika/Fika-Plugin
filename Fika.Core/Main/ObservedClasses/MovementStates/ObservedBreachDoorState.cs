using System;
using EFT;
using EFT.Interactive;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedBreachDoorState : BreachDoorState
{
    public ObservedBreachDoorState(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedBreachDoorState(MovementContext movementContext) : base(Il2CppInjection.Allocate<ObservedBreachDoorState>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<BreachDoorState>(this, movementContext);
    }

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

    public override void ExecuteDoorInteraction(WorldInteractiveObject interactive, InteractionResult interactionResult, Il2CppSystem.Action callback, Player user)
    {
        // Do nothing
    }
}
