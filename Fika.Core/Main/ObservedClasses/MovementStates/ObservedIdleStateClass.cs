using EFT;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedIdleStateClass : IdlePlayerState
{
    public ObservedIdleStateClass(IntPtr pointer) : base(pointer)
    {
    }

    protected ObservedIdleStateClass(IntPtr pointer, MovementContext movementContext) : base(pointer)
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<IdlePlayerState>(this, movementContext);
    }

    public ObservedIdleStateClass(MovementContext movementContext) : this(Il2CppInjection.Allocate<ObservedIdleStateClass>(), movementContext)
    {
    }

    public override void Move(Vector2 direction)
    {
        // do nothing
    }
}
