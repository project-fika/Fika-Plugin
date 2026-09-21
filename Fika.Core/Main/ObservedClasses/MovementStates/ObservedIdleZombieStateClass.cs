using EFT;
using EFT.ZombieMovementStates;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedIdleZombieStateClass : IdleZombieState
{
    public ObservedIdleZombieStateClass(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedIdleZombieStateClass(MovementContext movementContext) : base(Il2CppInjection.Allocate<ObservedIdleZombieStateClass>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<IdleZombieState>(this, movementContext);
    }

    public override void Move(Vector2 direction)
    {
        // do nothing
    }
}
