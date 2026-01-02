// © 2026 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using static EFT.Player;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

public class ObservedFirearmController : FirearmController
{
    public WeaponManagerClass WeaponManager
    {
        get
        {
            return _weaponManager;
        }
    }
    public override bool IsTriggerPressed
    {
        get
        {
            return _triggerPressed;
        }
    }

    public bool IsRevolver { get; internal set; }

    private ObservedPlayer _observedPlayer;
    private bool _triggerPressed;
    private bool _needsReset;
    private float _lastFireTime;
    private float _overlapCounter;
    private bool _hasFired;
    private WeaponPrefab _weaponPrefab;
    private WeaponManagerClass _weaponManager;
    private UnderbarrelManagerClass _underBarrelManager;
    private bool _boltActionReload;
    private bool _isThrowingPatron;
    private bool _stationaryWeapon;

    public override bool IsAiming
    {
        get
        {
            return _isAiming;
        }
        set
        {
            if (_isAiming == value)
            {
                if (FirearmsAnimator != null)
                {
                    method_64(); // Reset animator flags 
                }
                return;
            }
            _isAiming = value;
            method_63(_isAiming); // Set animator flags
            _observedPlayer.ProceduralWeaponAnimation.IsAiming = _isAiming;
        }
    }

    public override Vector3 WeaponDirection
    {
        get
        {
            return -CurrentFireport.up;
        }
    }

    public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
    {
        // Check for GClass increments..
        var operationFactoryDelegates = base.GetOperationFactoryDelegates();
        operationFactoryDelegates[typeof(GClass2037)] = new OperationFactoryDelegate(Idle1);
        // Look for operations that implement OnShellEjectEvent and ThrowPatronAsLoot
        operationFactoryDelegates[typeof(MutliBarrelReloadOperationClass)] = new OperationFactoryDelegate(ThrowPatron1);
        operationFactoryDelegates[typeof(SingleBarrelReloadOperationClass)] = new OperationFactoryDelegate(ThrowPatron2);
        operationFactoryDelegates[typeof(FixMalfunctionOperationClass)] = new OperationFactoryDelegate(ThrowPatron3);
        operationFactoryDelegates[typeof(RechamberOperationClass)] = new OperationFactoryDelegate(ThrowPatron4);
        return operationFactoryDelegates;
    }

    private BaseAnimationOperationClass ThrowPatron1()
    {
        return new ObservedThrowPatronOperation1(this);
    }

    private BaseAnimationOperationClass ThrowPatron2()
    {
        return new ObservedThrowPatronOperation2(this);
    }

    private BaseAnimationOperationClass ThrowPatron3()
    {
        return new ObservedThrowPatronOperation3(this);
    }

    private BaseAnimationOperationClass ThrowPatron4()
    {
        return new ObservedThrowPatronOperation4(this);
    }

    private BaseAnimationOperationClass Idle1()
    {
        return new ObservedIdleOperation(this);
    }

    protected void Start()
    {
        _objectInHandsAnimator.SetAiming(false);
        _weaponPrefab = ControllerGameObject.GetComponent<WeaponPrefab>();
        _weaponManager = _weaponPrefab.ObjectInHands as WeaponManagerClass;
        if (UnderbarrelWeapon != null)
        {
            var weaponTraverse = Traverse.Create(this);
            _underBarrelManager = weaponTraverse.Field<UnderbarrelManagerClass>("underbarrelManagerClass").Value;
        }
        IsRevolver = Weapon is RevolverItemClass;
        _stationaryWeapon = Weapon.IsStationaryWeapon;
    }

    public static ObservedFirearmController Create(ObservedPlayer player, Weapon weapon)
    {
        var controller = smethod_6<ObservedFirearmController>(player, weapon);
        controller._observedPlayer = player;
        return controller;
    }

    public override bool CanStartReload()
    {
        return true;
    }

    public override bool IsInInteractionStrictCheck()
    {
        return false;
    }

    public override void SetAim(int scopeIndex)
    {
        Item.AimIndex.Value = Mathf.Max(0, scopeIndex);
        SetObservedAim(scopeIndex >= 0);
    }

    private void SetObservedAim(bool isAiming)
    {
        IsAiming = isAiming;
        _player.method_60(0.2f, false);
        if (isAiming)
        {
            method_60();
        }
    }

    public override void ManualUpdate(float deltaTime)
    {
        base.ManualUpdate(deltaTime);
        if (_hasFired)
        {
            _lastFireTime += deltaTime;
            if (_lastFireTime > 0.1f)
            {
                FirearmsAnimator.SetFire(false);
                _hasFired = false;
                if (_needsReset)
                {
                    _needsReset = false;
                    WeaponSoundPlayer.OnBreakLoop();
                }
            }
        }
    }

    public override void ReloadMag(MagazineItemClass magazine, ItemAddress itemAddress, Callback callback)
    {
        _player.MovementContext.PlayerAnimator.AnimatedInteractions.ForceStopInteractions();
        CurrentOperation.ReloadMag(magazine, itemAddress, callback, null);
    }

    public override void QuickReloadMag(MagazineItemClass magazine, Callback callback)
    {
        CurrentOperation.QuickReloadMag(magazine, callback, null);
    }

    public override void ReloadGrenadeLauncher(AmmoPackReloadingClass foundItem, Callback callback)
    {
        CurrentOperation.ReloadGrenadeLauncher(foundItem, callback);
    }

    public override void ReloadCylinderMagazine(AmmoPackReloadingClass ammoPack, Callback callback, bool quickReload = false)
    {
        if (Item.GetCurrentMagazine() == null)
        {
            return;
        }

        CurrentOperation.ReloadCylinderMagazine(ammoPack, callback, null, quickReload);
    }

    public override void ReloadWithAmmo(AmmoPackReloadingClass ammoPack, Callback callback)
    {
        if (IsRevolver)
        {
            CurrentOperation.ReloadCylinderMagazine(ammoPack, callback, null, false);
            return;
        }

        CurrentOperation.ReloadWithAmmo(ammoPack, callback, null);
    }

    public override void ReloadBarrels(AmmoPackReloadingClass ammoPack, ItemAddress placeToPutContainedAmmoMagazine, Callback callback)
    {
        if (ammoPack.AmmoCount > 0)
        {
            CurrentOperation.ReloadBarrels(ammoPack, placeToPutContainedAmmoMagazine, callback, null);
        }
    }

    public override void WeaponOverlapping()
    {
        if (!_observedPlayer.ShouldOverlap)
        {
            return;
        }

        SetWeaponOverlapValue(_observedPlayer.ObservedOverlap);
        ObservedOverlapView();
        if (_overlapCounter <= 1f)
        {
            _overlapCounter += Time.deltaTime / 1f;
        }
        if (_observedPlayer.LeftStanceDisabled && _observedPlayer.MovementContext.LeftStanceEnabled && _overlapCounter > 1f)
        {
            _observedPlayer.MovementContext.LeftStanceController.DisableLeftStanceAnimFromHandsAction();
            _overlapCounter = 0f;
        }
        if (!_observedPlayer.MovementContext.LeftStanceController.LastAnimValue && !_observedPlayer.LeftStanceDisabled && _observedPlayer.MovementContext.LeftStanceEnabled && _overlapCounter > 1f)
        {
            _observedPlayer.MovementContext.LeftStanceController.SetAnimatorLeftStanceToCacheFromHandsAction();
            _overlapCounter = 0f;
        }

        _observedPlayer.ShouldOverlap = false;
    }

    private void ObservedOverlapView()
    {
        if (_observedPlayer.ObservedOverlap < 0.02f)
        {
            _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = _observedPlayer.ObservedOverlap;
            _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = true;
            _observedPlayer.ShouldOverlap = false;
            return;
        }

        var vector = _player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Get();
        _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = false;
        _player.ProceduralWeaponAnimation.TurnAway.OriginZShift = vector.y;
        _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = _observedPlayer.ObservedOverlap;
    }

    public override void OnPlayerDead()
    {
        try
        {
            base.OnPlayerDead();
            _triggerPressed = false;
            SetTriggerPressed(false);

            _needsReset = false;
            WeaponSoundPlayer.OnBreakLoop();

            _observedPlayer.HandsAnimator.Animator.Update(Time.fixedDeltaTime);
            ManualUpdate(Time.fixedDeltaTime);
            if (CurrentOperation.State != EOperationState.Finished)
            {
                CurrentOperation.FastForward();
            }

            StartCoroutine(BreakFiringLoop());
        }
        catch (Exception ex)
        {
            FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::OnPlayerDead: Exception was caught: " + ex.Message);
        }
    }

    public override void IEventsConsumerOnShellEject()
    {
        if (_isThrowingPatron)
        {
            _isThrowingPatron = false;
            CurrentOperation.OnShellEjectEvent();
            return;
        }

        _weaponManager.StartSpawnShell(_observedPlayer.Velocity * 0.66f, 0);
        if (_boltActionReload)
        {
            var magazine = Item.GetCurrentMagazine();
            var weapon = Weapon;
            if (magazine != null && magazine is not CylinderMagazineItemClass && weapon.HasChambers)
            {
                magazine.Cartridges.PopTo(_observedPlayer.InventoryController, Item.Chambers[0].CreateItemAddress());
            }

            FirearmsAnimator.SetBoltActionReload(false);
            FirearmsAnimator.SetFire(false);

            _boltActionReload = false;
        }
    }

    public override void CompassStateHandler(bool isActive)
    {
        /*_observedPlayer.CreateObservedCompass();
        _objectInHandsAnimator.ShowCompass(isActive);
        _observedPlayer.SetPropVisibility(isActive);*/
    }

    private IEnumerator BreakFiringLoop()
    {
        WeaponSoundPlayer.Release();
        var isFiring = Traverse.Create(WeaponSoundPlayer).Field<bool>("_isFiring");
        var attempts = 0;
        WaitForEndOfFrame waitForEndOfFrame = new();
        while (isFiring.Value && attempts < 10)
        {
            yield return waitForEndOfFrame;
            WeaponSoundPlayer.StopFiringLoop();
            attempts++;
        }
    }

    public override void SetScopeMode(FirearmScopeStateStruct[] scopeStates)
    {
        _player.ProceduralWeaponAnimation.ObservedCalibration();
        base.SetScopeMode(scopeStates);
    }

    public override void AdjustShotVectors(ref Vector3 position, ref Vector3 direction)
    {
        // Do nothing
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

    public override bool ToggleBipod()
    {
        return HasBipod && CurrentOperation.ToggleBipod();
    }

    /// <summary>
    /// Fires a replicated rocket
    /// </summary>
    /// <param name="rocketClass">The ammo to shoot</param>
    /// <param name="shotPosition">Start position</param>
    /// <param name="shotForward">The forward velocity</param>
    public void HandleRocketShot(AmmoItemClass rocketClass, Vector3 shotPosition, Vector3 shotForward)
    {
        FirearmsAnimator.SetFire(true);

        // Handle the rocket shot
        rocketClass.IsUsed = true;
        var smokePort = TransformHelperClass.FindTransformRecursiveContains(WeaponRoot.transform, "smokeport", false);
        InitiateRocket(rocketClass, shotPosition, shotForward, smokePort);
        Weapon.FirstLoadedChamberSlot.RemoveItem();
        WeaponManager.MoveAmmoFromChamberToShellPort(true, 0);

        FirearmsAnimator.SetFire(false);
    }

    public void HandleShotInfoPacket(ShotInfoPacket packet, InventoryController inventoryController)
    {
        if (packet.ShotType == EShotType.DryFire)
        {
            HandleObservedDryShot();
            return;
        }

        if (packet.ShotType >= EShotType.Misfire)
        {
            switch (packet.ShotType)
            {
                case EShotType.Misfire:
                    Weapon.MalfState.State = Weapon.EMalfunctionState.Misfire;
                    break;
                case EShotType.Feed:
                    Weapon.MalfState.State = Weapon.EMalfunctionState.Feed;
                    break;
                case EShotType.JamedShot:
                    Weapon.MalfState.State = Weapon.EMalfunctionState.Jam;
                    break;
                case EShotType.SoftSlidedShot:
                    Weapon.MalfState.State = Weapon.EMalfunctionState.SoftSlide;
                    break;
                case EShotType.HardSlidedShot:
                    Weapon.MalfState.State = Weapon.EMalfunctionState.HardSlide;
                    break;
            }

            var ammo = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate, null);
            Weapon.MalfState.MalfunctionedAmmo = ammo;
            Weapon.MalfState.AmmoToFire = ammo;

            FirearmsAnimator.MisfireSlideUnknown(false);
            _weaponPrefab.InitMalfunctionState(Weapon, false, false, out _);
            FirearmsAnimator.Malfunction((int)Weapon.MalfState.State);

            switch (Weapon.MalfState.State)
            {
                case Weapon.EMalfunctionState.Misfire:
                    FirearmsAnimator.Animator.Play("MISFIRE", 1, 0f);
                    break;
                case Weapon.EMalfunctionState.Jam:
                    FirearmsAnimator.Animator.Play("JAM", 1, 0f);
                    break;
                case Weapon.EMalfunctionState.HardSlide:
                    FirearmsAnimator.Animator.Play("HARD_SLIDE", 1, 0f);
                    break;
                case Weapon.EMalfunctionState.SoftSlide:
                    FirearmsAnimator.Animator.Play("SOFT_SLIDE", 1, 0f);
                    break;
                case Weapon.EMalfunctionState.Feed:
                    FirearmsAnimator.Animator.Play("FEED", 1, 0f);
                    break;
            }

            if (Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
            {
                if (Weapon.HasChambers)
                {
                    var firstChamber = Weapon.Chambers[0];
                    if (firstChamber.ContainedItem is AmmoItemClass)
                    {
                        firstChamber.RemoveItemWithoutRestrictions();
                    }
                }

                FirearmsAnimator.SetFire(true);
                _hasFired = true;
                _lastFireTime = 0f;
                return;
            }

            if (Weapon.HasChambers)
            {
                var firstChamber = Weapon.Chambers[0];
                if (firstChamber.ContainedItem is AmmoItemClass)
                {
                    firstChamber.RemoveItemWithoutRestrictions();
                }

                if (Weapon.MalfState.State == Weapon.EMalfunctionState.Feed)
                {
                    var currentMagazine = Weapon.GetCurrentMagazine();
                    if (currentMagazine != null)
                    {
                        var fedAmmo = (AmmoItemClass)currentMagazine.Cartridges.PopToNowhere(_observedPlayer.InventoryController).Value.ResultItem;
                        if (fedAmmo != null)
                        {
                            Weapon.MalfState.MalfunctionedAmmo = fedAmmo;
                            // Leave here for now - Lacyway
                            /*weaponManager.SetRoundIntoWeapon(fedAmmo, 0);
                            weaponManager.MoveAmmoFromChamberToShellPort(false, 0);*/
                        }
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogError("HandleShotInfoPacket: Could not find ammo when setting up feed malfunction!");
                        }
                    }
                }
                else
                {
                    _weaponManager.MoveAmmoFromChamberToShellPort(true, 0);
                }
            }
        }

        if (IsRevolver)
        {
            HandleRevolverShot(packet, inventoryController);
            return;
        }
        HandleObservedShot(packet, inventoryController);
    }

    private void HandleObservedDryShot()
    {
        if (IsRevolver)
        {
            var revolver = Weapon;
            var cylinderMagazine = (CylinderMagazineItemClass)revolver.GetCurrentMagazine();

            revolver.CylinderHammerClosed = revolver.FireMode.FireMode == Weapon.EFireMode.doubleaction;
            FirearmsAnimator.SetCamoraFireIndex(cylinderMagazine.CurrentCamoraIndex);
            if ((revolver.CylinderHammerClosed && revolver.FireMode.FireMode == Weapon.EFireMode.doubleaction)
                || (!revolver.CylinderHammerClosed && revolver.FireMode.FireMode == Weapon.EFireMode.single))
            {
                cylinderMagazine.DryFireIncrementCamoraIndex();
            }
            FirearmsAnimator.SetDoubleAction(Convert.ToSingle(revolver.CylinderHammerClosed));
            FirearmsAnimator.SetCamoraIndex(cylinderMagazine.CurrentCamoraIndex);

            FirearmsAnimator.SetFire(true);
            DryShot();
            _hasFired = true;
            _lastFireTime = 0f;
            return;
        }
        FirearmsAnimator.SetFire(true);
        DryShot();
        _hasFired = true;
        _lastFireTime = 0f;
    }

    private void HandleRevolverShot(ShotInfoPacket packet, InventoryController inventoryController)
    {
        var revolver = Weapon;
        var cylinderMagazine = (CylinderMagazineItemClass)revolver.GetCurrentMagazine();

        var ammo = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate, null);
        _observedPlayer.TurnOffFbbikAt = Time.time + 0.6f;
        InitiateShot(Item, ammo, packet.ShotPosition, packet.ShotDirection,
            CurrentFireport.position, packet.ChamberIndex, packet.Overheat);

        var pitchMult = method_61();
        WeaponSoundPlayer.FireBullet(ammo, packet.ShotPosition, packet.ShotDirection,
            pitchMult, Malfunction, false, IsBirstOf2Start);

        SetMalfAndDurability(packet, revolver);

        revolver.CylinderHammerClosed = revolver.FireMode.FireMode == Weapon.EFireMode.doubleaction;
        FirearmsAnimator.SetCamoraFireIndex(cylinderMagazine.CurrentCamoraIndex);
        var firstIndex = cylinderMagazine.GetCamoraFireOrLoadStartIndex(!revolver.CylinderHammerClosed);
        var cylinderAmmo = cylinderMagazine.GetFirstAmmo(!revolver.CylinderHammerClosed);
        if (cylinderAmmo != null)
        {
            var removeOperation = cylinderMagazine.RemoveAmmoInCamora(cylinderAmmo, inventoryController);
            if (removeOperation.Failed)
            {
                FikaPlugin.Instance.FikaLogger.LogError($"Error removing ammo from cylinderMagazine on netId [{_observedPlayer.NetId}], error: {removeOperation.Error}");
            }
            inventoryController.CheckChamber(revolver, false);
            cylinderAmmo.IsUsed = true;
            revolver.ShellsInChambers[firstIndex] = cylinderAmmo.AmmoTemplate;
        }
        if (revolver.CylinderHammerClosed || revolver.FireMode.FireMode != Weapon.EFireMode.doubleaction)
        {
            cylinderMagazine.IncrementCamoraIndex(false);
        }
        FirearmsAnimator.SetCamoraIndex(cylinderMagazine.CurrentCamoraIndex);
        if (cylinderMagazine.Count > 0)
        {
            revolver.CylinderHammerClosed = revolver.FireMode.FireMode == Weapon.EFireMode.doubleaction;
        }
        FirearmsAnimator.SetDoubleAction(Convert.ToSingle(revolver.CylinderHammerClosed));
        FirearmsAnimator.SetHammerArmed(!revolver.CylinderHammerClosed);
        _weaponManager.MoveAmmoFromChamberToShellPort(true, firstIndex);

        FirearmsAnimator.SetAmmoOnMag(cylinderMagazine.Count);

        FirearmsAnimator.SetFire(true);

        if (revolver.MalfState.State == Weapon.EMalfunctionState.None)
        {
            if (IsRevolver && Weapon.CylinderHammerClosed)
            {
                FirearmsAnimator.Animator.Play(FirearmsAnimator.FullDoubleActionFireStateName, 1, 0.2f);
            }
            else
            {
                FirearmsAnimator.Animator.Play(FirearmsAnimator.FullFireStateName, 1, 0.2f);
            }
        }

        FirearmsAnimator.SetFire(false);
    }

    private void HandleObservedShot(ShotInfoPacket packet, InventoryController inventoryController)
    {
        var ammo = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate, null);
        _observedPlayer.TurnOffFbbikAt = Time.time + 0.6f;
        InitiateShot(Item, ammo, packet.ShotPosition, packet.ShotDirection,
            CurrentFireport.position, packet.ChamberIndex, packet.Overheat);

        var weapon = Weapon;
        SetMalfAndDurability(packet, weapon);

        if (_stationaryWeapon)
        {
            _player.MovementContext.StationaryWeapon.ObservedShot();
        }

        if (weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
        {
            _triggerPressed = true;
        }

        var pitchMult = method_61();
        WeaponSoundPlayer.FireBullet(ammo, packet.ShotPosition, packet.ShotDirection,
            pitchMult, Malfunction, false, IsBirstOf2Start);

        _triggerPressed = false;
        _hasFired = true;
        _lastFireTime = 0f;
        if (weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
        {
            _needsReset = true;
        }

        var magazine = weapon.GetCurrentMagazine();

        FirearmsAnimator.SetFire(true);

        if (weapon.MalfState.State == Weapon.EMalfunctionState.None)
        {
            if (weapon.FireMode.FireMode == Weapon.EFireMode.semiauto)
            {
                FirearmsAnimator.Animator.Play(FirearmsAnimator.FullSemiFireStateName, 1, 0.2f);
            }
            else
            {
                FirearmsAnimator.Animator.Play(FirearmsAnimator.FullFireStateName, 1, 0.2f);
            }
        }

        if (packet.UnderbarrelShot)
        {
            if (UnderbarrelWeapon != null)
            {
                if (UnderbarrelWeapon.Chamber.ContainedItem is AmmoItemClass grenadeBullet && !grenadeBullet.IsUsed)
                {
                    grenadeBullet.IsUsed = true;
                    UnderbarrelWeapon.Chamber.RemoveItem();
                    _underBarrelManager?.DestroyPatronInWeapon();
                }
                FirearmsAnimator.SetFire(false);
                return;
            }

            if (weapon.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
            {
                var slot = weapon.FirstLoadedChamberSlot;
                var index = Array.IndexOf(weapon.Chambers, slot);
                if (slot.ContainedItem is AmmoItemClass grenadeBullet && !grenadeBullet.IsUsed)
                {
                    grenadeBullet.IsUsed = true;
                    slot.RemoveItem();
                    _weaponManager.MoveAmmoFromChamberToShellPort(true, index);
                    weapon.ShellsInChambers[index] = grenadeBullet.AmmoTemplate;
                    FirearmsAnimator.SetAmmoInChamber(weapon.ChamberAmmoCount);
                    FirearmsAnimator.SetShellsInWeapon(weapon.ShellsInWeaponCount);
                }

                FirearmsAnimator.SetFire(false);
                return;
            }
        }

        var hasChambers = weapon.HasChambers;
        if (hasChambers)
        {
            if (weapon.ReloadMode is Weapon.EReloadMode.OnlyBarrel)
            {
                if (weapon.FireMode.FireMode == Weapon.EFireMode.single)
                {
                    var firstLoadedSlot = weapon.FirstLoadedChamberSlot;
                    var index = Array.IndexOf(weapon.Chambers, firstLoadedSlot);
                    HandleBarrelOnlyShot(weapon, index);
                }
                else
                {
                    for (var i = 0; i < weapon.Chambers.Length; i++)
                    {
                        HandleBarrelOnlyShot(weapon, i);
                    }
                }
            }
            else
            {
                weapon.Chambers[0].RemoveItem();
                HandleShellEvent(_weaponManager, packet.ChamberIndex, ammo, magazine);
            }
            FirearmsAnimator.SetAmmoInChamber(weapon.ChamberAmmoCount);
        }

        ammo.IsUsed = true;

        /*magazine is not CylinderMagazineItemClass &&*/
        if (magazine?.Count > 0 && !weapon.BoltAction)
        {
            if (hasChambers && magazine.IsAmmoCompatible(Item.Chambers) && Item.Chambers[0].ContainedItem == null)
            {
                magazine.Cartridges.PopTo(inventoryController, Item.Chambers[0].CreateItemAddress());
                FirearmsAnimator.SetAmmoInChamber(weapon.ChamberAmmoCount);
            }
            else
            {
                magazine.Cartridges.PopToNowhere(inventoryController);
            }
        }

        if (weapon.IsBoltCatch && weapon.ChamberAmmoCount == 1 && !weapon.ManualBoltCatch && !weapon.MustBoltBeOpennedForExternalReload && !weapon.MustBoltBeOpennedForInternalReload)
        {
            FirearmsAnimator.SetBoltCatch(false);
        }

        if (ammo.AmmoTemplate.IsLightAndSoundShot)
        {
            method_62(packet.ShotPosition, packet.ShotDirection);
            LightAndSoundShot(packet.ShotPosition, packet.ShotDirection, ammo.AmmoTemplate);
        }
    }

    private void SetMalfAndDurability(ShotInfoPacket packet, Weapon weapon)
    {
        weapon.Repairable.Durability = Mathf.Clamp(packet.Durability, 0f, weapon.Repairable.MaxDurability);

        weapon.MalfState.LastShotOverheat = packet.LastShotOverheat;
        weapon.MalfState.LastShotTime = packet.LastShotTime;
        weapon.MalfState.SlideOnOverheatReached = packet.SlideOnOverheatReached;
    }

    /// <summary>
    /// Handles a shot when the reload mode is <see cref="Weapon.EReloadMode.OnlyBarrel"/>
    /// </summary>
    /// <param name="weapon">The weapon to handle</param>
    /// <param name="index">Index of the chamber</param>
    private void HandleBarrelOnlyShot(Weapon weapon, int index)
    {
        var ammoSlot = weapon.Chambers[index];
        if (ammoSlot.ContainedItem is AmmoItemClass bullet && !bullet.IsUsed)
        {
            bullet.IsUsed = true;
            if (!bullet.AmmoTemplate.RemoveShellAfterFire)
            {
                _weaponManager.MoveAmmoFromChamberToShellPort(bullet.IsUsed, index);
            }
            else
            {
                _weaponManager.DestroyPatronInWeapon();
            }
            ammoSlot.RemoveItem();
        }
    }

    public void HandleObservedBoltAction()
    {
        FirearmsAnimator.SetBoltActionReload(true);
        FirearmsAnimator.SetFire(true);

        _boltActionReload = true;
    }

    public List<AmmoItemClass> FindAmmoByIds(string[] ammoIds)
    {
        _preallocatedAmmoList.Clear();
        foreach (var id in ammoIds)
        {
            var gstruct = _player.FindItemById(id);
            if (gstruct.Succeeded && gstruct.Value is AmmoItemClass bulletClass)
            {
                _preallocatedAmmoList.Add(bulletClass);
            }
        }
        return _preallocatedAmmoList;
    }


    private void HandleShellEvent(WeaponManagerClass weaponEffectsManager, int chamberIndex, AmmoItemClass ammo, MagazineItemClass magazine)
    {
        weaponEffectsManager.DestroyPatronInWeapon(chamberIndex);
        if (!ammo.AmmoTemplate.RemoveShellAfterFire)
        {
            weaponEffectsManager.CreatePatronInShellPort(ammo, chamberIndex);
            FirearmsAnimator.SetShellsInWeapon(1);
        }
        else
        {
            FirearmsAnimator.SetShellsInWeapon(0);
        }

        var boltAction = Weapon.BoltAction;

        if (magazine != null && !boltAction)
        {
            weaponEffectsManager.SetRoundIntoWeapon(ammo, 0);
        }

        if (Weapon.IsBoltCatch && Weapon.ChamberAmmoCount == 0 && Weapon.GetCurrentMagazine() != null && Weapon.GetCurrentMagazineCount() == 0 && !Weapon.ManualBoltCatch && !boltAction)
        {
            FirearmsAnimator.SetBoltCatch(true);
            FirearmsAnimator.SetAmmoInChamber(0);
        }
    }

    private class ObservedIdleOperation(FirearmController controller) : GClass2037(controller)
    {
        public override void ProcessRemoveOneOffWeapon()
        {
            // Do nothing
        }
    }

    private class ObservedThrowPatronOperation1(FirearmController controller) : MutliBarrelReloadOperationClass(controller)
    {
        private readonly ObservedFirearmController _observedController = (ObservedFirearmController)controller;

        public override void Start(ReloadMultiBarrelResultClass reloadMultiBarrelResult, Callback callback)
        {
            _observedController._isThrowingPatron = true;
            base.Start(reloadMultiBarrelResult, callback);
        }
    }

    private class ObservedThrowPatronOperation2(FirearmController controller) : SingleBarrelReloadOperationClass(controller)
    {
        private readonly ObservedFirearmController _observedController = (ObservedFirearmController)controller;

        public override void Start(ReloadSingleBarrelResultClass reloadSingleBarrelResult, Callback callback)
        {
            _observedController._isThrowingPatron = true;
            base.Start(reloadSingleBarrelResult, callback);
        }
    }

    private class ObservedThrowPatronOperation3(FirearmController controller) : FixMalfunctionOperationClass(controller)
    {
        private readonly ObservedFirearmController _observedController = (ObservedFirearmController)controller;

        public override void Start()
        {
            _observedController._isThrowingPatron = true;
            base.Start();
        }
    }

    private class ObservedThrowPatronOperation4(FirearmController controller) : RechamberOperationClass(controller)
    {
        private readonly ObservedFirearmController _observedController = (ObservedFirearmController)controller;

        public override void Start(AmmoItemClass ammo, Callback callback)
        {
            _observedController._isThrowingPatron = true;
            base.Start(ammo, callback);
        }
    }
}
