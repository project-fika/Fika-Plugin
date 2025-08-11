// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.FirearmController;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using static EFT.Player;

namespace Fika.Core.Main.ObservedClasses
{
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

        private ObservedPlayer _fikaPlayer;
        private bool _triggerPressed;
        private bool _needsReset;
        private float _lastFireTime = 0f;
        private float _overlapCounter = 0f;
        private bool _hasFired = false;
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
                _fikaPlayer.ProceduralWeaponAnimation.IsAiming = _isAiming;
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
            Dictionary<Type, OperationFactoryDelegate> operationFactoryDelegates = base.GetOperationFactoryDelegates();
            operationFactoryDelegates[typeof(GClass1870)] = new OperationFactoryDelegate(Idle1);
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
            WeaponPrefab prefab = ControllerGameObject.GetComponent<WeaponPrefab>();
            _weaponPrefab = prefab;
            _weaponManager = _weaponPrefab.ObjectInHands as WeaponManagerClass;
            if (UnderbarrelWeapon != null)
            {
                Traverse weaponTraverse = Traverse.Create(this);
                _underBarrelManager = weaponTraverse.Field<UnderbarrelManagerClass>("underbarrelManagerClass").Value;
            }
            IsRevolver = Weapon is RevolverItemClass;
            _stationaryWeapon = Weapon.IsStationaryWeapon;
        }

        public static ObservedFirearmController Create(ObservedPlayer player, Weapon weapon)
        {
            ObservedFirearmController controller = smethod_6<ObservedFirearmController>(player, weapon);
            controller._fikaPlayer = player;
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
            if (_player.UsedSimplifiedSkeleton)
            {
                _player.MovementContext.PlayerAnimator.SetAiming(isAiming);
            }
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
            if (!_fikaPlayer.ShouldOverlap)
            {
                return;
            }

            SetWeaponOverlapValue(_fikaPlayer.ObservedOverlap);
            ObservedOverlapView();
            if (_overlapCounter <= 1f)
            {
                _overlapCounter += Time.deltaTime / 1f;
            }
            if (_fikaPlayer.LeftStanceDisabled && _fikaPlayer.MovementContext.LeftStanceEnabled && _overlapCounter > 1f)
            {
                _fikaPlayer.MovementContext.LeftStanceController.DisableLeftStanceAnimFromHandsAction();
                _overlapCounter = 0f;
            }
            if (!_fikaPlayer.MovementContext.LeftStanceController.LastAnimValue && !_fikaPlayer.LeftStanceDisabled && _fikaPlayer.MovementContext.LeftStanceEnabled && _overlapCounter > 1f)
            {
                _fikaPlayer.MovementContext.LeftStanceController.SetAnimatorLeftStanceToCacheFromHandsAction();
                _overlapCounter = 0f;
            }

            _fikaPlayer.ShouldOverlap = false;
        }

        private void ObservedOverlapView()
        {
            if (_fikaPlayer.ObservedOverlap < 0.02f)
            {
                _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = _fikaPlayer.ObservedOverlap;
                _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = true;
                _fikaPlayer.ShouldOverlap = false;
                return;
            }

            Vector3 vector = _player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Get();
            _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = false;
            _player.ProceduralWeaponAnimation.TurnAway.OriginZShift = vector.y;
            _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = _fikaPlayer.ObservedOverlap;
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

                _fikaPlayer.HandsAnimator.Animator.Update(Time.fixedDeltaTime);
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

            _weaponManager.StartSpawnShell(_fikaPlayer.Velocity * 0.66f, 0);
            if (_boltActionReload)
            {
                MagazineItemClass magazine = Item.GetCurrentMagazine();
                Weapon weapon = Weapon;
                if (magazine != null && magazine is not CylinderMagazineItemClass && weapon.HasChambers)
                {
                    magazine.Cartridges.PopTo(_fikaPlayer.InventoryController, Item.Chambers[0].CreateItemAddress());
                }

                FirearmsAnimator.SetBoltActionReload(false);
                FirearmsAnimator.SetFire(false);

                _boltActionReload = false;
            }
        }

        private IEnumerator BreakFiringLoop()
        {
            WeaponSoundPlayer.Release();
            Traverse<bool> isFiring = Traverse.Create(WeaponSoundPlayer).Field<bool>("_isFiring");
            int attempts = 0;
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
        public void HandleRocketShot(AmmoItemClass rocketClass, in Vector3 shotPosition, in Vector3 shotForward)
        {
            FirearmsAnimator.SetFire(true);

            // Handle the rocket shot
            rocketClass.IsUsed = true;
            Transform smokePort = TransformHelperClass.FindTransformRecursiveContains(WeaponRoot.transform, "smokeport", false);
            InitiateRocket(rocketClass, shotPosition, shotForward, smokePort);
            Weapon.FirstLoadedChamberSlot.RemoveItem();
            WeaponManager.MoveAmmoFromChamberToShellPort(true, 0);

            FirearmsAnimator.SetFire(false);
        }

        public void HandleShotInfoPacket(in ShotInfoPacket packet, InventoryController inventoryController)
        {
            if (packet.ShotType == EShotType.DryFire)
            {
                if (IsRevolver)
                {
                    Weapon.CylinderHammerClosed = Weapon.FireMode.FireMode == Weapon.EFireMode.doubleaction;
                }
                FirearmsAnimator.SetFire(true);
                DryShot();
                _hasFired = true;
                _lastFireTime = 0f;
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

                AmmoItemClass ammo = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate, null);
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
                        Slot firstChamber = Weapon.Chambers[0];
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
                    Slot firstChamber = Weapon.Chambers[0];
                    if (firstChamber.ContainedItem is AmmoItemClass)
                    {
                        firstChamber.RemoveItemWithoutRestrictions();
                    }

                    if (Weapon.MalfState.State == Weapon.EMalfunctionState.Feed)
                    {
                        MagazineItemClass currentMagazine = Weapon.GetCurrentMagazine();
                        if (currentMagazine != null)
                        {
                            AmmoItemClass fedAmmo = (AmmoItemClass)currentMagazine.Cartridges.PopToNowhere(_fikaPlayer.InventoryController).Value.ResultItem;
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

            HandleObservedShot(in packet, inventoryController);
        }

        // Leave here for now - Lacyway
        /*private AmmoItemClass GetFedAmmoFromMalfunction(MagazineItemClass currentMagazine)
        {
            if (!Weapon.HasChambers)
            {
                return (AmmoItemClass)currentMagazine.Cartridges.PopToNowhere(fikaPlayer.InventoryController).Value.ResultItem;
            }

            Slot chamberSlot = Weapon.Chambers[0];
            if (chamberSlot.ContainedItem != null)
            {
                chamberSlot.RemoveItemWithoutRestrictions();
            }
            return (AmmoItemClass)currentMagazine.Cartridges.PopTo(fikaPlayer.InventoryController, chamberSlot.CreateItemAddress()).Value.ResultItem;
        }*/

        private void HandleObservedShot(in ShotInfoPacket packet, InventoryController inventoryController)
        {
            AmmoItemClass ammo = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate, null);
            _fikaPlayer.TurnOffFbbikAt = Time.time + 0.6f;
            InitiateShot(Item, ammo, packet.ShotPosition, packet.ShotDirection,
                CurrentFireport.position, packet.ChamberIndex, packet.Overheat);

            Weapon weapon = Weapon;

            weapon.Repairable.Durability = Mathf.Clamp(packet.Durability, 0f, weapon.Repairable.MaxDurability);

            if (_stationaryWeapon)
            {
                _player.MovementContext.StationaryWeapon.ObservedShot();
            }

            if (weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
            {
                _triggerPressed = true;
            }

            float pitchMult = method_61();
            WeaponSoundPlayer.FireBullet(ammo, packet.ShotPosition, packet.ShotDirection,
                pitchMult, Malfunction, false, IsBirstOf2Start);

            weapon.MalfState.LastShotOverheat = packet.LastShotOverheat;
            weapon.MalfState.LastShotTime = packet.LastShotTime;
            weapon.MalfState.SlideOnOverheatReached = packet.SlideOnOverheatReached;

            _triggerPressed = false;
            _hasFired = true;
            _lastFireTime = 0f;
            if (weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
            {
                _needsReset = true;
            }

            MagazineItemClass magazine = weapon.GetCurrentMagazine();

            FirearmsAnimator.SetFire(true);

            if (weapon.MalfState.State == Weapon.EMalfunctionState.None)
            {
                if (IsRevolver && Weapon.CylinderHammerClosed)
                {
                    FirearmsAnimator.Animator.Play(FirearmsAnimator.FullDoubleActionFireStateName, 1, 0.2f);
                }
                else if (weapon.FireMode.FireMode == Weapon.EFireMode.semiauto)
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
                    Slot slot = weapon.FirstLoadedChamberSlot;
                    int index = Array.IndexOf(weapon.Chambers, slot);
                    if (slot.ContainedItem is AmmoItemClass grenadeBullet && !grenadeBullet.IsUsed)
                    {
                        grenadeBullet.IsUsed = true;
                        slot.RemoveItem();
                        _weaponManager.MoveAmmoFromChamberToShellPort(true, index);
                        weapon.ShellsInChambers[index] = grenadeBullet.AmmoTemplate;
                        FirearmsAnimator.SetAmmoInChamber(weapon.ChamberAmmoCount);
                        FirearmsAnimator.SetShellsInWeapon(weapon.ShellsInWeaponCount);
                    }
                }
            }

            bool hasChambers = weapon.HasChambers;
            if (hasChambers)
            {
                if (weapon.ReloadMode is Weapon.EReloadMode.OnlyBarrel)
                {
                    if (weapon.FireMode.FireMode == Weapon.EFireMode.single)
                    {
                        Slot firstLoadedSlot = weapon.FirstLoadedChamberSlot;
                        int index = Array.IndexOf(weapon.Chambers, firstLoadedSlot);
                        HandleBarrelOnlyShot(weapon, index);
                    }
                    else
                    {
                        for (int i = 0; i < weapon.Chambers.Length; i++)
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

            if (IsRevolver)
            {
                if (magazine is CylinderMagazineItemClass cylinderMagazine)
                {
                    FirearmsAnimator.SetCamoraFireIndex(cylinderMagazine.CurrentCamoraIndex);
                    int firstIndex = cylinderMagazine.GetCamoraFireOrLoadStartIndex(!weapon.CylinderHammerClosed);
                    AmmoItemClass cylinderAmmo = cylinderMagazine.GetFirstAmmo(!weapon.CylinderHammerClosed);
                    if (cylinderAmmo != null)
                    {
                        GStruct459<GInterface407> removeOperation = cylinderMagazine.RemoveAmmoInCamora(cylinderAmmo, inventoryController);
                        if (removeOperation.Failed)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"Error removing ammo from cylinderMagazine on netId {_fikaPlayer.NetId}, error: {removeOperation.Error}");
                        }
                        inventoryController.CheckChamber(weapon, false);
                        cylinderAmmo.IsUsed = true;
                        weapon.ShellsInChambers[firstIndex] = cylinderAmmo.AmmoTemplate;
                    }
                    if (weapon.CylinderHammerClosed || weapon.FireMode.FireMode != Weapon.EFireMode.doubleaction)
                    {
                        cylinderMagazine.IncrementCamoraIndex(false);
                    }
                    FirearmsAnimator.SetCamoraIndex(cylinderMagazine.CurrentCamoraIndex);
                    FirearmsAnimator.SetDoubleAction(Convert.ToSingle(weapon.CylinderHammerClosed));
                    FirearmsAnimator.SetHammerArmed(!weapon.CylinderHammerClosed);
                    _weaponManager.MoveAmmoFromChamberToShellPort(true, firstIndex);

                    FirearmsAnimator.SetAmmoOnMag(cylinderMagazine.Count);

                    if (cylinderMagazine.Count > 0)
                    {
                        weapon.CylinderHammerClosed = weapon.FireMode.FireMode == Weapon.EFireMode.doubleaction;
                    }
                }
            }

            ammo.IsUsed = true;

            if (magazine != null && magazine is not CylinderMagazineItemClass && magazine.Count > 0 && !weapon.BoltAction)
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

        /// <summary>
        /// Handles a shot when the reload mode is <see cref="Weapon.EReloadMode.OnlyBarrel"/>
        /// </summary>
        /// <param name="weapon">The weapon to handle</param>
        /// <param name="index">Index of the chamber</param>
        private void HandleBarrelOnlyShot(Weapon weapon, int index)
        {
            Slot ammoSlot = weapon.Chambers[index];
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
                if (!bullet.AmmoTemplate.RemoveShellAfterFire)
                {
                    weapon.ShellsInChambers[index] = bullet.AmmoTemplate;
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
            foreach (string id in ammoIds)
            {
                GStruct461<Item> gstruct = _player.FindItemById(id);
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

            bool boltAction = Weapon.BoltAction;

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

        private class ObservedIdleOperation(FirearmController controller) : GClass1870(controller)
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
}
