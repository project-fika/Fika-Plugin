using EFT;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
    public class DedicatedMovementContext : MovementContext
    {
        public override void ApplyGravity(ref Vector3 motion, float deltaTime, bool stickToGround)
        {
            // Do nothing
        }

        public new static DedicatedMovementContext Create(Player player, Func<IAnimator> animatorGetter, Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
        {
            DedicatedMovementContext movementContext = Create<DedicatedMovementContext>(player, animatorGetter, characterControllerGetter, groundMask);
            return movementContext;
        }
    }
}
