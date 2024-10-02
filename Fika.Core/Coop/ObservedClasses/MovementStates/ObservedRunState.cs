// © 2024 Lacyway All Rights Reserved

using EFT;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedRunState : RunState
	{
		public ObservedRunState(MovementContext movementContext) : base(movementContext)
		{
			MovementContext = movementContext;
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
