using EFT;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

internal class ObservedTransit2ProneStateClass : ObservedIdleStateClass
{
    public ObservedTransit2ProneStateClass(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedTransit2ProneStateClass(MovementContext movementContext) : base(Il2CppInjection.Allocate<ObservedTransit2ProneStateClass>(), movementContext)
    {
    }

    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        // do nothing
    }

    public override void ProcessAnimatorMovement(float deltaTime)
    {
        // do nothing
    }
}
