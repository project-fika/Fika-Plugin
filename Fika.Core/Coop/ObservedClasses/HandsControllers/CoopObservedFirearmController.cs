// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static EFT.Player;

// HandsControllerClass::method_10(GClass2249)
// GClass2370::method_39(GClass2296)

namespace Fika.Core.Coop.ObservedClasses
{
    public class CoopObservedFirearmController : FirearmController
    {
        public CoopPlayer coopPlayer;
        bool triggerPressed = false;
        bool needsReset = false;
        float lastFireTime = 0f;
        public override bool IsTriggerPressed => triggerPressed;
        private float overlapCounter = 0f;
        private float aimMovementSpeed = 1f;
        private bool hasFired = false;
        private WeaponPrefab weaponPrefab;
        private GClass1582 underBarrelManager;
        public override bool IsAiming
        {
            get => base.IsAiming;
            set
            {
                if (!value)
                {
                    _player.Physical.HoldBreath(false);
                }
                if (_isAiming == value)
                {
                    return;
                }

                _isAiming = value;
                _player.Skills.FastAimTimer.Target = value ? 0f : 2f;
                _player.MovementContext.SetAimingSlowdown(IsAiming, 0.33f + aimMovementSpeed);
                _player.Physical.Aim((!_isAiming || !(_player.MovementContext.StationaryWeapon == null)) ? 0f : ErgonomicWeight);
                coopPlayer.ProceduralWeaponAnimation.IsAiming = _isAiming;
            }
        }

        public override Vector3 WeaponDirection => -CurrentFireport.up;

        protected void Awake()
        {
            coopPlayer = GetComponent<CoopPlayer>();
        }

        protected void Start()
        {
            _objectInHandsAnimator.SetAiming(false);
            aimMovementSpeed = coopPlayer.Skills.GetWeaponInfo(Item).AimMovementSpeed;
            weaponPrefab = ControllerGameObject.GetComponent<WeaponPrefab>();
            if (UnderbarrelWeapon != null)
            {
                underBarrelManager = Traverse.Create(this).Field("gclass1582_0").GetValue<GClass1582>();
            }
        }

        public static CoopObservedFirearmController Create(CoopPlayer player, Weapon weapon)
        {
            return smethod_5<CoopObservedFirearmController>(player, weapon);
        }

        public override void ManualUpdate(float deltaTime)
        {
            base.ManualUpdate(deltaTime);
            if (Time.time - lastFireTime > 0.05f)
            {
                if (hasFired)
                {
                    FirearmsAnimator.SetFire(false);
                    hasFired = false;
                }
                if (needsReset)
                {
                    needsReset = false;
                    WeaponSoundPlayer.OnBreakLoop();
                }
            }
        }

        public override void WeaponOverlapping()
        {
            SetWeaponOverlapValue(coopPlayer.observedOverlap);
            ObservedOverlapView();
            if (overlapCounter <= 1f)
            {
                overlapCounter += Time.deltaTime / 1f;
            }
            if (coopPlayer.leftStanceDisabled && coopPlayer.MovementContext.LeftStanceEnabled && overlapCounter > 1f)
            {
                coopPlayer.MovementContext.LeftStanceController.DisableLeftStanceAnimFromHandsAction();
                overlapCounter = 0f;
            }
            if (!coopPlayer.MovementContext.LeftStanceController.LastAnimValue && !coopPlayer.leftStanceDisabled && coopPlayer.MovementContext.LeftStanceEnabled && overlapCounter > 1f)
            {
                coopPlayer.MovementContext.LeftStanceController.SetAnimatorLeftStanceToCacheFromHandsAction();
                overlapCounter = 0f;
            }
        }

        private void ObservedOverlapView()
        {
            Vector3 vector = _player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Get();
            if (coopPlayer.observedOverlap < 0.02f)
            {
                _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = coopPlayer.observedOverlap;
                _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = true;
            }
            else
            {
                _player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = false;
                _player.ProceduralWeaponAnimation.TurnAway.OriginZShift = vector.y;
                _player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = coopPlayer.observedOverlap;
            }
        }

        public override void OnPlayerDead()
        {
            triggerPressed = false;
            SetTriggerPressed(false);
            WeaponSoundPlayer.enabled = false;

            coopPlayer.HandsAnimator.Animator.Update(Time.fixedDeltaTime);
            ManualUpdate(Time.fixedDeltaTime);
            if (CurrentOperation.State != EOperationState.Finished)
            {
                CurrentOperation.FastForward();
            }

            base.OnPlayerDead();
        }

        public override void SetScopeMode(GStruct164[] scopeStates)
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

        public void HandleFirearmPacket(in WeaponPacket packet, InventoryControllerClass inventoryController)
        {
            if (packet.HasShotInfo)
            {
                // TODO: Flares, GClass2376::method_12

                if (packet.ShotInfoPacket.ShotType != EShotType.RegularShot && packet.ShotInfoPacket.ShotType != EShotType.DryFire)
                {
                    switch (packet.ShotInfoPacket.ShotType)
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

                    if (string.IsNullOrEmpty(packet.ShotInfoPacket.AmmoTemplate))
                    {
                        FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleFirearmPacket: AmmoTemplate was null or empty!");
                        return;
                    }

                    Weapon.MalfState.MalfunctionedAmmo = (BulletClass)Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), packet.ShotInfoPacket.AmmoTemplate, null);
                    if (weaponPrefab != null)
                    {
                        weaponPrefab.InitMalfunctionState(Weapon, false, false, out _);
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleFirearmPacket: WeaponPrefab was null!");
                    }
                }
                else if (packet.ShotInfoPacket.ShotType == EShotType.DryFire)
                {
                    DryShot();
                }
                else if (packet.ShotInfoPacket.ShotType == EShotType.RegularShot)
                {
                    if (string.IsNullOrEmpty(packet.ShotInfoPacket.AmmoTemplate))
                    {
                        FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleFirearmPacket: AmmoTemplate was null or empty!");
                        return;
                    }

                    BulletClass ammo = (BulletClass)Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), packet.ShotInfoPacket.AmmoTemplate, null);
                    InitiateShot(packet.ShotInfoPacket.UnderbarrelShot ? UnderbarrelWeapon : Item, ammo, packet.ShotInfoPacket.ShotPosition, packet.ShotInfoPacket.ShotDirection,
                        packet.ShotInfoPacket.FireportPosition, packet.ShotInfoPacket.ChamberIndex, packet.ShotInfoPacket.Overheat);

                    if (Weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
                    {
                        triggerPressed = true;
                    }

                    float pitchMult = method_55();
                    WeaponSoundPlayer.FireBullet(ammo, packet.ShotInfoPacket.ShotPosition, packet.ShotInfoPacket.ShotDirection,
                        pitchMult, Malfunction, false, IsBirstOf2Start);

                    Weapon.MalfState.LastShotOverheat = packet.ShotInfoPacket.LastShotOverheat;
                    Weapon.MalfState.LastShotTime = packet.ShotInfoPacket.LastShotTime;
                    Weapon.MalfState.SlideOnOverheatReached = packet.ShotInfoPacket.SlideOnOverheatReached;

                    triggerPressed = false;
                    hasFired = true;
                    lastFireTime = Time.time;
                    if (Weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
                    {
                        needsReset = true;
                    }

                    MagazineClass magazine = Weapon.GetCurrentMagazine();

                    FirearmsAnimator.SetFire(true);

                    if (packet.ShotInfoPacket.UnderbarrelShot)
                    {
                        if (UnderbarrelWeapon.Chamber.ContainedItem is BulletClass grenadeBullet && !grenadeBullet.IsUsed)
                        {
                            grenadeBullet.IsUsed = true;
                            UnderbarrelWeapon.Chamber.RemoveItem();
                            underBarrelManager?.DestroyPatronInWeapon();
                        }
                        FirearmsAnimator.SetFire(false);
                        return;
                    }

                    if (Weapon.HasChambers)
                    {
                        if (Weapon.ReloadMode is Weapon.EReloadMode.OnlyBarrel)
                        {
                            for (int i = 0; i < Weapon.Chambers.Length; i++)
                            {
                                if (Weapon.Chambers[i].ContainedItem is BulletClass bClass && !bClass.IsUsed)
                                {
                                    bClass.IsUsed = true;
                                    if (weaponPrefab != null && weaponPrefab.ObjectInHands is GClass1668 weaponEffectsManager)
                                    {
                                        if (!bClass.AmmoTemplate.RemoveShellAfterFire)
                                        {
                                            weaponEffectsManager.MoveAmmoFromChamberToShellPort(bClass.IsUsed, i);
                                        }
                                        else
                                        {
                                            weaponEffectsManager.DestroyPatronInWeapon();
                                        }
                                    }
                                    if (!bClass.AmmoTemplate.RemoveShellAfterFire)
                                    {
                                        Weapon.ShellsInChambers[i] = bClass.AmmoTemplate;
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Weapon.Chambers[0].RemoveItem();
                            if (weaponPrefab != null && weaponPrefab.ObjectInHands is GClass1668 weaponEffectsManager)
                            {
                                HandleShellEvent(weaponEffectsManager, packet, ammo, magazine);
                            }
                        }
                    }

                    // Remember to check if classes increment
                    if (Weapon is GClass2696)
                    {
                        Weapon.CylinderHammerClosed = Weapon.FireMode.FireMode == Weapon.EFireMode.doubleaction;

                        if (magazine is CylinderMagazineClass cylinderMagazine)
                        {
                            BulletClass cylinderAmmo = cylinderMagazine.GetFirstAmmo(!Weapon.CylinderHammerClosed);
                            if (cylinderAmmo != null)
                            {
                                cylinderAmmo.IsUsed = true;
                                cylinderMagazine.RemoveAmmoInCamora(cylinderAmmo, inventoryController);
                                FirearmsAnimator.SetAmmoOnMag(cylinderMagazine.Count);
                                if (!cylinderAmmo.AmmoTemplate.RemoveShellAfterFire)
                                {
                                    Weapon.ShellsInChambers[cylinderMagazine.CurrentCamoraIndex] = cylinderAmmo.AmmoTemplate;
                                }
                            }
                            if (Weapon.CylinderHammerClosed)
                            {
                                cylinderMagazine.IncrementCamoraIndex(false);
                            }
                            FirearmsAnimator.SetCamoraIndex(cylinderMagazine.CurrentCamoraIndex);
                            FirearmsAnimator.SetDoubleAction(Convert.ToSingle(Weapon.CylinderHammerClosed));
                            FirearmsAnimator.SetHammerArmed(!Weapon.CylinderHammerClosed);
                        }
                    }

                    ammo.IsUsed = true;

                    if (magazine != null && magazine is not CylinderMagazineClass && !Weapon.BoltAction)
                    {
                        if (Item.HasChambers)
                        {
                            magazine.Cartridges.PopTo(inventoryController, new GClass2767(Item.Chambers[0]));
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
                        method_56(packet.ShotInfoPacket.ShotPosition, packet.ShotInfoPacket.ShotDirection);
                        LightAndSoundShot(packet.ShotInfoPacket.ShotPosition, packet.ShotInfoPacket.ShotDirection, ammo.AmmoTemplate);
                    }
                }
            }

            if (packet.ChangeFireMode)
            {
                ChangeFireMode(packet.FireMode);
            }

            if (packet.ExamineWeapon)
            {
                ExamineWeapon();
            }

            if (packet.ToggleAim)
            {
                SetAim(packet.AimingIndex);
            }

            if (packet.CheckAmmo)
            {
                CheckAmmo();
            }

            if (packet.CheckChamber)
            {
                CheckChamber();
            }

            if (packet.CheckFireMode)
            {
                CheckFireMode();
            }

            if (packet.ToggleTacticalCombo)
            {
                SetLightsState(packet.LightStatesPacket.LightStates, true);
            }

            if (packet.ChangeSightMode)
            {
                SetScopeMode(packet.ScopeStatesPacket.GStruct164);
            }

            if (packet.ToggleLauncher)
            {
                ToggleLauncher();
            }

            if (packet.EnableInventory)
            {
                SetInventoryOpened(packet.InventoryStatus);
            }

            if (packet.HasReloadMagPacket)
            {
                if (packet.ReloadMagPacket.Reload)
                {
                    MagazineClass magazine = null;
                    try
                    {
                        Item item = coopPlayer.FindItem(packet.ReloadMagPacket.MagId);
                        if (item is MagazineClass magazineClass)
                        {
                            magazine = magazineClass;
                        }
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleFirearmPacket::ReloadMagPacket: Item was not MagazineClass, it was {item.GetType()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError(ex);
                        FikaPlugin.Instance.FikaLogger.LogError($"CoopObservedFirearmController::HandleFirearmPacket: There is no item {packet.ReloadMagPacket.MagId} in profile {coopPlayer.ProfileId}");
                        throw;
                    }
                    GClass2769 gridItemAddress = null;
                    if (packet.ReloadMagPacket.LocationDescription != null)
                    {
                        using MemoryStream memoryStream = new(packet.ReloadMagPacket.LocationDescription);
                        using BinaryReader binaryReader = new(memoryStream);
                        try
                        {
                            if (packet.ReloadMagPacket.LocationDescription.Length != 0)
                            {
                                GClass1528 descriptor = binaryReader.ReadEFTGridItemAddressDescriptor();
                                gridItemAddress = inventoryController.ToGridItemAddress(descriptor);
                            }
                        }
                        catch (GException4 exception2)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError(exception2);
                        }
                    }
                    if (magazine != null)
                    {
                        ReloadMag(magazine, gridItemAddress, null);
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogError($"CoopObservedFirearmController::HandleFirearmPacket::ReloadMag final variables were null! Mag: {magazine}, Address: {gridItemAddress}");
                    }
                }
            }


            if (packet.HasQuickReloadMagPacket)
            {
                if (packet.QuickReloadMagPacket.Reload)
                {
                    MagazineClass magazine;
                    try
                    {
                        Item item = coopPlayer.FindItem(packet.QuickReloadMagPacket.MagId);
                        magazine = item as MagazineClass;
                        if (magazine == null)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"CoopObservedFirearmController::HandleFirearmPacket::QuickReloadMag could not cast {packet.ReloadMagPacket.MagId} as a magazine, got {item.ShortName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError(ex);
                        FikaPlugin.Instance.FikaLogger.LogError($"CoopObservedFirearmController: There is no item {packet.ReloadMagPacket.MagId} in profile {coopPlayer.ProfileId}");
                        throw;
                    }
                    QuickReloadMag(magazine, null);
                }
            }

            if (packet.HasReloadWithAmmoPacket)
            {
                if (packet.ReloadWithAmmo.Status == FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.AbortReload)
                {
                    CurrentOperation.SetTriggerPressed(true);
                }

                if (packet.ReloadWithAmmo.Reload)
                {
                    if (packet.ReloadWithAmmo.Status == FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload)
                    {
                        List<BulletClass> bullets = FindAmmoByIds(packet.ReloadWithAmmo.AmmoIds);
                        GClass2495 ammoPack = new(bullets);
                        if (!packet.HasCylinderMagPacket)
                        {
                            CurrentOperation.ReloadWithAmmo(ammoPack, null, null);
                        }
                        else
                        {
                            CurrentOperation.ReloadCylinderMagazine(ammoPack, null, null);
                        }
                    }

                    if (packet.CylinderMag.Changed && Weapon.GetCurrentMagazine() is CylinderMagazineClass cylinderMagazine)
                    {
                        cylinderMagazine.SetCurrentCamoraIndex(packet.CylinderMag.CamoraIndex);
                        Weapon.CylinderHammerClosed = packet.CylinderMag.HammerClosed;
                    }
                }
            }

            /*if (packet.HasCylinderMagPacket)
            {
                if (packet.ReloadWithAmmo.Reload && packet.CylinderMag.Changed)
                {
                    if (packet.ReloadWithAmmo.Status == FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload)
                    {
                        List<BulletClass> bullets = FindAmmoByIds(packet.ReloadWithAmmo.AmmoIds);
                        GClass2495 ammoPack = new(bullets);
                        ReloadCylinderMagazine(ammoPack, null);
                    }
                }

                if (packet.ReloadWithAmmo.Status == FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.AbortReload)
                {
                    CurrentOperation.SetTriggerPressed(true);
                }
            }*/

            if (packet.HasRollCylinder && Weapon is GClass2696 rollWeapon)
            {
                RollCylinder(packet.RollToZeroCamora);
            }

            if (packet.HasReloadLauncherPacket)
            {
                if (packet.ReloadLauncher.Reload)
                {
                    List<BulletClass> ammo = FindAmmoByIds(packet.ReloadLauncher.AmmoIds);
                    GClass2495 ammoPack = new(ammo);
                    ReloadGrenadeLauncher(ammoPack, null);
                }
            }

            if (packet.HasReloadBarrelsPacket)
            {
                if (packet.ReloadBarrels.Reload)
                {
                    List<BulletClass> ammo = FindAmmoByIds(packet.ReloadBarrels.AmmoIds);

                    GClass2495 ammoPack = new(ammo);

                    GClass2769 gridItemAddress = null;

                    using MemoryStream memoryStream = new(packet.ReloadBarrels.LocationDescription);
                    using BinaryReader binaryReader = new(memoryStream);
                    try
                    {
                        if (packet.ReloadBarrels.LocationDescription.Length > 0)
                        {
                            GClass1528 descriptor = binaryReader.ReadEFTGridItemAddressDescriptor();
                            gridItemAddress = inventoryController.ToGridItemAddress(descriptor);
                        }
                    }
                    catch (GException4 exception2)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError(exception2);
                    }

                    if (ammoPack != null)
                    {
                        ReloadBarrels(ammoPack, gridItemAddress, null);
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogError($"CoopObservedFirearmController::HandleFirearmPacket::ReloadBarrel final variables were null! Ammo: {ammoPack}, Address: {gridItemAddress}");
                    }
                }
            }

            if (packet.HasStanceChange)
            {
                if (coopPlayer.MovementContext.LeftStanceEnabled != packet.LeftStanceState)
                {
                    ChangeLeftStance();
                }
            }

            if (packet.HasFlareShot)
            {
                BulletClass bulletClass = (BulletClass)Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), packet.FlareShotPacket.AmmoTemplateId, null);
                InitiateFlare(bulletClass, packet.FlareShotPacket.ShotPosition, packet.FlareShotPacket.ShotForward);
            }

            if (packet.ReloadBoltAction)
            {
                StartCoroutine(ObservedBoltAction(FirearmsAnimator, this, inventoryController));
            }

            if (packet.UnderbarrelSightingRangeUp)
            {
                UnderbarrelSightingRangeUp();
            }

            if (packet.UnderbarrelSightingRangeDown)
            {
                UnderbarrelSightingRangeDown();
            }
        }

        private IEnumerator ObservedBoltAction(FirearmsAnimator animator, FirearmController controller, InventoryControllerClass inventoryController)
        {
            animator.SetBoltActionReload(true);
            animator.SetFire(true);

            yield return new WaitForSeconds(0.75f);

            if (weaponPrefab != null && weaponPrefab.ObjectInHands is GClass1668 weaponEffectsManager)
            {
                weaponEffectsManager.StartSpawnShell(coopPlayer.Velocity * 0.33f, 0);
            }

            MagazineClass magazine = controller.Item.GetCurrentMagazine();

            if (controller.Item.GetCurrentMagazine() != null && magazine is not CylinderMagazineClass)
            {
                magazine.Cartridges.PopTo(inventoryController, new GClass2767(controller.Item.Chambers[0]));
            }

            animator.SetBoltActionReload(false);
            animator.SetFire(false);
        }

        private void HandleShellEvent(GClass1668 weaponEffectsManager, WeaponPacket packet, BulletClass ammo, MagazineClass magazine)
        {
            weaponEffectsManager.DestroyPatronInWeapon(packet.ShotInfoPacket.ChamberIndex);
            if (!ammo.AmmoTemplate.RemoveShellAfterFire)
            {
                weaponEffectsManager.CreatePatronInShellPort(ammo, packet.ShotInfoPacket.ChamberIndex);
                FirearmsAnimator.SetShellsInWeapon(1);
            }
            else
            {
                FirearmsAnimator.SetShellsInWeapon(0);
            }

            if (magazine != null && !Weapon.BoltAction)
            {
                weaponEffectsManager.SetRoundIntoWeapon(ammo, 0);
            }

            if (Weapon is GClass2696 || Weapon.ReloadMode == Weapon.EReloadMode.OnlyBarrel || Weapon.BoltAction)
            {
                return;
            }

            weaponEffectsManager.StartSpawnShell(coopPlayer.Velocity * 0.33f, 0);
        }
    }
}
