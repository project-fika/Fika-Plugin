using EFT;
using UnityEngine;

namespace Fika.Core.Main.ObservedClasses.MovementStates
{
    public class ObservedJumpState(MovementContext movementContext) : JumpStateClass(movementContext)
    {
        public override void ApplyMovementAndRotation(float deltaTime)
        {
            Quaternion quaternion = Quaternion.Lerp(MovementContext.TransformRotation,
                Quaternion.AngleAxis(MovementContext.Yaw, Vector3.up),
                EFTHardSettings.Instance.TRANSFORM_ROTATION_LERP_SPEED * deltaTime);
            MovementContext.ApplyRotation(quaternion);
            MovementContext.PlayerAnimatorSetAimAngle(MovementContext.Pitch);
        }

        public override void Exit(bool toSameState)
        {
            MovementContext.GrounderSetActive(true);
            MovementContext.LeftStanceController.SetAnimatorLeftStanceToCacheFromBodyAction(false);
        }

        public override void ManualAnimatorMoveUpdate(float deltaTime)
        {
            if (EjumpState_0 == EJumpState.PushingFromTheGround)
            {
                if (!MovementContext.IsGrounded && Float_2 > Float_3)
                {
                    EjumpState_0 = EJumpState.Jump;
                }
            }
            else if (EjumpState_0 == EJumpState.Jump)
            {
                if (Float_10 > EFTHardSettings.Instance.JumpTimeDescendingForStateExit && MovementContext.IsGrounded)
                {
                    if (Vector2_0.sqrMagnitude > 0.1f && MovementContext.CanWalk)
                    {
                        MovementContext.MovementDirection = Vector2_0;
                    }
                }
            }
            Float_2 += deltaTime;
            ApplyMovementAndRotation(deltaTime);
        }
    }
}
