using EFT;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    public class ClientMovementContext : MovementContext
    {
        private bool doGravity;

        public new static ClientMovementContext Create(Player player, Func<IAnimator> animatorGetter, Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
        {
            ClientMovementContext movementContext = Create<ClientMovementContext>(player, animatorGetter, characterControllerGetter, groundMask);
            return movementContext;
        }

        public override void Init()
        {
            doGravity = true;
            base.Init();
        }

        public override void ApplyGravity(ref Vector3 motion, float deltaTime, bool stickToGround)
        {
            if (!doGravity)
            {
                return;
            }

            base.ApplyGravity(ref motion, deltaTime, stickToGround);
        }

        public void SetGravity(bool enabled)
        {
            doGravity = enabled;
        }
    }
}
