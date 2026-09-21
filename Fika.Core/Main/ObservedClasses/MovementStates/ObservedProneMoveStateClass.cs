using EFT;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedProneMoveStateClass : ObservedRunState
{
    public ObservedProneMoveStateClass(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedProneMoveStateClass(MovementContext movementContext) : base(Il2CppInjection.Allocate<ObservedProneMoveStateClass>(), movementContext)
    {
    }

    public override void Rotate(Vector2 deltaRotation, bool ignoreClamp = false)
    {
        if (!ignoreClamp)
        {
            deltaRotation = ClampRotation(deltaRotation);
        }
        MovementContext.Rotation += deltaRotation;
    }

    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        base.ManualAnimatorMoveUpdate(deltaTime);
        MovementContext.AlignToSurface(deltaTime, new Il2CppSystem.Nullable<Vector3>());
    }
}