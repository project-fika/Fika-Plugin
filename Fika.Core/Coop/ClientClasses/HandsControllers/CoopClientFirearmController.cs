// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
    public class CoopClientFirearmController : Player.FirearmController
    {
        public CoopPlayer coopPlayer;

        private void Awake()
        {
            coopPlayer = GetComponent<CoopPlayer>();
        }

        public static CoopClientFirearmController Create(CoopPlayer player, Weapon weapon)
        {
            return smethod_5<CoopClientFirearmController>(player, weapon);
        }

        public override void SetWeaponOverlapValue(float overlap)
        {
            base.SetWeaponOverlapValue(overlap);
            coopPlayer.observedOverlap = overlap;
        }

        public override void WeaponOverlapping()
        {
            base.WeaponOverlapping();
            coopPlayer.leftStanceDisabled = DisableLeftStanceByOverlap;
        }

        public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
        {
            Dictionary<Type, OperationFactoryDelegate> operationFactoryDelegates = base.GetOperationFactoryDelegates();
            operationFactoryDelegates[typeof(GClass1588)] = new OperationFactoryDelegate(Weapon1);
            operationFactoryDelegates[typeof(GClass1589)] = new OperationFactoryDelegate(Weapon2);
            operationFactoryDelegates[typeof(GClass1601)] = new OperationFactoryDelegate(Weapon3);
            return operationFactoryDelegates;
        }

        public Player.GClass1583 Weapon1()
        {
            if (Item.ReloadMode == Weapon.EReloadMode.InternalMagazine && Item.Chambers.Length == 0)
            {
                return new FirearmClass2(this);
            }
            if (Item.MustBoltBeOpennedForInternalReload)
            {
                return new FirearmClass3(this);
            }
            return new FirearmClass2(this);
        }

        public Player.GClass1583 Weapon2()
        {
            return new FirearmClass1(this);
        }

        public Player.GClass1583 Weapon3()
        {
            if (Item.IsFlareGun)
            {
                return new GClass1605(this);
            }
            if (Item.IsOneOff)
            {
                return new GClass1607(this);
            }
            if (Item.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
            {
                return new GClass1604(this);
            }
            if (Item is GClass2696)
            {
                return new GClass1603(this);
            }
            if (!Item.BoltAction)
            {
                return new GClass1601(this);
            }
            return new FirearmClass4(this);
        }

        public override bool CheckChamber()
        {
            bool flag = base.CheckChamber();
            if (flag)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    CheckChamber = true
                });
            }
            return flag;
        }

        public override bool CheckAmmo()
        {
            bool flag = base.CheckAmmo();
            if (flag)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    CheckAmmo = true
                });
            }
            return flag;
        }

        public override bool ChangeFireMode(Weapon.EFireMode fireMode)
        {
            bool flag = base.ChangeFireMode(fireMode);
            if (flag)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    ChangeFireMode = true,
                    FireMode = fireMode
                });
            }
            return flag;
        }

        public override void ChangeAimingMode()
        {
            base.ChangeAimingMode();
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                ToggleAim = true,
                AimingIndex = IsAiming ? Item.AimIndex.Value : -1
            });
        }

        public override void SetAim(bool value)
        {
            bool isAiming = IsAiming;
            bool aimingInterruptedByOverlap = AimingInterruptedByOverlap;
            base.SetAim(value);
            if (IsAiming != isAiming || aimingInterruptedByOverlap)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    ToggleAim = true,
                    AimingIndex = IsAiming ? Item.AimIndex.Value : -1
                });
            }
        }

        public override bool CheckFireMode()
        {
            bool flag = base.CheckFireMode();
            if (flag)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    CheckFireMode = true
                });
            }
            return flag;
        }

        public override void DryShot(int chamberIndex = 0, bool underbarrelShot = false)
        {
            base.DryShot(chamberIndex, underbarrelShot);
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasShotInfo = true,
                ShotInfoPacket = new()
                {
                    IsPrimaryActive = true,
                    ShotType = EShotType.DryFire,
                    AmmoAfterShot = underbarrelShot ? 0 : Item.GetCurrentMagazineCount(),
                    ChamberIndex = chamberIndex,
                    UnderbarrelShot = underbarrelShot
                }
            });
        }

        public override bool ExamineWeapon()
        {
            bool flag = base.ExamineWeapon();
            if (flag)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    ExamineWeapon = true
                });
            }
            return flag;
        }

        public override void InitiateShot(GInterface322 weapon, BulletClass ammo, Vector3 shotPosition, Vector3 shotDirection, Vector3 fireportPosition, int chamberIndex, float overheat)
        {
            EShotType shotType = new();

            switch (weapon.MalfState.State)
            {
                case Weapon.EMalfunctionState.None:
                    shotType = EShotType.RegularShot;
                    break;
                case Weapon.EMalfunctionState.Misfire:
                    shotType = EShotType.Misfire;
                    break;
                case Weapon.EMalfunctionState.Jam:
                    shotType = EShotType.JamedShot;
                    break;
                case Weapon.EMalfunctionState.HardSlide:
                    shotType = EShotType.HardSlidedShot;
                    break;
                case Weapon.EMalfunctionState.SoftSlide:
                    shotType = EShotType.SoftSlidedShot;
                    break;
                case Weapon.EMalfunctionState.Feed:
                    shotType = EShotType.Feed;
                    break;
            }

            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasShotInfo = true,
                ShotInfoPacket = new()
                {
                    IsPrimaryActive = (weapon == Item),
                    ShotType = shotType,
                    AmmoAfterShot = weapon.GetCurrentMagazineCount(),
                    ShotPosition = shotPosition,
                    ShotDirection = shotDirection,
                    FireportPosition = fireportPosition,
                    ChamberIndex = chamberIndex,
                    Overheat = overheat,
                    UnderbarrelShot = Weapon.IsUnderBarrelDeviceActive,
                    AmmoTemplate = ammo.AmmoTemplate._id,
                    LastShotOverheat = weapon.MalfState.LastShotOverheat,
                    LastShotTime = weapon.MalfState.LastShotTime,
                    SlideOnOverheatReached = weapon.MalfState.SlideOnOverheatReached
                }
            });

            coopPlayer.StatisticsManager.OnShot(Weapon, ammo);

            base.InitiateShot(weapon, ammo, shotPosition, shotDirection, fireportPosition, chamberIndex, overheat);
        }

        public override void QuickReloadMag(MagazineClass magazine, Callback callback)
        {
            if (!CanStartReload())
            {
                return;
            }

            base.QuickReloadMag(magazine, callback);

            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasQuickReloadMagPacket = true,
                QuickReloadMagPacket = new()
                {
                    Reload = true,
                    MagId = magazine.Id
                }
            });
        }

        public override void ReloadBarrels(GClass2495 ammoPack, GClass2769 placeToPutContainedAmmoMagazine, Callback callback)
        {
            if (!CanStartReload() && ammoPack.AmmoCount < 1)
            {
                return;
            }

            ReloadBarrelsHandler handler = new(coopPlayer, placeToPutContainedAmmoMagazine, ammoPack);
            CurrentOperation.ReloadBarrels(ammoPack, placeToPutContainedAmmoMagazine, callback, new Callback(handler.Process));
        }

        public override void ReloadCylinderMagazine(GClass2495 ammoPack, Callback callback, bool quickReload = false)
        {
            if (Blindfire)
            {
                return;
            }
            if (Item.GetCurrentMagazine() == null)
            {
                return;
            }
            if (!CanStartReload())
            {
                return;
            }

            ReloadCylinderMagazineHandler handler = new(coopPlayer, this, quickReload, ammoPack.GetReloadingAmmoIds(), [], (CylinderMagazineClass)Item.GetCurrentMagazine());
            Weapon.GetShellsIndexes(handler.shellsIndexes);
            CurrentOperation.ReloadCylinderMagazine(ammoPack, callback, new Callback(handler.Process), handler.quickReload);
        }

        public override void ReloadGrenadeLauncher(GClass2495 ammoPack, Callback callback)
        {
            if (!CanStartReload())
            {
                return;
            }

            string[] reloadingAmmoIds = ammoPack.GetReloadingAmmoIds();
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasReloadLauncherPacket = true,
                ReloadLauncher = new()
                {
                    Reload = true,
                    AmmoIds = reloadingAmmoIds
                }
            });

            CurrentOperation.ReloadGrenadeLauncher(ammoPack, callback);
        }

        public override void UnderbarrelSightingRangeUp()
        {
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                UnderbarrelSightingRangeUp = true
            });
            base.UnderbarrelSightingRangeUp();
        }

        public override void UnderbarrelSightingRangeDown()
        {
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                UnderbarrelSightingRangeDown = true
            });
            base.UnderbarrelSightingRangeDown();
        }

        public override void ReloadMag(MagazineClass magazine, GClass2769 gridItemAddress, Callback callback)
        {
            if (!CanStartReload() || Blindfire)
            {
                return;
            }

            ReloadMagHandler handler = new(coopPlayer, gridItemAddress, magazine);
            CurrentOperation.ReloadMag(magazine, gridItemAddress, callback, new Callback(handler.Process));
        }

        public override void ReloadWithAmmo(GClass2495 ammoPack, Callback callback)
        {
            if (Item.GetCurrentMagazine() == null)
            {
                return;
            }
            if (!CanStartReload())
            {
                return;
            }

            ReloadWithAmmoHandler handler = new(coopPlayer, ammoPack.GetReloadingAmmoIds());
            CurrentOperation.ReloadWithAmmo(ammoPack, callback, new Callback(handler.Process));
        }

        public override void SetLightsState(GStruct163[] lightsStates, bool force = false)
        {
            if (force || CurrentOperation.CanChangeLightState(lightsStates))
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    ToggleTacticalCombo = true,
                    LightStatesPacket = new()
                    {
                        Amount = lightsStates.Length,
                        LightStates = lightsStates
                    }
                });
            }
            base.SetLightsState(lightsStates, force);
        }

        public override void SetScopeMode(GStruct164[] scopeStates)
        {
            if (!CurrentOperation.CanChangeScopeStates(scopeStates))
            {
                return;
            }

            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                ChangeSightMode = true,
                ScopeStatesPacket = new()
                {
                    Amount = scopeStates.Length,
                    GStruct164 = scopeStates
                }
            });

            base.SetScopeMode(scopeStates);
        }

        public override void ShotMisfired(BulletClass ammo, Weapon.EMalfunctionState malfunctionState, float overheat)
        {
            EShotType shotType = new();

            switch (malfunctionState)
            {
                case Weapon.EMalfunctionState.Misfire:
                    shotType = EShotType.Misfire;
                    break;
                case Weapon.EMalfunctionState.Jam:
                    shotType = EShotType.JamedShot;
                    break;
                case Weapon.EMalfunctionState.HardSlide:
                    shotType = EShotType.HardSlidedShot;
                    break;
                case Weapon.EMalfunctionState.SoftSlide:
                    shotType = EShotType.SoftSlidedShot;
                    break;
                case Weapon.EMalfunctionState.Feed:
                    shotType = EShotType.Feed;
                    break;
            }

            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasShotInfo = true,
                ShotInfoPacket = new()
                {
                    IsPrimaryActive = true,
                    ShotType = shotType,
                    AmmoAfterShot = Item.GetCurrentMagazineCount(),
                    Overheat = overheat,
                    AmmoTemplate = ammo.TemplateId
                }
            });

            base.ShotMisfired(ammo, malfunctionState, overheat);
        }

        public override bool ToggleLauncher()
        {
            bool flag = base.ToggleLauncher();
            if (flag)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    ToggleLauncher = true
                });
            }
            return flag;
        }

        public override void Loot(bool p)
        {
            base.Loot(p);
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                Loot = p
            });
        }

        public override void SetInventoryOpened(bool opened)
        {
            base.SetInventoryOpened(opened);
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                EnableInventory = true,
                InventoryStatus = opened
            });
        }

        public override void ChangeLeftStance()
        {
            base.ChangeLeftStance();
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasStanceChange = true,
                LeftStanceState = coopPlayer.MovementContext.LeftStanceEnabled
            });
        }

        public override void SendStartOneShotFire()
        {
            base.SendStartOneShotFire();
        }

        public override void CreateFlareShot(BulletClass flareItem, Vector3 shotPosition, Vector3 forward)
        {
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasFlareShot = true,
                FlareShotPacket = new()
                {
                    ShotPosition = shotPosition,
                    ShotForward = forward,
                    AmmoTemplateId = flareItem.TemplateId
                }
            });
            base.CreateFlareShot(flareItem, shotPosition, forward);
        }

        private void SendAbortReloadPacket(int amount)
        {
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasReloadWithAmmoPacket = true,
                ReloadWithAmmo = new()
                {
                    Reload = true,
                    Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.AbortReload,
                    AmmoLoadedToMag = amount
                }
            });
        }

        public override void RollCylinder(bool rollToZeroCamora)
        {
            if (Blindfire || IsAiming)
            {
                return;
            }

            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasRollCylinder = true,
                RollToZeroCamora = rollToZeroCamora
            });

            CurrentOperation.RollCylinder(null, rollToZeroCamora);
        }

        private void SendEndReloadPacket(int amount)
        {
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                HasReloadWithAmmoPacket = true,
                ReloadWithAmmo = new()
                {
                    Reload = true,
                    Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.EndReload,
                    AmmoLoadedToMag = amount
                }
            });
        }

        private void SendBoltActionReloadPacket()
        {
            coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
            {
                ReloadBoltAction = true
            });
        }

        private class FirearmClass1(Player.FirearmController controller) : GClass1589(controller)
        {
            public override void SetTriggerPressed(bool pressed)
            {
                bool bool_ = bool_1;
                base.SetTriggerPressed(pressed);
                if (bool_1 && !bool_)
                {
                    coopClientFirearmController.SendAbortReloadPacket(int_0);
                }
            }

            public override void SwitchToIdle()
            {
                coopClientFirearmController.SendEndReloadPacket(int_0);
                method_13();
                base.SwitchToIdle();
            }

            private CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
        }

        private class FirearmClass2(Player.FirearmController controller) : GClass1590(controller)
        {
            public override void SetTriggerPressed(bool pressed)
            {
                bool bool_ = bool_1;
                base.SetTriggerPressed(pressed);
                if (bool_1 && !bool_)
                {
                    coopClientFirearmController.SendAbortReloadPacket(int_0);
                }
            }

            public override void SwitchToIdle()
            {
                coopClientFirearmController.SendEndReloadPacket(int_0);
                base.SwitchToIdle();
            }

            private readonly CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
        }

        private class FirearmClass3(Player.FirearmController controller) : GClass1591(controller)
        {
            public override void SetTriggerPressed(bool pressed)
            {
                bool bool_ = bool_1;
                base.SetTriggerPressed(pressed);
                if (bool_1 && !bool_)
                {
                    coopClientFirearmController.SendAbortReloadPacket(int_0);
                }
            }

            public override void SwitchToIdle()
            {
                coopClientFirearmController.SendEndReloadPacket(int_0);
                base.SwitchToIdle();
            }

            private readonly CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
        }

        private class FirearmClass4(Player.FirearmController controller) : GClass1602(controller)
        {
            public override void Start()
            {
                base.Start();
                SendBoltActionReloadPacket(!firearmController_0.IsTriggerPressed);
            }

            public override void SetTriggerPressed(bool pressed)
            {
                base.SetTriggerPressed(pressed);
                SendBoltActionReloadPacket(!firearmController_0.IsTriggerPressed);
            }

            public override void SetInventoryOpened(bool opened)
            {
                base.SetInventoryOpened(opened);
                SendBoltActionReloadPacket(true);
            }

            public override void ReloadMag(MagazineClass magazine, GClass2769 gridItemAddress, Callback finishCallback, Callback startCallback)
            {
                base.ReloadMag(magazine, gridItemAddress, finishCallback, startCallback);
                SendBoltActionReloadPacket(true);
            }

            public override void QuickReloadMag(MagazineClass magazine, Callback finishCallback, Callback startCallback)
            {
                base.QuickReloadMag(magazine, finishCallback, startCallback);
                SendBoltActionReloadPacket(true);
            }

            public override void ReloadWithAmmo(GClass2495 ammoPack, Callback finishCallback, Callback startCallback)
            {
                base.ReloadWithAmmo(ammoPack, finishCallback, startCallback);
                SendBoltActionReloadPacket(true);
            }

            private void SendBoltActionReloadPacket(bool value)
            {
                if (!hasSent && value)
                {
                    hasSent = true;
                    coopClientFirearmController.SendBoltActionReloadPacket();
                }
            }

            public override void Reset()
            {
                base.Reset();
                hasSent = false;
            }

            private CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
            private bool hasSent;
        }

        private class ReloadMagHandler(CoopPlayer coopPlayer, GClass2769 gridItemAddress, MagazineClass magazine)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly GClass2769 gridItemAddress = gridItemAddress;
            private readonly MagazineClass magazine = magazine;

            public void Process(IResult error)
            {
                GClass1528 gridItemAddressDescriptor = (gridItemAddress == null) ? null : GClass1632.FromGridItemAddress(gridItemAddress);

                using MemoryStream memoryStream = new();
                using BinaryWriter binaryWriter = new(memoryStream);
                byte[] locationDescription;
                if (gridItemAddressDescriptor != null)
                {
                    binaryWriter.Write(gridItemAddressDescriptor);
                    locationDescription = memoryStream.ToArray();
                }
                else
                {
                    locationDescription = Array.Empty<byte>();
                }

                if (error.Succeed)
                {
                    coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                    {
                        HasReloadMagPacket = true,
                        ReloadMagPacket = new()
                        {
                            Reload = true,
                            MagId = magazine.Id,
                            LocationDescription = locationDescription,
                        }
                    });
                }
            }
        }

        private class ReloadCylinderMagazineHandler(CoopPlayer coopPlayer, CoopClientFirearmController coopClientFirearmController, bool quickReload, string[] ammoIds, List<int> shellsIndexes, CylinderMagazineClass cylinderMagazine)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly CoopClientFirearmController coopClientFirearmController = coopClientFirearmController;
            public readonly bool quickReload = quickReload;
            private readonly string[] ammoIds = ammoIds;
            public readonly List<int> shellsIndexes = shellsIndexes;
            private readonly CylinderMagazineClass cylinderMagazine = cylinderMagazine;

            public void Process(IResult error)
            {
                coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                {
                    HasReloadWithAmmoPacket = true,
                    ReloadWithAmmo = new()
                    {
                        Reload = true,
                        Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
                        AmmoIds = ammoIds
                    },
                    HasCylinderMagPacket = true,
                    CylinderMag = new()
                    {
                        Changed = true,
                        CamoraIndex = cylinderMagazine.CurrentCamoraIndex,
                        HammerClosed = coopClientFirearmController.Item.CylinderHammerClosed
                    }
                });
            }
        }

        private class ReloadBarrelsHandler(CoopPlayer coopPlayer, GClass2769 placeToPutContainedAmmoMagazine, GClass2495 ammoPack)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly GClass2769 placeToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
            private readonly GClass2495 ammoPack = ammoPack;

            public void Process(IResult error)
            {
                GClass1528 gridItemAddressDescriptor = (placeToPutContainedAmmoMagazine == null) ? null : GClass1632.FromGridItemAddress(placeToPutContainedAmmoMagazine);

                string[] ammoIds = ammoPack.GetReloadingAmmoIds();

                using MemoryStream memoryStream = new();
                using BinaryWriter binaryWriter = new(memoryStream);
                byte[] locationDescription;
                if (gridItemAddressDescriptor != null)
                {
                    binaryWriter.Write(gridItemAddressDescriptor);
                    locationDescription = memoryStream.ToArray();
                }
                else
                {
                    locationDescription = Array.Empty<byte>();
                }

                if (coopPlayer.HealthController.IsAlive)
                {
                    coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                    {
                        HasReloadBarrelsPacket = true,
                        ReloadBarrels = new()
                        {
                            Reload = true,
                            AmmoIds = ammoIds,
                            LocationDescription = locationDescription,
                        }
                    });
                }
            }
        }

        private class ReloadWithAmmoHandler(CoopPlayer coopPlayer, string[] ammoIds)
        {
            private readonly CoopPlayer coopPlayer = coopPlayer;
            private readonly string[] ammoIds = ammoIds;

            public void Process(IResult error)
            {
                if (error.Succeed)
                {
                    coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
                    {
                        HasReloadWithAmmoPacket = true,
                        ReloadWithAmmo = new()
                        {
                            Reload = true,
                            Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
                            AmmoIds = ammoIds
                        }
                    });
                }
            }
        }
    }
}
