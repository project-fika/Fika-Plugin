// © 2024 Lacyway All Rights Reserved

using Diz.LanguageExtensions;
using EFT;
using Fika.Core.Coop.ObservedClasses.MovementStates;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedMovementContext : MovementContext
	{
		public override bool CanJump => true;
		public override bool CanMoveInProne => true;
		public override bool CanProne => true;
		public override bool CanSprint => true;
		public override bool CanWalk => true;
		public override Error CanInteract => null;
		public override bool StateLocksInventory { set { } }

		public override void ApplyApproachMotion(Vector3 motion, float deltaTime)
		{
			base.DirectApplyMotion(motion, deltaTime);
		}

		public override void Flash(ref Vector3 motion)
		{
			// Do nothing
		}

		public override void LimitMotionXZ(ref Vector3 motion, float deltaTime, float threshold = 0.0001F)
		{
			InputMotionBeforeLimit = motion / deltaTime;
		}

		public override void LimitProneMotion(ref Vector3 motion)
		{
			// Do nothing
		}

		public override void ApplyGravity(ref Vector3 motion, float deltaTime, bool stickToGround)
		{
			// Do nothing
		}

		public override bool CanRoll(int direction)
		{
			return true;
		}

		public override bool CanStandAt(float h)
		{
			return true;
		}

		public override bool HasGround(float depth, Vector3? axis = null, float extraCastLn = 0)
		{
			return true;
		}

		public override bool HeadBump(float velocity)
		{
			return false;
		}

		public override void OnControllerColliderHit(ControllerColliderHit hit)
		{
			// Do nothing
		}

		public override bool OverlapOrHasNoGround(float depth, Vector3? axis = null, float width = 0, float heightDivider = 4, float extraCastLn = 0)
		{
			return false;
		}

		public override void ProjectMotionToSurface(ref Vector3 motion)
		{
			// Do nothing
		}

		public override bool IsAbleToRotate(Vector3 motion, float deltaYaw, Quaternion predictionRotation, Transform pivot, out ECantRotate cause)
		{
			cause = ECantRotate.NotGround;
			return true;
		}

		public override void SetCharacterMovementSpeed(float characterMovementSpeed, bool force = false)
		{
			CharacterMovementSpeed = characterMovementSpeed;
			SmoothedCharacterMovementSpeed = characterMovementSpeed;
			UpdateCovertEfficiency(characterMovementSpeed, false);
		}

		/*public override void ManualUpdate(float deltaTime)
        {
            if (!_player.HealthController.IsAlive)
            {
                return;
            }
            LastDeltaTime = deltaTime;
            UpdateGroundCollision(deltaTime);
            SmoothPoseLevel(deltaTime);
            if (_player.Physical.Sprinting)
            {
                PreSprintAcceleration(deltaTime);
            }
            method_13(deltaTime);
            if (Math.Abs(Tilt) > 0)
            {
                _player.ProceduralWeaponAnimation.UpdatePossibleTilt(SmoothedCharacterMovementSpeed, SmoothedPoseLevel);
            }
        }*/

		public override void SmoothPitchLimitations(float deltaTime)
		{
			// Do nothing
		}

		public override void ProcessSpeedLimits(float deltaTime)
		{
			// Do nothing
		}

		public override void UpdateGroundCollision(float deltaTime)
		{
			if (!_player.IsVisible)
			{
				return;
			}
			float num = 1f;
			if (IsGrounded)
			{
				num = 0f;
				FreefallTime = deltaTime;
			}
			else
			{
				FreefallTime += deltaTime;
			}
			PlayerAnimatorSetFallingDownFloat(num);
			PlayerAnimator.SetIsGrounded(IsGrounded);
			CheckFlying(deltaTime);
		}

		public override void WeightRelatedValuesUpdated()
		{
			PlayerAnimatorTransitionSpeed = TransitionSpeed;
			UpdateCovertEfficiency(ClampedSpeed, true);
			_player.UpdateStepSoundRolloff();
			TiltInertia = EFTHardSettings.Instance.InertiaTiltCurve.Evaluate(_player.Physical.Inertia);
			WalkInertia = InertiaSettings.WalkInertia.Evaluate(_player.Physical.Inertia);
			SprintBrakeInertia = InertiaSettings.SprintBrakeInertia.Evaluate(_player.Physical.Inertia);
		}

		public override BaseMovementState GetNewState(EPlayerState name, bool isAI = false)
		{
			if (name == EPlayerState.Run)
				return new ObservedRunState(this);
			if (name == EPlayerState.Sprint)
				return new ObservedSprintState(this);
			else
				return base.GetNewState(name, isAI);
		}

		public new static ObservedMovementContext Create(Player player, Func<IAnimator> animatorGetter, Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
		{
			ObservedMovementContext movementContext = Create<ObservedMovementContext>(player, animatorGetter, characterControllerGetter, groundMask);
			return movementContext;
		}
	}
}
