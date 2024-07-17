using Comfort.Common;
using EFT;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    /// <summary>
    /// Used to simulate having near no inertia
    /// </summary>
    public class NoInertiaMovementContext : ClientMovementContext
    {
        public new static NoInertiaMovementContext Create(Player player, Func<IAnimator> animatorGetter, Func<ICharacterController> characterControllerGetter, LayerMask groundMask)
        {
            NoInertiaMovementContext movementContext = Create<NoInertiaMovementContext>(player, animatorGetter, characterControllerGetter, groundMask);
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
            if (_player.ProceduralWeaponAnimation != null)
            {
                _player.ProceduralWeaponAnimation.Overweight = _player.Physical.Overweight;
                _player.ProceduralWeaponAnimation.UpdateSwayFactors();
                _player.ProceduralWeaponAnimation.UpdateSwaySettings();
                _player.ProceduralWeaponAnimation.WeaponFlipSpeed = InertiaSettings.WeaponFlipSpeed.Evaluate(_player.Physical.Inertia);
            }
            UpdateCovertEfficiency(_player.MovementContext.ClampedSpeed, true);
            _player.UpdateStepSoundRolloff();
            _player.HealthController.FallSafeHeight = Mathf.Lerp(Singleton<BackendConfigSettingsClass>.Instance.Health.Falling.SafeHeight, Singleton<BackendConfigSettingsClass>.Instance.Stamina.SafeHeightOverweight, _player.Physical.Overweight);
            PlayerAnimatorTransitionSpeed = TransitionSpeed;
            if (PoseLevel > _player.Physical.MaxPoseLevel && CurrentState is MovementState movementState)
            {
                movementState.ChangePose(_player.Physical.MaxPoseLevel - PoseLevel);
            }
            if (_player.PoseMemo > _player.Physical.MaxPoseLevel)
            {
                _player.PoseMemo = _player.Physical.MaxPoseLevel;
            }
            float walkSpeedLimit = _player.Physical.WalkSpeedLimit;
            RemoveStateSpeedLimit(Player.ESpeedLimit.Weight);
            if (walkSpeedLimit < 1f)
            {
                AddStateSpeedLimit(walkSpeedLimit * MaxSpeed, Player.ESpeedLimit.Weight);
            }
            UpdateCharacterControllerSpeedLimit();
        }
    }
}
