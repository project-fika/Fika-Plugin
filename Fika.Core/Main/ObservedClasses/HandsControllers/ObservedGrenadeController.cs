// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using System;
using System.Collections.Generic;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal class ObservedGrenadeController : Player.GrenadeHandsController
{
    private ObservedPlayer _observedPlayer;

    public static ObservedGrenadeController Create(ObservedPlayer observedPlayer, ThrowWeapItemClass item)
    {
        ObservedGrenadeController controller = smethod_9<ObservedGrenadeController>(observedPlayer, item);
        controller._observedPlayer = observedPlayer;
        return controller;
    }

    public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        Dictionary<Type, OperationFactoryDelegate> operationFactoryDelegates = base.GetOperationFactoryDelegates();
        operationFactoryDelegates[typeof(TripwireStateManagerClass)] = new OperationFactoryDelegate(Grenade1);
        return operationFactoryDelegates;
    }

    private Player.BaseAnimationOperationClass Grenade1()
    {
        return new ObservedTripwireState(this, _observedPlayer);
    }

    public override bool CanChangeCompassState(bool newState)
    {
        return false;
    }

    public override bool CanRemove()
    {
        return true;
    }

    public override void OnCanUsePropChanged(bool canUse)
    {
        // Do nothing
    }

    public override void SetCompassState(bool active)
    {
        // Do nothing
    }

    public override void CompassStateHandler(bool isActive)
    {
        _observedPlayer.CreateObservedCompass();
        _objectInHandsAnimator.ShowCompass(isActive);
    }

    /// <summary>
    /// Original method to spawn a grenade, we use <see cref="SpawnGrenade(float, Vector3, Quaternion, Vector3, bool)"/> instead
    /// </summary>
    /// <param name="timeSinceSafetyLevelRemoved"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="force"></param>
    /// <param name="lowThrow"></param>
    public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
    {
        // Do nothing, we use our own method
    }

    /// <summary>
    /// Spawns a grenade, uses data from <see cref="SubPackets.GrenadePacket"/>
    /// </summary>
    /// <param name="timeSinceSafetyLevelRemoved">The time since the safety was removed, use 0f</param>
    /// <param name="position">The <see cref="Vector3"/> position to start from</param>
    /// <param name="rotation">The <see cref="Quaternion"/> rotation of the grenade</param>
    /// <param name="force">The <see cref="Vector3"/> force of the grenade</param>
    /// <param name="lowThrow">If it's a low throw or not</param>
    public void SpawnGrenade(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
    {
        base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
    }
}

public class ObservedTripwireState(Player.GrenadeHandsController controller, FikaPlayer player) : Player.GrenadeHandsController.TripwireStateManagerClass(controller)
{
    private readonly FikaPlayer _fikaPlayer = player;

    public new void Start()
    {
        Gparam_0.FirearmsAnimator.SetFireMode(Weapon.EFireMode.greanadePlanting, false);
        EPlantOperationState_0 = EPlantOperationState.StateIn;
        State = Player.EOperationState.Executing;
        SetLeftStanceAnimOnStartOperation();
    }

    public override void OnIdleStartAction()
    {
        EPlantOperationState_0 = EPlantOperationState.Idling;
    }

    public override void OnEnd()
    {
        // Do nothing
    }

    public override void HandleFireInput()
    {
        // Do nothing
    }

    public override void HandleAltFireInput()
    {
        // Do nothing
    }


    public override void Execute(GInterface438 operation, Callback callback)
    {
        callback.Succeed();
    }

    public override void PlantTripwire()
    {
        EPlantOperationState_0 = EPlantOperationState.Planting;
        Gparam_0.FirearmsAnimator.SetGrenadeFire(FirearmsAnimator.EGrenadeFire.Throw);
    }
}
