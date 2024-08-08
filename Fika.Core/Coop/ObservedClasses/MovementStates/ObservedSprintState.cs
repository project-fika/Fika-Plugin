// © 2024 Lacyway All Rights Reserved

using EFT;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.MovementStates
{
	internal class ObservedSprintState : GClass1719
	{
		public ObservedSprintState(MovementContext movementContext) : base(movementContext)
		{
			MovementContext = (ObservedMovementContext)movementContext;
		}

		public override void UpdatePosition(float deltaTime)
		{
			if (!MovementContext.IsGrounded)
			{
				MovementContext.PlayerAnimatorEnableFallingDown(true);
			}
		}

		public override void EnableSprint(bool enabled, bool isToggle = false)
		{
			MovementContext.EnableSprint(enabled);
		}

		public override void ChangePose(float poseDelta)
		{
			MovementContext.SetPoseLevel(MovementContext.PoseLevel + poseDelta, false);
		}

		public override void ManualAnimatorMoveUpdate(float deltaTime)
		{
			if (MovementContext.IsSprintEnabled)
			{
				/*MovementContext.ProcessSpeedLimits(deltaTime);*/
				MovementContext.MovementDirection = Vector2.Lerp(MovementContext.MovementDirection, Direction, deltaTime * EFTHardSettings.Instance.DIRECTION_LERP_SPEED);
				//MovementContext.ApplyRotation(Quaternion.AngleAxis(MovementContext.Yaw, Vector3.up));
				MovementContext.SetUpDiscreteDirection(GClass1669.ConvertToMovementDirection(Direction));
				Direction = Vector2.zero;
				MovementContext.SprintAcceleration(deltaTime);
				UpdateRotationAndPosition(deltaTime);
			}
			else
			{
				MovementContext.PlayerAnimatorEnableSprint(false, false);
			}
			if (!MovementContext.PlayerAnimator.Animator.IsInTransition(0))
			{
				MovementContext.ObstacleCollisionFacade.RecalculateCollision(velocityThreshold);
			}
		}
	}
}
