using System;
using Comfort.Common;
using EFT;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

/// <summary>
/// Used to simulate having near no inertia
/// </summary>
public class NoInertiaMovementContext : ClientMovementContext
{
    public NoInertiaMovementContext(IntPtr pointer) : base(pointer)
    {
    }

    public NoInertiaMovementContext() : base(Il2CppInjection.Allocate<NoInertiaMovementContext>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<MovementContext>(this);
    }

    public new static NoInertiaMovementContext Create(Player player, Il2CppSystem.Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
    {
        var movementContext = Create<NoInertiaMovementContext>(player, characterControllerGetter, groundMask);
        return movementContext;
    }

    public override void Init()
    {
        base.Init();
        TiltInertia = 0.22f;
        WalkInertia = 0.005f;
        SprintBrakeInertia = 0f;
    }

    public override void WeightRelatedValuesUpdated()
    {
        UpdateStrengthCurveCache();
        if (_player.ProceduralWeaponAnimation != null)
        {
            _player.ProceduralWeaponAnimation.Overweight = _player.Physical.Overweight;
            _player.ProceduralWeaponAnimation.UpdateSwayFactors();
            _player.ProceduralWeaponAnimation.UpdateSwaySettings();
            _player.ProceduralWeaponAnimation.WeaponFlipSpeed = InertiaSettings.WeaponFlipSpeed.Evaluate(_player.Physical.Inertia);
        }
        UpdateCovertEfficiency(_player.MovementContext.ClampedSpeed, true);
        UpdateInertiaCurveCache();
        _player.HealthController.FallSafeHeight = Mathf.Lerp(Singleton<GlobalConfiguration>.Instance.Health.Falling.SafeHeight, Singleton<GlobalConfiguration>.Instance.Stamina.SafeHeightOverweight, _player.Physical.Overweight);
        PlayerAnimatorTransitionSpeed = TransitionSpeed;
        if (PoseLevel > _player.Physical.MaxPoseLevel && CurrentState is MovementState movementState)
        {
            movementState.ChangePose(_player.Physical.MaxPoseLevel - PoseLevel);
        }
        if (_player.PoseMemo > _player.Physical.MaxPoseLevel)
        {
            _player.PoseMemo = _player.Physical.MaxPoseLevel;
        }
        var walkSpeedLimit = _player.Physical.WalkSpeedLimit;
        RemoveStateSpeedLimit(Player.ESpeedLimit.Weight);
        if (walkSpeedLimit < 1f)
        {
            AddStateSpeedLimit(walkSpeedLimit * MaxSpeed, Player.ESpeedLimit.Weight);
        }
        UpdateCharacterControllerSpeedLimit();
    }
}
