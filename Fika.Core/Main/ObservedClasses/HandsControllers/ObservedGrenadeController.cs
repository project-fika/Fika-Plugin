// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Main.Players;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal sealed class ObservedGrenadeController : Player.GrenadeHandsController
{
    public ObservedGrenadeController(IntPtr pointer) : base(pointer)
    {
    }

    private ObservedPlayer _observedPlayer;

    public static ObservedGrenadeController Create(ObservedPlayer observedPlayer, ThrowWeap item)
    {
        var controller = CreateController<ObservedGrenadeController>(observedPlayer, item);
        controller._observedPlayer = observedPlayer;
        return controller;
    }

    public override Il2CppSystem.Collections.Generic.Dictionary<Il2CppSystem.Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        var operationFactoryDelegates = base.GetOperationFactoryDelegates();
        operationFactoryDelegates[(typeof(Player.GrenadeHandsController.PlantTripwireOperation)).ToIl2Cpp()] = new System.Func<Player.ObjectInHandsOperation>(Grenade1);
        return operationFactoryDelegates;
    }

    private Player.ObjectInHandsOperation Grenade1()
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
        /*_observedPlayer.CreateObservedCompass();
        _objectInHandsAnimator.ShowCompass(isActive);
        _observedPlayer.SetPropVisibility(isActive);*/
    }

    /// <summary>
    /// Original method to spawn a grenade, we use <see cref="SpawnGrenade(float, Vector3, Quaternion, Vector3, bool)"/> instead
    /// </summary>
    /// <param name="timeSinceSafetyLevelRemoved"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="force"></param>
    /// <param name="lowThrow"></param>
    public override void ThrowGrenade(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
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
        base.ThrowGrenade(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
    }
}

public sealed class ObservedTripwireState : Player.GrenadeHandsController.PlantTripwireOperation
{
    public ObservedTripwireState(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedTripwireState(Player.GrenadeHandsController controller, FikaPlayer player) : base(Il2CppInjection.Allocate<ObservedTripwireState>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Player.GrenadeHandsController.PlantTripwireOperation>(this, controller);
        _fikaPlayer = player;
    }

    private readonly FikaPlayer _fikaPlayer;

    public new void Start()
    {
        Controller.FirearmsAnimator.SetFireMode(Weapon.EFireMode.greanadePlanting, false);
        PlantOperationState = EPlantOperationState.StateIn;
        State = Player.EOperationState.Executing;
        SetLeftStanceAnimOnStartOperation();
    }

    public override void OnIdleStartAction()
    {
        PlantOperationState = EPlantOperationState.Idling;
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


    public override void Execute(IInventoryOperation operation, Callback callback)
    {
        callback.Succeed();
    }

    public override void PlantTripwire()
    {
        PlantOperationState = EPlantOperationState.Planting;
        Controller.FirearmsAnimator.SetGrenadeFire(FirearmsAnimator.EGrenadeFire.Throw);
    }
}
