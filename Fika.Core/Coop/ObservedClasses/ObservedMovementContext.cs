// © 2024 Lacyway All Rights Reserved

using Diz.LanguageExtensions;
using EFT;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedMovementContext : MovementContext
	{
		public override bool CanJump
		{
			get
			{
				return true;
			}
		}
		public override bool CanMoveInProne
		{
			get
			{
				return true;
			}
		}
		public override bool CanProne
		{
			get
			{
				return true;
			}
		}
		public override bool CanSprint
		{
			get
			{
				return true;
			}
		}
		public override bool CanWalk
		{
			get
			{
				return true;
			}
		}
		public override Error CanInteract
		{
			get
			{
				return null;
			}
		}

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

		public override bool IsAbleToRotateProne(Vector3 motion, float deltaYaw, Quaternion predictionRotation, Transform pivot, out ECantRotate cause)
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
			return name switch
			{
				EPlayerState.Run => new ObservedRunState(this),
				EPlayerState.Sprint => new ObservedSprintState(this),
				EPlayerState.Stationary => new ObservedStationaryState(this),
				EPlayerState.IdleWeaponMounting => new ObservedMountedState(this, _player),
				EPlayerState.Jump => new ObservedJumpState(this),
				_ => base.GetNewState(name, isAI)
			};
		}

		public new static ObservedMovementContext Create(Player player, Func<IAnimator> animatorGetter, Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
		{
			ObservedMovementContext movementContext = Create<ObservedMovementContext>(player, animatorGetter, characterControllerGetter, groundMask);
			return movementContext;
		}

		public override void SetStationaryWeapon(Action<Player.AbstractHandsController, Player.AbstractHandsController> callback)
		{
			StationaryHandler handler = new(this, callback);
			if (_player.HandsController.Item == StationaryWeapon.Item)
			{
				handler.callback(null, _player.HandsController);
				return;
			}
			OnHandsControllerChanged += handler.HandleSwap;
		}

		public override void DropStationary(GStruct177.EStationaryCommand command)
		{
			if (command is GStruct177.EStationaryCommand.Leave)
			{
				PlayerAnimatorSetStationary(false);
				RotationAction = DefaultRotationFunction;
			}
		}

		public override void Init()
		{
			base.Init();
			RotationAction = Rotate;
		}

		private void Rotate(Player player)
		{
			if (player.HandsController != null)
			{
				Quaternion handsRotation = Quaternion.Euler(Pitch, Yaw, 0);
				player.HandsController.ControllerGameObject.transform.SetPositionAndRotation(player.PlayerBones.Ribcage.Original.position, handsRotation);
				player.CameraContainer.transform.rotation = handsRotation;
			}
		}

		public void ObservedStartExitingMountedState()
		{
			if (OverridenControlsState is not ObservedMountedState observedMountedState)
			{
				return;
			}
			_player.ProceduralWeaponAnimation.SetMountingData(false, false);
			observedMountedState.StartExiting();
			PlayerMountingPointData.OnStartExitMountedState -= StartExitingMountedState;
			Player.AbstractHandsController handsController = _player.HandsController;
			if (handsController == null)
			{
				return;
			}
			handsController.FirearmsAnimator.SetMounted(false);
		}

		private class StationaryHandler(MovementContext context, Action<Player.AbstractHandsController, Player.AbstractHandsController> callback)
		{
			private readonly MovementContext context = context;
			public readonly Action<Player.AbstractHandsController, Player.AbstractHandsController> callback = callback;

			public void HandleSwap(Player.AbstractHandsController oldController, Player.AbstractHandsController newController)
			{
				if (newController is not CoopObservedFirearmController)
				{
					return;
				}

				if (newController != null && newController is CoopObservedFirearmController observedController && observedController.Item == context.StationaryWeapon.Item)
				{
					context.OnHandsControllerChanged -= HandleSwap;
					callback(null, newController);
				}
			}
		}
	}
}
