using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedJumpState(MovementContext movementContext) : JumpPlayerState(movementContext)
{
    public override void ApplyMovementAndRotation(float deltaTime)
    {
        var quaternion = Quaternion.Lerp(MovementContext.TransformRotation,
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
        if (_jumpState == EJumpState.PushingFromTheGround)
        {
            if (!MovementContext.IsGrounded && _stateTime > _liftDelay)
            {
                _jumpState = EJumpState.Jump;
            }
        }
        else if (_jumpState == EJumpState.Jump)
        {
            if (_timeDescending > EFTHardSettings.Instance.JumpTimeDescendingForStateExit && MovementContext.IsGrounded)
            {
                if (_moveDirection.sqrMagnitude > 0.1f && MovementContext.CanWalk)
                {
                    MovementContext.MovementDirection = _moveDirection;
                }
            }
        }
        _stateTime += deltaTime;
        ApplyMovementAndRotation(deltaTime);
    }
}
