// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using static Fika.Core.Networking.FirearmSubPackets;

namespace Fika.Core.Coop.ObservedClasses
{
    public class CoopObservedFirearmController : FirearmController
    {
        public WeaponManagerClass WeaponManager
        {
            get
            {
                return weaponManager;
            }
        }
        public override bool IsTriggerPressed
        {
            get
            {
                return triggerPressed;
            }
        }

        private ObservedCoopPlayer coopPlayer;
        private bool triggerPressed;
        private bool needsReset;
        private float lastFireTime = 0f;
        private float overlapCounter = 0f;
        private float aimMovementSpeed = 1f;
        private bool hasFired = false;
        private WeaponPrefab weaponPrefab;
        private WeaponManagerClass weaponManager;
        private GClass1775 underBarrelManager;
        private bool boltActionReload;
        private bool isThrowingPatron;

        public override bool IsAiming
        {
            get
            {
                return base.IsAiming;
            }
            set
            {
                if (!value)
                {
                    _player.Physical.HoldBreath(false);
                }
                if (_isAiming == value)
                {
                    method_64(); // Reset animator flags
                    return;
                }
                _isAiming = value;
                _player.Skills.FastAimTimer.Target = value ? 0f : 2f;
                _player.MovementContext.SetAimingSlowdown(IsAiming, 0.33f + aimMovementSpeed);
                _player.Physical.Aim((!_isAiming || !(_player.MovementContext.StationaryWeapon == null)) ? 0f : ErgonomicWeight);
                method_63(_isAiming); // Set animator flags
                coopPlayer.ProceduralWeaponAnimation.IsAiming = _isAiming;
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
            operationFactoryDelegates[typeof(GClass1801)] = new OperationFactoryDelegate(Idle1);
            operationFactoryDelegates[typeof(GClass1785)] = new OperationFactoryDelegate(ThrowPatron1);
            operationFactoryDelegates[typeof(GClass1786)] = new OperationFactoryDelegate(ThrowPatron2);
            operationFactoryDelegates[typeof(GClass1812)] = new OperationFactoryDelegate(ThrowPatron3);
            operationFactoryDelegates[typeof(GClass1815)] = new OperationFactoryDelegate(ThrowPatron4);
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
            aimMovementSpeed = coopPlayer.Skills.GetWeaponInfo(Item).AimMovementSpeed;
            WeaponPrefab prefab = ControllerGameObject.GetComponent<WeaponPrefab>();
            weaponPrefab = prefab;
            weaponManager = weaponPrefab.ObjectInHands as WeaponManagerClass;
            if (UnderbarrelWeapon != null)
            {
                underBarrelManager = Traverse.Create(this).Field<GClass1775>("GClass1775_0").Value;
            }
        }

        public static CoopObservedFirearmController Create(ObservedCoopPlayer player, Weapon weapon)
        {
            CoopObservedFirearmController controller = smethod_6<CoopObservedFirearmController>(player, weapon);
            controller.coopPlayer = player;
            return controller;
        }

        public override bool CanStartReload()
        {
            return true;
        }

        public override void SetAim(int scopeIndex)
        {
            Item.AimIndex.Value = Mathf.Max(0, scopeIndex);
            SetObservedAim(scopeIndex >= 0);
        }

        private void SetObservedAim(bool isAiming)
        {
            IsAiming = isAiming;

            // Lacyway: Unsure if this is needed, remove later if it is
            /*if (Weapon is GClass3111 && coopPlayer.ProceduralWeaponAnimation.IsAiming != isAiming)
            {
                FirearmsAnimator.SetAiming(!isAiming);
            }*/

            _player.method_58(0.2f, false);
            if (isAiming)
            {
                method_60();
            }
        }

        public override void ManualUpdate(float deltaTime)
        {
            base.ManualUpdate(deltaTime);
            if (hasFired)
            {
                lastFireTime += deltaTime;
                if (lastFireTime > 0.1f)
                {
                    FirearmsAnimator.SetFire(false);
                    hasFired = false;
                    if (needsReset)
                    {
                        needsReset = false;
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
            if (Item is RevolverItemClass)
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
            if (!coopPlayer.ShouldOverlap)
            {
                return;
            }

            SetWeaponOverlapValue(coopPlayer.ObservedOverlap);
            ObservedOverlapView();
            if (overlapCounter <= 1f)
            {
                overlapCounter += Time.deltaTime / 1f;
            }
            if (coopPlayer.LeftStanceDisabled && coopPlayer.MovementContext.LeftStanceEnabled && overlapCounter > 1f)
            {
                coopPlayer.MovementContext.LeftStanceController.DisableLeftStanceAnimFromHandsAction();
                overlapCounter = 0f;
            }
            if (!coopPlayer.MovementContext.LeftStanceController.LastAnimValue && !coopPlayer.LeftStanceDisabled && coopPlayer.MovementContext.LeftStanceEnabled && overlapCounter > 1f)
            {
                coopPlayer.MovementContext.LeftStanceController.SetAnimatorLeftStanceToCacheFromHandsAction();
                overlapCounter = 0f;
            }

            coopPlayer.ShouldOverlap = false;
        }

        private void ObservedOverlapView()
        {
            if (coopPlayer.ObservedOverlap < 0.02f)
            {
                _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = coopPlayer.ObservedOverlap;
                _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = true;
                coopPlayer.ShouldOverlap = false;
                return;
            }

            Vector3 vector = _player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Get();
            _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = false;
            _player.ProceduralWeaponAnimation.TurnAway.OriginZShift = vector.y;
            _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = coopPlayer.ObservedOverlap;
        }

        public override void OnPlayerDead()
        {
            try
            {
                base.OnPlayerDead();
                triggerPressed = false;
                SetTriggerPressed(false);

                needsReset = false;
                WeaponSoundPlayer.OnBreakLoop();

                coopPlayer.HandsAnimator.Animator.Update(Time.fixedDeltaTime);
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
            if (isThrowingPatron)
            {
                isThrowingPatron = false;
                CurrentOperation.OnShellEjectEvent();
                return;
            }

            weaponManager.StartSpawnShell(coopPlayer.Velocity * 0.66f, 0);
            if (boltActionReload)
            {
                MagazineItemClass magazine = Item.GetCurrentMagazine();
                Weapon weapon = Weapon;
                if (magazine != null && magazine is not CylinderMagazineItemClass && weapon.HasChambers)
                {
                    magazine.Cartridges.PopTo(coopPlayer.InventoryController, Item.Chambers[0].CreateItemAddress());
                }

                FirearmsAnimator.SetBoltActionReload(false);
                FirearmsAnimator.SetFire(false);

                boltActionReload = false;
            }
        }

        private IEnumerator BreakFiringLoop()
        {
            WeaponSoundPlayer.Release();
            Traverse<bool> isFiring = Traverse.Create(WeaponSoundPlayer).Field<bool>("_isFiring");
            int attempts = 0;
            while (isFiring.Value && attempts < 10)
            {
                yield return new WaitForEndOfFrame();
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

        public void HandleShotInfoPacket(ref ShotInfoPacket packet, InventoryController inventoryController)
        {
            if (packet.ShotType == EShotType.DryFire)
            {
                FirearmsAnimator.SetFire(true);
                DryShot();
                hasFired = true;
                lastFireTime = 0f;
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

                if (!packet.AmmoTemplate.HasValue)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleShotInfoPacket: AmmoTemplate was null!");
                    return;
                }

                AmmoItemClass ammo = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate.Value, null);
                Weapon.MalfState.MalfunctionedAmmo = ammo;
                Weapon.MalfState.AmmoToFire = ammo;

                FirearmsAnimator.MisfireSlideUnknown(false);
                weaponPrefab.InitMalfunctionState(Weapon, false, false, out _);
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
                    hasFired = true;
                    lastFireTime = 0f;
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
                            AmmoItemClass fedAmmo = (AmmoItemClass)currentMagazine.Cartridges.PopToNowhere(coopPlayer.InventoryController).Value.ResultItem;
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
                        weaponManager.MoveAmmoFromChamberToShellPort(true, 0);
                    }
                }
            }

            HandleObservedShot(ref packet, inventoryController);
        }

        // Leave here for now - Lacyway
        /*private AmmoItemClass GetFedAmmoFromMalfunction(MagazineItemClass currentMagazine)
        {
            if (!Weapon.HasChambers)
            {
                return (AmmoItemClass)currentMagazine.Cartridges.PopToNowhere(coopPlayer.InventoryController).Value.ResultItem;
            }

            Slot chamberSlot = Weapon.Chambers[0];
            if (chamberSlot.ContainedItem != null)
            {
                chamberSlot.RemoveItemWithoutRestrictions();
            }
            return (AmmoItemClass)currentMagazine.Cartridges.PopTo(coopPlayer.InventoryController, chamberSlot.CreateItemAddress()).Value.ResultItem;
        }*/

        private void HandleObservedShot(ref ShotInfoPacket packet, InventoryController inventoryController)
        {
            if (!packet.AmmoTemplate.HasValue)
            {
                FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleObservedShot: AmmoTemplate was null!");
                return;
            }

            AmmoItemClass ammo = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate.Value, null);
            InitiateShot(Item, ammo, packet.ShotPosition, packet.ShotDirection,
                CurrentFireport.position, packet.ChamberIndex, packet.Overheat);

            if (Weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
            {
                triggerPressed = true;
            }

            float pitchMult = method_61();
            WeaponSoundPlayer.FireBullet(ammo, packet.ShotPosition, packet.ShotDirection,
                pitchMult, Malfunction, false, IsBirstOf2Start);

            Weapon.MalfState.LastShotOverheat = packet.LastShotOverheat;
            Weapon.MalfState.LastShotTime = packet.LastShotTime;
            Weapon.MalfState.SlideOnOverheatReached = packet.SlideOnOverheatReached;

            triggerPressed = false;
            hasFired = true;
            lastFireTime = 0f;
            if (Weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
            {
                needsReset = true;
            }

            bool isRevolver = Weapon is RevolverItemClass;

            MagazineItemClass magazine = Weapon.GetCurrentMagazine();

            FirearmsAnimator.SetFire(true);

            if (Weapon.MalfState.State == Weapon.EMalfunctionState.None)
            {
                if (isRevolver && Weapon.CylinderHammerClosed)
                {
                    FirearmsAnimator.Animator.Play(FirearmsAnimator.FullDoubleActionFireStateName, 1, 0.2f);
                }
                else if (Weapon.FireMode.FireMode == Weapon.EFireMode.semiauto)
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
                        underBarrelManager?.DestroyPatronInWeapon();
                    }
                    FirearmsAnimator.SetFire(false);
                    return;
                }

                if (Weapon.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
                {
                    Slot slot = Weapon.FirstLoadedChamberSlot;
                    int index = Weapon.Chambers.IndexOf(slot);
                    if (slot.ContainedItem is AmmoItemClass grenadeBullet && !grenadeBullet.IsUsed)
                    {
                        grenadeBullet.IsUsed = true;
                        slot.RemoveItem();
                        weaponManager.MoveAmmoFromChamberToShellPort(true, index);
                        Weapon.ShellsInChambers[index] = grenadeBullet.AmmoTemplate;
                        FirearmsAnimator.SetAmmoInChamber(Weapon.ChamberAmmoCount);
                        FirearmsAnimator.SetShellsInWeapon(Weapon.ShellsInWeaponCount);
                    }
                }
            }

            bool hasChambers = Weapon.HasChambers;
            if (hasChambers)
            {
                if (Weapon.ReloadMode is Weapon.EReloadMode.OnlyBarrel)
                {
                    for (int i = 0; i < Weapon.Chambers.Length; i++)
                    {
                        if (Weapon.Chambers[i].ContainedItem is AmmoItemClass bClass && !bClass.IsUsed)
                        {
                            bClass.IsUsed = true;
                            if (!bClass.AmmoTemplate.RemoveShellAfterFire)
                            {
                                weaponManager.MoveAmmoFromChamberToShellPort(bClass.IsUsed, i);
                            }
                            else
                            {
                                weaponManager.DestroyPatronInWeapon();
                            }
                            if (!bClass.AmmoTemplate.RemoveShellAfterFire)
                            {
                                Weapon.ShellsInChambers[i] = bClass.AmmoTemplate;
                            }
                        }
                    }
                }
                else
                {
                    Weapon.Chambers[0].RemoveItem(false);
                    HandleShellEvent(weaponManager, packet.ChamberIndex, ammo, magazine);
                }
                FirearmsAnimator.SetAmmoInChamber(Weapon.ChamberAmmoCount);
            }

            if (isRevolver)
            {
                if (magazine is CylinderMagazineItemClass cylinderMagazine)
                {
                    FirearmsAnimator.SetCamoraFireIndex(cylinderMagazine.CurrentCamoraIndex);
                    int firstIndex = cylinderMagazine.GetCamoraFireOrLoadStartIndex(!Weapon.CylinderHammerClosed);
                    AmmoItemClass cylinderAmmo = cylinderMagazine.GetFirstAmmo(!Weapon.CylinderHammerClosed);
                    if (cylinderAmmo != null)
                    {
                        GStruct452<GInterface398> removeOperation = cylinderMagazine.RemoveAmmoInCamora(cylinderAmmo, inventoryController);
                        if (removeOperation.Failed)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"Error removing ammo from cylinderMagazine on netId {coopPlayer.NetId}, error: {removeOperation.Error}");
                        }
                        inventoryController.CheckChamber(Weapon, false);
                        cylinderAmmo.IsUsed = true;
                        Weapon.ShellsInChambers[firstIndex] = cylinderAmmo.AmmoTemplate;
                    }
                    if (Weapon.CylinderHammerClosed || Weapon.FireMode.FireMode != Weapon.EFireMode.doubleaction)
                    {
                        cylinderMagazine.IncrementCamoraIndex(false);
                    }
                    FirearmsAnimator.SetCamoraIndex(cylinderMagazine.CurrentCamoraIndex);
                    FirearmsAnimator.SetDoubleAction(Convert.ToSingle(Weapon.CylinderHammerClosed));
                    FirearmsAnimator.SetHammerArmed(!Weapon.CylinderHammerClosed);
                    weaponManager.MoveAmmoFromChamberToShellPort(cylinderAmmo.IsUsed, firstIndex);

                    FirearmsAnimator.SetAmmoOnMag(cylinderMagazine.Count);

                    if (cylinderMagazine.Cartridges.Count > 0)
                    {
                        Weapon.CylinderHammerClosed = Weapon.FireMode.FireMode == Weapon.EFireMode.doubleaction;
                    }
                }
            }

            ammo.IsUsed = true;

            if (magazine != null && magazine is not CylinderMagazineItemClass && magazine.Count > 0 && !Weapon.BoltAction)
            {
                if (hasChambers && magazine.IsAmmoCompatible(Item.Chambers) && Item.Chambers[0].ContainedItem == null)
                {
                    magazine.Cartridges.PopTo(inventoryController, Item.Chambers[0].CreateItemAddress());
                    FirearmsAnimator.SetAmmoInChamber(Weapon.ChamberAmmoCount);
                }
                else
                {
                    magazine.Cartridges.PopToNowhere(inventoryController);
                }
            }

            if (Weapon.IsBoltCatch && Weapon.ChamberAmmoCount == 1 && !Weapon.ManualBoltCatch && !Weapon.MustBoltBeOpennedForExternalReload && !Weapon.MustBoltBeOpennedForInternalReload)
            {
                FirearmsAnimator.SetBoltCatch(false);
            }

            if (ammo.AmmoTemplate.IsLightAndSoundShot)
            {
                method_62(packet.ShotPosition, packet.ShotDirection);
                LightAndSoundShot(packet.ShotPosition, packet.ShotDirection, ammo.AmmoTemplate);
            }
        }

        public void HandleObservedBoltAction()
        {
            FirearmsAnimator.SetBoltActionReload(true);
            FirearmsAnimator.SetFire(true);

            boltActionReload = true;
        }

        public List<AmmoItemClass> FindAmmoByIds(string[] ammoIds)
        {
            _preallocatedAmmoList.Clear();
            foreach (string id in ammoIds)
            {
                GStruct454<Item> gstruct = _player.FindItemById(id);
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

        private class ObservedIdleOperation(FirearmController controller) : GClass1801(controller)
        {
            public override void ProcessRemoveOneOffWeapon()
            {
                // Do nothing
            }
        }

        private class ObservedThrowPatronOperation1(FirearmController controller) : GClass1785(controller)
        {
            private readonly CoopObservedFirearmController observedController = (CoopObservedFirearmController)controller;

            public override void Start(GClass1772 reloadMultiBarrelResult, Callback callback)
            {
                observedController.isThrowingPatron = true;
                base.Start(reloadMultiBarrelResult, callback);
            }
        }

        private class ObservedThrowPatronOperation2(FirearmController controller) : GClass1786(controller)
        {
            private readonly CoopObservedFirearmController observedController = (CoopObservedFirearmController)controller;

            public override void Start(GClass1773 reloadSingleBarrelResult, Callback callback)
            {
                observedController.isThrowingPatron = true;
                base.Start(reloadSingleBarrelResult, callback);
            }
        }

        private class ObservedThrowPatronOperation3(FirearmController controller) : GClass1812(controller)
        {
            private readonly CoopObservedFirearmController observedController = (CoopObservedFirearmController)controller;

            public override void Start()
            {
                observedController.isThrowingPatron = true;
                base.Start();
            }
        }

        private class ObservedThrowPatronOperation4(FirearmController controller) : GClass1815(controller)
        {
            private readonly CoopObservedFirearmController observedController = (CoopObservedFirearmController)controller;

            public override void Start(AmmoItemClass ammo, Callback callback)
            {
                observedController.isThrowingPatron = true;
                base.Start(ammo, callback);
            }
        }
    }
}
