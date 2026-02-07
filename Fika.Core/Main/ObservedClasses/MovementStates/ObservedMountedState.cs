using Comfort.Common;
using EFT;
using EFT.WeaponMounting;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedMountedState : MovementState
{
    public ObservedMountedState(MovementContext movementContext, Player observedPlayer) : base(movementContext)
    {
        _mountingMovementSettings = Singleton<BackendConfigSettingsClass>.Instance.MountingSettings.MovementSettings;
        _player = observedPlayer;
        RotationSpeedClamp = _mountingMovementSettings.RotationSpeedClamp;
        StateSensitivity = _mountingMovementSettings.SensitivityMultiplier;
    }

    private readonly Player _player;
    private PlayerMountingPointData _playerMountingPointData;
    private readonly IMountingMovementSettings _mountingMovementSettings;

    private Quaternion _quaternion_0;
    private Vector2 _vector2_0;
    private Vector3 _vector3_0;

    private int _int_0;

    private bool _bool_0;
    private bool _bool_1;

    private float _float_4;
    private float _float_5;
    private float _float_6;
    private float _float_7;
    private float _float_8;
    private float _float_9;
    private float _float_10;
    private float _float_11;
    private float _float_12;

    private float PitchLimitX
    {
        get
        {
            return _playerMountingPointData.PitchLimit.x;
        }
    }
    private float PitchLimitY
    {
        get
        {
            return _float_12;
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
        _player.CurrentLeanType = Player.LeanType.SlowLean;
        _playerMountingPointData = MovementContext.PlayerMountingPointData;
        _float_12 = _playerMountingPointData.PitchLimit.y;
        _float_7 = MovementContext.SmoothedTilt;
        MovementContext.SetTilt(0f, false);
        MovementContext.Step = 0;
        MovementContext.MountedSmoothedTilt = 0f;
        MovementContext.MountedSmoothedTiltForCamera = 0f;
        if (MovementContext.LeftStanceController.LeftStance)
        {
            MovementContext.LeftStanceController.ToggleLeftStance();
        }
        if (Mathf.Abs(_player.ProceduralWeaponAnimation.CurrentScope.Rotation) > EFTHardSettings.Instance.SCOPE_ROTATION_THRESHOLD)
        {
            MovementContext.ToggleFirearmAimByBodyAction();
        }
        _vector3_0 = MovementContext.TransformPosition;
        _vector2_0 = MovementContext.Rotation;
        _quaternion_0 = MovementContext.TransformRotation;
        _float_4 = 0f;
        _float_10 = 0f;
        _float_6 = _playerMountingPointData.TargetHandsRotation;
        _float_9 = 0f;
        _float_8 = 0f;
        _int_0 = (int)_playerMountingPointData.MountPointData.MountSideDirection;
        _float_11 = Mathf.Abs(Mathf.DeltaAngle(_playerMountingPointData.YawLimit.x, _playerMountingPointData.YawLimit.y));
        if (_playerMountingPointData.MountPointData.MountSideDirection == EMountSideDirection.Forward)
        {
            _float_11 /= 2f;
        }
        if (_playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
        {
            var num = MovementContext.Yaw;
            if (num < -360f)
            {
                num += 360f;
            }
            if (num > 360f)
            {
                num -= 360f;
            }
            var num2 = num > 0f ? num - 360f : num + 360f;
            if (num < _playerMountingPointData.YawLimit.x || num > _playerMountingPointData.YawLimit.y)
            {
                num = num2;
            }
            if (num >= _playerMountingPointData.YawLimit.x && num <= _playerMountingPointData.YawLimit.y)
            {
                _float_6 = MovementContext.Yaw;
            }
            else
            {
                _float_6 = MovementContext.Yaw < _playerMountingPointData.YawLimit.x ? _playerMountingPointData.YawLimit.x : _playerMountingPointData.YawLimit.y;
            }
            _float_9 = Mathf.Approximately(_playerMountingPointData.TargetHandsRotation, _playerMountingPointData.YawLimit.x)
                ? _float_6 - _playerMountingPointData.YawLimit.x : _float_6 - _playerMountingPointData.YawLimit.y;
            var num3 = _int_0 * _float_9 < 0f ? _int_0 : _int_0 == 0 ? -_float_9 : 0f;
            var num4 = Mathf.Abs(_float_9) / _float_11;
            _float_8 = Mathf.Sign(num3) * Mathf.Lerp(0f, 5f, num4);
        }
        _playerMountingPointData.TransitionMounting = true;
        _playerMountingPointData.TransitionProgress = 0f;
        MovementContext.method_25();
        MovementContext.RotationAction = MovementContext.MountingRotationFunction;
        MovementContext.SetPitchSmoothly(PitchLimitX, PitchLimitY);
        _bool_0 = MovementContext.CanUseProp.Value;
        _bool_1 = false;
        _player.OnMounting(MountingPacketStruct.EMountingCommand.Enter);
        _float_5 = _playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward || MovementContext.IsInPronePose
            ? MovementContext.Pitch < MovementContext.PitchLimit.x ? PitchLimitX : MovementContext.Pitch > MovementContext.PitchLimit.y ? MovementContext.PitchLimit.y : MovementContext.Pitch : 0f;
        var num5 = Mathf.InverseLerp(PitchLimitX, PitchLimitY, _float_5);
        var num6 = Mathf.Lerp(_playerMountingPointData.PoseLimit.x, _playerMountingPointData.PoseLimit.y, num5);
        MovementContext.SetPoseLevel(num6, false);
        var onEnterMountedState = _playerMountingPointData.OnEnterMountedState;
        if (onEnterMountedState == null)
        {
            return;
        }
        onEnterMountedState(_playerMountingPointData.CurrentApproachTime);
    }

    public override void Exit(bool toSameState)
    {
        base.Exit(toSameState);
        if (_playerMountingPointData.MountPointData.MountSideDirection == EMountSideDirection.Forward)
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
        MovementContext.SetYawLimit(Player.PlayerMovementConstantsClass.FULL_YAW_RANGE);
        MovementContext.SetPitchSmoothly(MovementContext.IsInPronePose ? Player.PlayerMovementConstantsClass.PRONE_POSE_ROTATION_PITCH_RANGE : Player.PlayerMovementConstantsClass.STAND_POSE_ROTATION_PITCH_RANGE);
        MovementContext.RotationAction = MovementContext.DefaultRotationFunction;
        MovementContext.CanUseProp.Value = _bool_0;
        _player.ProceduralWeaponAnimation.SetStrategy(EPointOfView.ThirdPerson);
        var onExitMountedState = _playerMountingPointData.OnExitMountedState;
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
        if (_bool_1)
        {
            if (_float_4 > _mountingMovementSettings.ExitTime)
            {
                MovementContext.ExitMountedState();
            }
            if (_float_4 <= _mountingMovementSettings.ExitTime)
            {
                _float_4 += deltaTime * Time.timeScale;
                _playerMountingPointData.TransitionProgress = 1f - _float_4 / _mountingMovementSettings.ExitTime;
            }
            return;
        }
        if (_float_4 <= _playerMountingPointData.CurrentApproachTime)
        {
            UpdateApproach(deltaTime * Time.timeScale);
            _player.OnMounting(MountingPacketStruct.EMountingCommand.Update);
            return;
        }
        MovementContext.SetYawLimit(_playerMountingPointData.YawLimit);
        MovementContext.SetPitchSmoothly(PitchLimitX, PitchLimitY);
        UpdateState();
        _player.OnMounting(MountingPacketStruct.EMountingCommand.Update);
    }

    public void UpdateState()
    {
        var num = Mathf.Abs(_float_10) / _float_11;
        if (!MovementContext.IsInPronePose)
        {
            SetRotation(num);
            SetPose();
        }
        if (_int_0 != 0)
        {
            _playerMountingPointData.CurrentMountingPointVerticalOffset = -0.1f * num;
        }
    }

    public void SetPose()
    {
        var num = Mathf.InverseLerp(PitchLimitX, PitchLimitY, MovementContext.Pitch);
        var num2 = Mathf.Lerp(_playerMountingPointData.PoseLimit.x, _playerMountingPointData.PoseLimit.y, num);
        MovementContext.SetPoseLevel(num2, false);
    }

    public void UpdateApproach(float deltaTime)
    {
        MovementContext.IgnoreDeltaMovement = true;
        _float_4 += deltaTime;
        var num = _float_4 / _playerMountingPointData.CurrentApproachTime;
        _playerMountingPointData.TransitionProgress = num;
        _float_10 = Mathf.LerpAngle(0f, _float_9, num);
        var num2 = Mathf.LerpAngle(_vector2_0.x, _float_6, num);
        var num3 = Mathf.LerpAngle(_vector2_0.y, _float_5, num);
        MovementContext.ApplyRotation(Quaternion.Lerp(_quaternion_0, _playerMountingPointData.TargetBodyRotation, num));
        MovementContext.Rotation = Vector2.Lerp(_vector2_0, new Vector2(num2, num3), num);
        if (!MovementContext.IsInPronePose)
        {
            var num4 = Mathf.Lerp(_float_7, _float_8, num);
            MovementContext.SetTilt(num4, false);
            MovementContext.MountedSmoothedTilt = num4;
            if (_playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
            {
                MovementContext.MountedSmoothedTiltForCamera = num4;
                MovementContext.PlayerAnimatorSetTilt(num4);
                _playerMountingPointData.CurrentMountingPointVerticalOffset = -0.1f * Mathf.Abs(_float_10) / _float_11;
            }
        }
        var vector = Vector3.Lerp(_vector3_0, _playerMountingPointData.PlayerTargetPos, num) - MovementContext.TransformPosition;
        vector.y = 0f;
        var vector2 = (MovementContext.TransformPosition - _playerMountingPointData.PlayerTargetPos).XZ();
        if (vector.sqrMagnitude > 0f)
        {
            MovementContext.ApplyMotion(vector, deltaTime);
            var vector3 = MovementContext.InverseTransformVector(vector.normalized);
            MovementContext.MovementDirection = new Vector2(vector3.x, vector3.z);
            MovementContext.PlayerAnimatorEnableInert(vector2.sqrMagnitude > 0.0005f);
        }
        if (_float_4 > _playerMountingPointData.CurrentApproachTime)
        {
            MovementContext.TransformPosition = _playerMountingPointData.PlayerTargetPos;
            MovementContext.ApplyMotion(Vector3.zero, deltaTime);
            MovementContext.PlayerAnimatorEnableInert(false);
            _playerMountingPointData.TransitionMounting = false;
        }
    }

    public void SetRotation(float currentRotationFactor)
    {
        if (_int_0 * _float_10 < 0f || _int_0 == 0)
        {
            var num = Mathf.Sign(_int_0 * _float_10 < 0f ? _int_0 : _int_0 == 0 ? -_float_10 : 0f) * Mathf.Lerp(0f, 5f, currentRotationFactor);
            MovementContext.SetTilt(num, false);
            MovementContext.MountedSmoothedTilt = num;
            if (_playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
            {
                MovementContext.MountedSmoothedTiltForCamera = num;
                MovementContext.PlayerAnimatorSetTilt(num);
            }
        }
    }

    public void UpdateForward()
    {
        if (_playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
        {
            return;
        }
        if ((double)_player.MovementContext.OverlapDepth >= 0.01)
        {
            _float_12 = MovementContext.Rotation.y;
            return;
        }
        _float_12 = _playerMountingPointData.PitchLimit.y;
    }

    public override void Move(Vector2 direction)
    {

    }

    public override void Prone()
    {

    }

    public override void Rotate(Vector2 deltaRotation, bool ignoreClamp = false)
    {
        if (_float_4 > _playerMountingPointData.CurrentApproachTime && !_bool_1)
        {
            if (_playerMountingPointData.MountPointData.MountSideDirection == EMountSideDirection.Forward)
            {
                UpdateForward();
                MovementContext.SetPitchForce(PitchLimitX, PitchLimitY);
            }
            if (!ignoreClamp)
            {
                deltaRotation = ClampRotation(deltaRotation);
            }
            var num = _int_0 > 0 ? -_float_11 : 0f;
            var num2 = _int_0 > 0 ? 0f : _float_11;
            if (_int_0 == 0)
            {
                num = -_float_11;
                num2 = _float_11;
            }
            var num3 = _float_10;
            _float_10 = Mathf.Clamp(_float_10 + deltaRotation.x, num, num2);
            if (Mathf.Abs(_float_10) > Mathf.Abs(num3))
            {
                var num4 = Mathf.Abs(_float_10) / _float_11;
                var num5 = _int_0 * _float_10 < 0f ? _int_0 : _int_0 == 0 ? Mathf.Sign(-_float_10) : 0f;
                if (!MovementContext.RotationOverlapPrediction(MovementContext.PlayerTransform.right * (num5 * num4 * _mountingMovementSettings.TiltPositionOffset), Quaternion.Euler(0f, 2f * deltaRotation.x, 0f), MovementContext.PlayerTransform.Original).Equals(Vector3.zero))
                {
                    _float_10 = num3;
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
        if (_bool_1)
        {
            return;
        }
        _bool_1 = true;
        _float_4 = 0f;
        _playerMountingPointData.TransitionMounting = true;
        _playerMountingPointData.TransitionProgress = 1f;
        if (_playerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward)
        {
            MovementContext.PlayerAnimatorSetTilt(MovementContext.MountedSmoothedTilt);
        }
        _player.OnMounting(MountingPacketStruct.EMountingCommand.StartLeaving);
        var onExitMountedState = _playerMountingPointData.OnExitMountedState;
        if (onExitMountedState == null)
        {
            return;
        }
        onExitMountedState(_mountingMovementSettings.ExitTime);
    }

    public override void Vaulting()
    {

    }
}
