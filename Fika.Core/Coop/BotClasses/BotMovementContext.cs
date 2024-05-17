using EFT;
using Fika.Core.Coop.Players;
using System;
using UnityEngine;

namespace Fika.Core.Coop.BotClasses
{
    public sealed class BotMovementContext : MovementContext
    {
        private CoopBot Bot;

        public override void ApplyGravity(ref Vector3 motion, float deltaTime, bool stickToGround)
        {
            if (Bot.AIData.BotOwner.BotState == EBotState.NonActive)
            {
                return;
            }

            base.ApplyGravity(ref motion, deltaTime, stickToGround);
        }

        public new static BotMovementContext Create(Player player, Func<IAnimator> animatorGetter, Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
        {
            BotMovementContext movementContext = Create<BotMovementContext>(player, animatorGetter, characterControllerGetter, groundMask);
            movementContext.Bot = (CoopBot)player;
            return movementContext;
        }
    }
}
