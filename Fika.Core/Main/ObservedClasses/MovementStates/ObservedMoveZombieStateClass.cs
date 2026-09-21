using EFT;
using EFT.ZombieMovementStates;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedMoveZombieStateClass : MoveZombieState
{
    public ObservedMoveZombieStateClass(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedMoveZombieStateClass(MovementContext movementContext) : base(Il2CppInjection.Allocate<ObservedMoveZombieStateClass>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<MoveZombieState>(this, movementContext);
    }

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
