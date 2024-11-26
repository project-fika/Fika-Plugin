using EFT;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedJumpState(MovementContext movementContext) : JumpState(movementContext)
	{
		public override void ApplyMovementAndRotation(float deltaTime)
		{
			Quaternion quaternion = Quaternion.Lerp(MovementContext.TransformRotation, Quaternion.AngleAxis(MovementContext.Yaw, Vector3.up), EFTHardSettings.Instance.TRANSFORM_ROTATION_LERP_SPEED * deltaTime);
			MovementContext.ApplyRotation(quaternion);
			MovementContext.PlayerAnimatorSetAimAngle(MovementContext.Pitch);
		}
	}
}
