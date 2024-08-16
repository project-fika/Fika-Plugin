// © 2024 Lacyway All Rights Reserved

using EFT;

namespace Fika.Core.Coop.ObservedClasses.MovementStates
{
	internal class ObservedRunState : GClass1717
	{
		public ObservedRunState(MovementContext movementContext) : base(movementContext)
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
	}
}
