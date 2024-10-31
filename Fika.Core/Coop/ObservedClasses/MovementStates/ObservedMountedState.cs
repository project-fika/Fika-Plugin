using Comfort.Common;
using EFT;
using EFT.WeaponMounting;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedMountedState : MovementState
	{
		public ObservedMountedState(MovementContext movementContext, Player observedPlayer) : base(movementContext)
		{
			mountingMovementSettings = Singleton<BackendConfigSettingsClass>.Instance.MountingSettings.MovementSettings;
			player = observedPlayer;
			RotationSpeedClamp = mountingMovementSettings.RotationSpeedClamp;
			StateSensitivity = mountingMovementSettings.SensitivityMultiplier;
		}

		private readonly Player player;
		private PlayerMountingPointData playerMountingPointData;
		private readonly IMountingMovementSettings mountingMovementSettings;

		private Quaternion quaternion_0;
		private Vector2 vector2_0;
		private Vector3 vector3_0;

		private int int_0;

		private bool bool_0;
		private bool bool_1;

		private float float_4;
		private float float_5;
		private float float_6;
		private float float_7;
		private float float_8;
		private float float_9;
		private float float_10;
		private float float_11;
		private float float_12;

		private float PitchLimitX
		{
			get
			{
				return playerMountingPointData.PitchLimit.x;
			}
		}
		private float PitchLimitY
		{
			get
			{
				return float_12;
			}
		}

		public override void BlindFire(int b)
		{

		}

		public override void ChangePose(float poseDelta)
		{

		}

		public override void ChangeSpeed(float speedDelta)
		{

		}

		public override void EnableBreath(bool enable)
		{
			MovementContext.HoldBreath(enable);
		}

		public override void EnableSprint(bool enable, bool isToggle = false)
		{

		}

		public override void Enter(bool isFromSameState)
		{
			base.Enter(isFromSameState);
			player.CurrentLeanType = Player.LeanType.SlowLean;
			playerMountingPointData = MovementContext.PlayerMountingPointData;
			float_12 = playerMountingPointData.PitchLimit.y;
			float_7 = MovementContext.SmoothedTilt;
			MovementContext.SetTilt(0f, false);
			MovementContext.Step = 0;
			MovementContext.MountedSmoothedTilt = 0f;
			MovementContext.MountedSmoothedTiltForCamera = 0f;
			if (MovementContext.LeftStanceController.LeftStance)
			{
				MovementContext.LeftStanceController.ToggleLeftStance();
			}
			if (Mathf.Abs(player.ProceduralWeaponAnimation.CurrentScope.Rotation) > EFTHardSettings.Instance.SCOPE_ROTATION_THRESHOLD)
			{
				MovementContext.ToggleFirearmAimByBodyAction();
			}
			vector3_0 = MovementContext.TransformPosition;
			vector2_0 = MovementContext.Rotation;
			quaternion_0 = MovementContext.TransformRotation;
			float_4 = 0f;
			float_10 = 0f;
			float_6 = playerMountingPointData.TargetHandsRotation;
			float_9 = 0f;
			float_8 = 0f;
			int_0 = (int)playerMountingPointData.MountPointData.MountSideDirection;
			float_11 = Mathf.Abs(Mathf.DeltaAngle(playerMountingPointData.YawLimit.x, playerMountingPointData.YawLimit.y));
			if (playerMountingPointData.MountPointData.MountSideDirection == EMountSideDirection.Forward)
			{
				float_11 /= 2f;
			}
			if (playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
			{
				float num = MovementContext.Yaw;
				if (num < -360f)
				{
					num += 360f;
				}
				if (num > 360f)
				{
					num -= 360f;
				}
				float num2 = ((num > 0f) ? (num - 360f) : (num + 360f));
				if (num < playerMountingPointData.YawLimit.x || num > playerMountingPointData.YawLimit.y)
				{
					num = num2;
				}
				if (num >= playerMountingPointData.YawLimit.x && num <= playerMountingPointData.YawLimit.y)
				{
					float_6 = MovementContext.Yaw;
				}
				else
				{
					float_6 = ((MovementContext.Yaw < playerMountingPointData.YawLimit.x) ? playerMountingPointData.YawLimit.x : playerMountingPointData.YawLimit.y);
				}
				float_9 = (Mathf.Approximately(playerMountingPointData.TargetHandsRotation, playerMountingPointData.YawLimit.x)
					? (float_6 - playerMountingPointData.YawLimit.x) : (float_6 - playerMountingPointData.YawLimit.y));
				float num3 = (((float)int_0 * float_9 < 0f) ? ((float)int_0) : ((int_0 == 0) ? (-float_9) : 0f));
				float num4 = Mathf.Abs(float_9) / float_11;
				float_8 = Mathf.Sign(num3) * Mathf.Lerp(0f, 5f, num4);
			}
			playerMountingPointData.TransitionMounting = true;
			playerMountingPointData.TransitionProgress = 0f;
			MovementContext.method_25();
			MovementContext.RotationAction = MovementContext.MountingRotationFunction;
			MovementContext.SetPitchSmoothly(PitchLimitX, PitchLimitY);
			bool_0 = MovementContext.CanUseProp.Value;
			bool_1 = false;
			player.OnMounting(GStruct179.EMountingCommand.Enter);
			float_5 = ((playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward || MovementContext.IsInPronePose)
				? ((MovementContext.Pitch < MovementContext.PitchLimit.x) ? PitchLimitX : ((MovementContext.Pitch > MovementContext.PitchLimit.y) ? MovementContext.PitchLimit.y : MovementContext.Pitch)) : 0f);
			float num5 = Mathf.InverseLerp(PitchLimitX, PitchLimitY, float_5);
			float num6 = Mathf.Lerp(playerMountingPointData.PoseLimit.x, playerMountingPointData.PoseLimit.y, num5);
			MovementContext.SetPoseLevel(num6, false);
			Action<float> onEnterMountedState = playerMountingPointData.OnEnterMountedState;
			if (onEnterMountedState == null)
			{
				return;
			}
			onEnterMountedState(playerMountingPointData.CurrentApproachTime);
		}

		public override void Exit(bool toSameState)
		{
			base.Exit(toSameState);
			if (playerMountingPointData.MountPointData.MountSideDirection == EMountSideDirection.Forward)
			{
				MovementContext.SetTilt(0f, false);
			}
			else
			{
				MovementContext.SetTilt(MovementContext.MountedSmoothedTilt, true);
				MovementContext.PlayerAnimatorSetTilt(MovementContext.MountedSmoothedTilt);
			}
			MovementContext.IgnoreDeltaMovement = false;
			MovementContext.MountedSmoothedTilt = 0f;
			MovementContext.MountedSmoothedTiltForCamera = 0f;
			MovementContext.SetYawLimit(Player.GClass1738.FULL_YAW_RANGE);
			MovementContext.SetPitchSmoothly(MovementContext.IsInPronePose ? Player.GClass1738.PRONE_POSE_ROTATION_PITCH_RANGE : Player.GClass1738.STAND_POSE_ROTATION_PITCH_RANGE);
			MovementContext.RotationAction = MovementContext.DefaultRotationFunction;
			MovementContext.CanUseProp.Value = bool_0;
			player.ProceduralWeaponAnimation.SetStrategy(EPointOfView.ThirdPerson);
			Action<float> onExitMountedState = playerMountingPointData.OnExitMountedState;
			if (onExitMountedState == null)
			{
				return;
			}
			onExitMountedState(0f);
		}

		public override void Jump()
		{

		}

		public override void ManualAnimatorMoveUpdate(float deltaTime)
		{
			MovementContext.CanUseProp.Value = false;
			if (bool_1)
			{
				if (float_4 > mountingMovementSettings.ExitTime)
				{
					MovementContext.ExitMountedState();
				}
				if (float_4 <= mountingMovementSettings.ExitTime)
				{
					float_4 += deltaTime * Time.timeScale;
					playerMountingPointData.TransitionProgress = 1f - float_4 / mountingMovementSettings.ExitTime;
				}
				return;
			}
			if (float_4 <= playerMountingPointData.CurrentApproachTime)
			{
				UpdateApproach(deltaTime * Time.timeScale);
				player.OnMounting(GStruct179.EMountingCommand.Update);
				return;
			}
			MovementContext.SetYawLimit(playerMountingPointData.YawLimit);
			MovementContext.SetPitchSmoothly(PitchLimitX, PitchLimitY);
			UpdateState();
			player.OnMounting(GStruct179.EMountingCommand.Update);
		}

		public void UpdateState()
		{
			float num = Mathf.Abs(float_10) / float_11;
			if (!MovementContext.IsInPronePose)
			{
				SetRotation(num);
				SetPose();
			}
			if (int_0 != 0)
			{
				playerMountingPointData.CurrentMountingPointVerticalOffset = -0.1f * num;
			}
		}

		public void SetPose()
		{
			float num = Mathf.InverseLerp(PitchLimitX, PitchLimitY, MovementContext.Pitch);
			float num2 = Mathf.Lerp(playerMountingPointData.PoseLimit.x, playerMountingPointData.PoseLimit.y, num);
			MovementContext.SetPoseLevel(num2, false);
		}

		public void UpdateApproach(float deltaTime)
		{
			MovementContext.IgnoreDeltaMovement = true;
			float_4 += deltaTime;
			float num = float_4 / playerMountingPointData.CurrentApproachTime;
			playerMountingPointData.TransitionProgress = num;
			float_10 = Mathf.LerpAngle(0f, float_9, num);
			float num2 = Mathf.LerpAngle(vector2_0.x, float_6, num);
			float num3 = Mathf.LerpAngle(vector2_0.y, float_5, num);
			MovementContext.ApplyRotation(Quaternion.Lerp(quaternion_0, playerMountingPointData.TargetBodyRotation, num));
			MovementContext.Rotation = Vector2.Lerp(vector2_0, new Vector2(num2, num3), num);
			if (!MovementContext.IsInPronePose)
			{
				float num4 = Mathf.Lerp(float_7, float_8, num);
				MovementContext.SetTilt(num4, false);
				MovementContext.MountedSmoothedTilt = num4;
				if (playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
				{
					MovementContext.MountedSmoothedTiltForCamera = num4;
					MovementContext.PlayerAnimatorSetTilt(num4);
					playerMountingPointData.CurrentMountingPointVerticalOffset = -0.1f * Mathf.Abs(float_10) / float_11;
				}
			}
			Vector3 vector = Vector3.Lerp(vector3_0, playerMountingPointData.PlayerTargetPos, num) - MovementContext.TransformPosition;
			vector.y = 0f;
			Vector3 vector2 = (MovementContext.TransformPosition - playerMountingPointData.PlayerTargetPos).XZ();
			if (vector.sqrMagnitude > 0f)
			{
				MovementContext.ApplyMotion(vector, deltaTime);
				Vector3 vector3 = MovementContext.InverseTransformVector(vector.normalized);
				MovementContext.MovementDirection = new Vector2(vector3.x, vector3.z);
				MovementContext.PlayerAnimatorEnableInert(vector2.sqrMagnitude > 0.0005f);
			}
			if (float_4 > playerMountingPointData.CurrentApproachTime)
			{
				MovementContext.TransformPosition = playerMountingPointData.PlayerTargetPos;
				MovementContext.ApplyMotion(Vector3.zero, deltaTime);
				MovementContext.PlayerAnimatorEnableInert(false);
				playerMountingPointData.TransitionMounting = false;
			}
		}

		public void SetRotation(float currentRotationFactor)
		{
			if ((float)int_0 * float_10 < 0f || int_0 == 0)
			{
				float num = Mathf.Sign(((float)int_0 * float_10 < 0f) ? ((float)int_0) : ((int_0 == 0) ? (-float_10) : 0f)) * Mathf.Lerp(0f, 5f, currentRotationFactor);
				MovementContext.SetTilt(num, false);
				MovementContext.MountedSmoothedTilt = num;
				if (playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
				{
					MovementContext.MountedSmoothedTiltForCamera = num;
					MovementContext.PlayerAnimatorSetTilt(num);
				}
			}
		}

		public void UpdateForward()
		{
			if (playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
			{
				return;
			}
			if ((double)player.MovementContext.OverlapDepth >= 0.01)
			{
				float_12 = MovementContext.Rotation.y;
				return;
			}
			float_12 = playerMountingPointData.PitchLimit.y;
		}

		public override void Move(Vector2 direction)
		{

		}

		public override void Prone()
		{

		}

		public override void Rotate(Vector2 deltaRotation, bool ignoreClamp = false)
		{
			if (float_4 > playerMountingPointData.CurrentApproachTime && !bool_1)
			{
				if (playerMountingPointData.MountPointData.MountSideDirection == EMountSideDirection.Forward)
				{
					UpdateForward();
					MovementContext.SetPitchForce(PitchLimitX, PitchLimitY);
				}
				if (!ignoreClamp)
				{
					deltaRotation = base.ClampRotation(deltaRotation);
				}
				float num = ((int_0 > 0) ? (-float_11) : 0f);
				float num2 = ((int_0 > 0) ? 0f : float_11);
				if (int_0 == 0)
				{
					num = -float_11;
					num2 = float_11;
				}
				float num3 = float_10;
				float_10 = Mathf.Clamp(float_10 + deltaRotation.x, num, num2);
				if (Mathf.Abs(float_10) > Mathf.Abs(num3))
				{
					float num4 = Mathf.Abs(float_10) / float_11;
					float num5 = (((float)int_0 * float_10 < 0f) ? ((float)int_0) : ((int_0 == 0) ? Mathf.Sign(-float_10) : 0f));
					if (!MovementContext.RotationOverlapPrediction(MovementContext.PlayerTransform.right * (num5 * num4 * mountingMovementSettings.TiltPositionOffset), Quaternion.Euler(0f, 2f * deltaRotation.x, 0f), MovementContext.PlayerTransform.Original).Equals(Vector3.zero))
					{
						float_10 = num3;
						deltaRotation.x = 0f;
					}
				}
				MovementContext.Rotation += deltaRotation;
				MovementContext.UpdateDeltaAngle();
				return;
			}
		}

		public override void SetBlindFireAnim(float blindFire)
		{

		}

		public override void SetStep(int step)
		{

		}

		public override void SetTilt(float tilt)
		{

		}

		public void StartExiting()
		{
			if (bool_1)
			{
				return;
			}
			bool_1 = true;
			float_4 = 0f;
			playerMountingPointData.TransitionMounting = true;
			playerMountingPointData.TransitionProgress = 1f;
			if (playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
			{
				MovementContext.PlayerAnimatorSetTilt(MovementContext.MountedSmoothedTilt);
			}
			player.OnMounting(GStruct179.EMountingCommand.StartLeaving);
			Action<float> onExitMountedState = playerMountingPointData.OnExitMountedState;
			if (onExitMountedState == null)
			{
				return;
			}
			onExitMountedState(mountingMovementSettings.ExitTime);
		}

		public override void Vaulting()
		{

		}
	}
}
