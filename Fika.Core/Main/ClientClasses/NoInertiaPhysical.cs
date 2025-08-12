using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ClientClasses;

/// <summary>
/// Currently unused
/// </summary>
public class NoInertiaPhysical : PlayerPhysicalClass
{
    private FikaPlayer _fikaPlayer;

    public override void Init(IPlayer player)
    {
        base.Init(player);
        _fikaPlayer = (FikaPlayer)player;
    }

    public override void OnWeightUpdated()
    {
        BackendConfigSettingsClass.InertiaSettings inertia = Singleton<BackendConfigSettingsClass>.Instance.Inertia;
        float num = IobserverToPlayerBridge_0.Skills.StrengthBuffElite ? _fikaPlayer.InventoryController.Inventory.TotalWeightEliteSkill : _fikaPlayer.InventoryController.Inventory.TotalWeight;
        Inertia = 0.0113f;
        SprintAcceleration = 0.9887f;
        PreSprintAcceleration = 2.9853f;
        float num2 = Mathf.Lerp(inertia.MinMovementAccelerationRangeRight.x, inertia.MaxMovementAccelerationRangeRight.x, Inertia);
        float num3 = Mathf.Lerp(inertia.MinMovementAccelerationRangeRight.y, inertia.MaxMovementAccelerationRangeRight.y, Inertia);
        EFTHardSettings.Instance.MovementAccelerationRange.MoveKey(1, new Keyframe(num2, num3));
        Overweight = BaseOverweightLimits.InverseLerp(num);
        WalkOverweight = WalkOverweightLimits.InverseLerp(num);
        WalkSpeedLimit = 1f;
        Float_3 = SprintOverweightLimits.InverseLerp(num);
        MoveSideInertia = 1.9887f;
        MoveDiagonalInertia = 1.3955f;
        if (_fikaPlayer.IsAI)
        {
            Float_3 = 0f;
        }
        MaxPoseLevel = (Overweight >= 1f) ? 0.9f : 1f;
        Consumptions[EConsumptionType.OverweightIdle].SetActive(this, Overweight >= 1f);
        Consumptions[EConsumptionType.OverweightIdle].Delta.SetDirty();
        Consumptions[EConsumptionType.SitToStand].AllowsRestoration = Overweight >= 1f;
        Consumptions[EConsumptionType.StandUp].AllowsRestoration = Overweight >= 1f;
        Consumptions[EConsumptionType.Walk].Delta.SetDirty();
        Consumptions[EConsumptionType.Sprint].Delta.SetDirty();
        Consumptions[EConsumptionType.VaultLegs].Delta.SetDirty();
        Consumptions[EConsumptionType.VaultHands].Delta.SetDirty();
        Consumptions[EConsumptionType.ClimbLegs].Delta.SetDirty();
        Consumptions[EConsumptionType.ClimbHands].Delta.SetDirty();
        TransitionSpeed.SetDirty();
        PoseLevelDecreaseSpeed.SetDirty();
        PoseLevelIncreaseSpeed.SetDirty();
        FallDamageMultiplier = Mathf.Lerp(1f, StaminaParameters.FallDamageMultiplier, Overweight);
        SoundRadius = StaminaParameters.SoundRadius.Evaluate(Overweight);
        MinStepSound.SetDirty();
        method_3();
        method_7(num);
    }
}
