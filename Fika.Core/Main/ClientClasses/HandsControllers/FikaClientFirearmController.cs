// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.FirearmController;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Fika.Core.Networking.Packets.FirearmController.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Main.ClientClasses.HandsControllers
{
    public class FikaClientFirearmController : Player.FirearmController
    {
        protected FikaPlayer _fikaPlayer;
        private bool _isClient;
        private bool _isGrenadeLauncher;

        public static FikaClientFirearmController Create(FikaPlayer player, Weapon weapon)
        {
            FikaClientFirearmController controller = smethod_6<FikaClientFirearmController>(player, weapon);
            controller._fikaPlayer = player;
            controller._isClient = FikaBackendUtils.IsClient;
            controller._isGrenadeLauncher = weapon.IsGrenadeLauncher;
            return controller;
        }

        public override void SetWeaponOverlapValue(float overlap)
        {
            base.SetWeaponOverlapValue(overlap);
            _fikaPlayer.ObservedOverlap = overlap;
        }

        public override void WeaponOverlapping()
        {
            base.WeaponOverlapping();
            _fikaPlayer.LeftStanceDisabled = DisableLeftStanceByOverlap;
        }

        public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
        {
            Dictionary<Type, OperationFactoryDelegate> operationFactoryDelegates = base.GetOperationFactoryDelegates();
            operationFactoryDelegates[typeof(AmmoPackReloadOperationClass)] = new OperationFactoryDelegate(Weapon1);
            operationFactoryDelegates[typeof(CylinderReloadOperationClass)] = new OperationFactoryDelegate(Weapon2);
            operationFactoryDelegates[typeof(GenericFireOperationClass)] = new OperationFactoryDelegate(Weapon3);
            return operationFactoryDelegates;
        }

        public override void OnPlayerDead()
        {
            if (IsAiming)
            {
                SetAim(false);
            }
            base.OnPlayerDead();
        }

        public override bool CanStartReload()
        {
            if (_isClient)
            {
                return !_fikaPlayer.WaitingForCallback && base.CanStartReload();
            }

            return base.CanStartReload();
        }

        public override bool CanPressTrigger()
        {
            if (_isClient)
            {
                return !_fikaPlayer.WaitingForCallback && base.CanPressTrigger();
            }

            return base.CanPressTrigger();
        }

        public Player.BaseAnimationOperationClass Weapon1()
        {
            if (Item.ReloadMode is Weapon.EReloadMode.InternalMagazine && Item.Chambers.Length == 0)
            {
                return new FirearmClass2(this);
            }
            if (Item.MustBoltBeOpennedForInternalReload)
            {
                return new FirearmClass3(this);
            }
            return new FirearmClass2(this);
        }

        public Player.BaseAnimationOperationClass Weapon2()
        {
            return new FirearmClass1(this);
        }

        public Player.BaseAnimationOperationClass Weapon3()
        {
            if (Item is RocketLauncherItemClass)
            {
                return new GClass1867(this);
            }
            if (Item.IsFlareGun)
            {
                return new FlareGunFireOperationClass(this);
            }
            if (Item.IsOneOff)
            {
                return new IsOneOffFireOperationClass(this);
            }
            if (Item.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
            {
                return new FireOnlyBarrelFireOperation(this);
            }
            if (Item is RevolverItemClass) // This is a revolver
            {
                return new RevolverFireOperationClass(this);
            }
            if (!Item.BoltAction)
            {
                return new GenericFireOperationClass(this);
            }
            return new FirearmClass4(this);
        }

        public override bool ToggleBipod()
        {
            bool success = base.ToggleBipod();
            if (success)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ToggleBipod
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
            return success;
        }

        public override bool CheckChamber()
        {
            bool flag = base.CheckChamber();
            if (flag)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.CheckChamber
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
            return flag;
        }

        public override bool CheckAmmo()
        {
            bool flag = base.CheckAmmo();
            if (flag)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.CheckAmmo
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
            return flag;
        }

        public override bool ChangeFireMode(Weapon.EFireMode fireMode)
        {
            bool flag = base.ChangeFireMode(fireMode);
            if (flag)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ChangeFireMode,
                    SubPacket = new ChangeFireModePacket()
                    {
                        FireMode = fireMode
                    }
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
            return flag;
        }

        public override void ChangeAimingMode()
        {
            base.ChangeAimingMode();
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ToggleAim,
                SubPacket = new ToggleAimPacket()
                {
                    AimingIndex = IsAiming ? Item.AimIndex.Value : -1
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override void SetAim(bool value)
        {
            bool isAiming = IsAiming;
            bool aimingInterruptedByOverlap = AimingInterruptedByOverlap;
            base.SetAim(value);
            if (IsAiming != isAiming || aimingInterruptedByOverlap && _fikaPlayer.HealthController.IsAlive)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ToggleAim,
                    SubPacket = new ToggleAimPacket()
                    {
                        AimingIndex = IsAiming ? Item.AimIndex.Value : -1
                    }
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
        }

        public override void AimingChanged(bool newValue)
        {
            base.AimingChanged(newValue);
            if (!IsAiming && _fikaPlayer.HealthController.IsAlive)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ToggleAim,
                    SubPacket = new ToggleAimPacket()
                    {
                        AimingIndex = -1
                    }
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
        }

        public override bool CheckFireMode()
        {
            bool flag = base.CheckFireMode();
            if (flag && _fikaPlayer.HealthController.IsAlive)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.CheckFireMode
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
            return flag;
        }

        public override void DryShot(int chamberIndex = 0, bool underbarrelShot = false)
        {
            base.DryShot(chamberIndex, underbarrelShot);
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ShotInfo,
                SubPacket = new ShotInfoPacket(chamberIndex, underbarrelShot, EShotType.DryFire)
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override bool ExamineWeapon()
        {
            bool flag = base.ExamineWeapon();
            if (flag && _fikaPlayer.HealthController.IsAlive)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ExamineWeapon
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
            return flag;
        }

        public override void InitiateShot(IWeapon weapon, AmmoItemClass ammo, Vector3 shotPosition, Vector3 shotDirection, Vector3 fireportPosition, int chamberIndex, float overheat)
        {
            EShotType shotType = default;

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

            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ShotInfo,
                SubPacket = new ShotInfoPacket(shotPosition, shotDirection, ammo.TemplateId, overheat, weapon.MalfState.LastShotOverheat,
                weapon.MalfState.LastShotTime, Weapon.Repairable.Durability, chamberIndex,
                Weapon.IsUnderBarrelDeviceActive || _isGrenadeLauncher, weapon.MalfState.SlideOnOverheatReached, shotType)
            };

            _fikaPlayer.PacketSender.SendPacket(ref packet);
            _fikaPlayer.StatisticsManager.OnShot(Weapon, ammo);

            base.InitiateShot(weapon, ammo, shotPosition, shotDirection, fireportPosition, chamberIndex, overheat);
        }

        public override void QuickReloadMag(MagazineItemClass magazine, Callback callback)
        {
            if (CanStartReload())
            {
                base.QuickReloadMag(magazine, callback);
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.QuickReloadMag,
                    SubPacket = new QuickReloadMagPacket()
                    {
                        Reload = true,
                        MagId = magazine.Id
                    }
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
                return;
            }

            callback?.Fail("Can't start QuickReloadMag");
        }

        public override void ReloadBarrels(AmmoPackReloadingClass ammoPack, ItemAddress placeToPutContainedAmmoMagazine, Callback callback)
        {
            if (CanStartReload() && ammoPack.AmmoCount > 0)
            {
                ReloadBarrelsHandler handler = new(_fikaPlayer, placeToPutContainedAmmoMagazine, ammoPack);
                CurrentOperation.ReloadBarrels(ammoPack, placeToPutContainedAmmoMagazine, callback, handler.Process);
                return;
            }

            callback?.Fail("Can't start ReloadBarrels");
        }

        public override void ReloadCylinderMagazine(AmmoPackReloadingClass ammoPack, Callback callback, bool quickReload = false)
        {
            if (Blindfire)
            {
                return;
            }
            if (Item.GetCurrentMagazine() == null)
            {
                return;
            }
            if (CanStartReload())
            {
                ReloadCylinderMagazineHandler handler = new(_fikaPlayer, this, quickReload, ammoPack.GetReloadingAmmoIds(),
                [], (CylinderMagazineItemClass)Item.GetCurrentMagazine());
                Weapon.GetShellsIndexes(handler.ShellsIndexes);
                CurrentOperation.ReloadCylinderMagazine(ammoPack, callback, handler.Process, handler.QuickReload);
                return;
            }

            callback?.Fail("Can't start ReloadCylinderMagazine");
        }

        public override void ReloadGrenadeLauncher(AmmoPackReloadingClass ammoPack, Callback callback)
        {
            if (CanStartReload())
            {
                string[] reloadingAmmoIds = ammoPack.GetReloadingAmmoIds();
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ReloadLauncher,
                    SubPacket = new ReloadLauncherPacket()
                    {
                        Reload = true,
                        AmmoIds = reloadingAmmoIds
                    }
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);

                CurrentOperation.ReloadGrenadeLauncher(ammoPack, callback);
                return;
            }

            callback?.Fail("Can't start ReloadGrenadeLauncher");
        }

        public override void ReloadMag(MagazineItemClass magazine, ItemAddress itemAddress, Callback callback)
        {
            if (!CanStartReload() || Blindfire)
            {
                return;
            }

            _player.MovementContext.PlayerAnimator.AnimatedInteractions.ForceStopInteractions();
            if (!_player.MovementContext.PlayerAnimator.AnimatedInteractions.IsInteractionPlaying)
            {
                ReloadMagHandler handler = new(_fikaPlayer, itemAddress, magazine);
                CurrentOperation.ReloadMag(magazine, itemAddress, callback, handler.Process);
                return;
            }

            callback?.Fail("Can't start ReloadMag");
        }

        public override void ReloadWithAmmo(AmmoPackReloadingClass ammoPack, Callback callback)
        {
            if (Item.GetCurrentMagazine() == null)
            {
                return;
            }
            if (CanStartReload())
            {
                ReloadWithAmmoHandler handler = new(_fikaPlayer, ammoPack.GetReloadingAmmoIds());
                CurrentOperation.ReloadWithAmmo(ammoPack, callback, handler.Process);
                return;
            }

            callback?.Fail("Can't start ReloadWithAmmo");
        }

        public override bool SetLightsState(FirearmLightStateStruct[] lightsStates, bool force = false, bool animated = true)
        {
            if (force || CurrentOperation.CanChangeLightState(lightsStates))
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ToggleLightStates,
                    SubPacket = new LightStatesPacket()
                    {
                        Amount = lightsStates.Length,
                        States = lightsStates
                    }
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }

            return base.SetLightsState(lightsStates, force);
        }

        public override void SetScopeMode(FirearmScopeStateStruct[] scopeStates)
        {
            SendScopeStates(scopeStates);
            base.SetScopeMode(scopeStates);
        }
        public override void OpticCalibrationSwitchUp(FirearmScopeStateStruct[] scopeStates)
        {
            SendScopeStates(scopeStates);
            base.OpticCalibrationSwitchUp(scopeStates);
        }

        public override void OpticCalibrationSwitchDown(FirearmScopeStateStruct[] scopeStates)
        {
            SendScopeStates(scopeStates);
            base.OpticCalibrationSwitchDown(scopeStates);
        }

        private void SendScopeStates(FirearmScopeStateStruct[] scopeStates)
        {
            if (!CurrentOperation.CanChangeScopeStates(scopeStates))
            {
                return;
            }

            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ToggleScopeStates,
                SubPacket = new ScopeStatesPacket()
                {
                    Amount = scopeStates.Length,
                    States = scopeStates
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override void ShotMisfired(AmmoItemClass ammo, Weapon.EMalfunctionState malfunctionState, float overheat)
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

            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ShotInfo,
                SubPacket = new ShotInfoPacket(ammo.TemplateId, overheat, shotType)
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);

            base.ShotMisfired(ammo, malfunctionState, overheat);
        }

        public override bool ToggleLauncher(Action callback = null)
        {
            bool flag = base.ToggleLauncher(callback);
            if (flag)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ToggleLauncher
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
            return flag;
        }

        public override void Loot(bool p)
        {
            base.Loot(p);
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.Loot
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override void SetInventoryOpened(bool opened)
        {
            base.SetInventoryOpened(opened);
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ToggleInventory,
                SubPacket = new ToggleInventoryPacket()
                {
                    Open = opened
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override void ChangeLeftStance()
        {
            base.ChangeLeftStance();
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.LeftStanceChange,
                SubPacket = new LeftStanceChangePacket()
                {
                    LeftStance = _fikaPlayer.MovementContext.LeftStanceEnabled
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override void SendStartOneShotFire()
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.FlareShot,
                SubPacket = new FlareShotPacket()
                {
                    StartOneShotFire = true
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override void CreateFlareShot(AmmoItemClass flareItem, Vector3 shotPosition, Vector3 forward)
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.FlareShot,
                SubPacket = new FlareShotPacket()
                {
                    ShotPosition = shotPosition,
                    ShotForward = forward,
                    AmmoTemplateId = flareItem.TemplateId
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
            base.CreateFlareShot(flareItem, shotPosition, forward);
        }

        public override void CreateRocketShot(AmmoItemClass rocketItem, Vector3 shotPosition, Vector3 forward, Transform smokeport = null)
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.RocketShot,
                SubPacket = new RocketShotPacket()
                {
                    AmmoTemplateId = rocketItem.TemplateId,
                    ShotPosition = shotPosition,
                    ShotForward = forward
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
            base.CreateRocketShot(rocketItem, shotPosition, forward, smokeport);
        }

        private void SendAbortReloadPacket(int amount)
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ReloadWithAmmo,
                SubPacket = new ReloadWithAmmoPacket()
                {
                    Reload = true,
                    Status = EReloadWithAmmoStatus.AbortReload,
                    AmmoLoadedToMag = amount
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        public override void RollCylinder(bool rollToZeroCamora)
        {
            if (Blindfire || IsAiming)
            {
                return;
            }

            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.RollCylinder,
                SubPacket = new RollCylinderPacket()
                {
                    RollToZeroCamora = rollToZeroCamora
                }
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);

            CurrentOperation.RollCylinder(null, rollToZeroCamora);
        }

        private void SendEndReloadPacket(int amount)
        {
            if (_fikaPlayer.HealthController.IsAlive)
            {
                WeaponPacket packet = new()
                {
                    NetId = _fikaPlayer.NetId,
                    Type = EFirearmSubPacketType.ReloadWithAmmo,
                    SubPacket = new ReloadWithAmmoPacket()
                    {
                        Reload = true,
                        Status = EReloadWithAmmoStatus.EndReload,
                        AmmoLoadedToMag = amount
                    }
                };
                _fikaPlayer.PacketSender.SendPacket(ref packet);
            }
        }

        private void SendBoltActionReloadPacket()
        {
            WeaponPacket packet = new()
            {
                NetId = _fikaPlayer.NetId,
                Type = EFirearmSubPacketType.ReloadBoltAction
            };
            _fikaPlayer.PacketSender.SendPacket(ref packet);
        }

        private class FirearmClass1(Player.FirearmController controller) : CylinderReloadOperationClass(controller)
        {
            public override void SetTriggerPressed(bool pressed)
            {
                bool bool_ = Bool_1;
                base.SetTriggerPressed(pressed);
                if (Bool_1 && !bool_)
                {
                    coopClientFirearmController.SendAbortReloadPacket(Int_0);
                }
            }

            public override void SwitchToIdle()
            {
                coopClientFirearmController.SendEndReloadPacket(Int_0);
                method_13();
                base.SwitchToIdle();
            }

            private FikaClientFirearmController coopClientFirearmController = (FikaClientFirearmController)controller;
        }

        private class FirearmClass2(Player.FirearmController controller) : AmmoPackReloadInternalOneChamberOperationClass(controller)
        {
            public override void SetTriggerPressed(bool pressed)
            {
                bool bool_ = Bool_1;
                base.SetTriggerPressed(pressed);
                if (Bool_1 && !bool_)
                {
                    _coopClientFirearmController.SendAbortReloadPacket(Int_0);
                }
            }

            public override void SwitchToIdle()
            {
                _coopClientFirearmController.SendEndReloadPacket(Int_0);
                base.SwitchToIdle();
            }

            private readonly FikaClientFirearmController _coopClientFirearmController = (FikaClientFirearmController)controller;
        }

        private class FirearmClass3(Player.FirearmController controller) : AmmoPackReloadInternalBoltOpenOperationClass(controller)
        {
            public override void SetTriggerPressed(bool pressed)
            {
                bool bool_ = Bool_1;
                base.SetTriggerPressed(pressed);
                if (Bool_1 && !bool_)
                {
                    _coopClientFirearmController.SendAbortReloadPacket(Int_0);
                }
            }

            public override void SwitchToIdle()
            {
                _coopClientFirearmController.SendEndReloadPacket(Int_0);
                base.SwitchToIdle();
            }

            private readonly FikaClientFirearmController _coopClientFirearmController = (FikaClientFirearmController)controller;
        }

        private class FirearmClass4(Player.FirearmController controller) : DefaultWeaponOperationClass(controller)
        {
            public override void Start()
            {
                base.Start();
                SendBoltActionReloadPacket(!FirearmController_0.IsTriggerPressed);
            }

            public override void SetTriggerPressed(bool pressed)
            {
                base.SetTriggerPressed(pressed);
                SendBoltActionReloadPacket(!FirearmController_0.IsTriggerPressed);
            }

            public override void SetInventoryOpened(bool opened)
            {
                base.SetInventoryOpened(opened);
                SendBoltActionReloadPacket(true);
            }

            public override void ReloadMag(MagazineItemClass magazine, ItemAddress gridItemAddress, Callback finishCallback, Callback startCallback)
            {
                base.ReloadMag(magazine, gridItemAddress, finishCallback, startCallback);
                SendBoltActionReloadPacket(true);
            }

            public override void QuickReloadMag(MagazineItemClass magazine, Callback finishCallback, Callback startCallback)
            {
                base.QuickReloadMag(magazine, finishCallback, startCallback);
                SendBoltActionReloadPacket(true);
            }

            public override void ReloadWithAmmo(AmmoPackReloadingClass ammoPack, Callback finishCallback, Callback startCallback)
            {
                base.ReloadWithAmmo(ammoPack, finishCallback, startCallback);
                SendBoltActionReloadPacket(true);
            }

            private void SendBoltActionReloadPacket(bool value)
            {
                if (!_hasSent && value)
                {
                    _hasSent = true;
                    _coopClientFirearmController.SendBoltActionReloadPacket();
                }
            }

            public override void Reset()
            {
                base.Reset();
                _hasSent = false;
            }

            private FikaClientFirearmController _coopClientFirearmController = (FikaClientFirearmController)controller;
            private bool _hasSent;
        }

        private class ReloadMagHandler(FikaPlayer fikaPlayer, ItemAddress gridItemAddress, MagazineItemClass magazine)
        {
            private readonly FikaPlayer _fikaPlayer = fikaPlayer;
            private readonly ItemAddress _gridItemAddress = gridItemAddress;
            private readonly MagazineItemClass _magazine = magazine;

            public void Process(IResult result)
            {
                ItemAddress itemAddress = _gridItemAddress;
                GClass1785 descriptor = itemAddress?.ToDescriptor();
                EFTWriterClass eftWriter = new();

                byte[] locationDescription;
                if (descriptor != null)
                {
                    eftWriter.WritePolymorph(descriptor);
                    locationDescription = eftWriter.ToArray();
                }
                else
                {
                    locationDescription = [];
                }

                if (_fikaPlayer.HealthController.IsAlive)
                {
                    WeaponPacket packet = new()
                    {
                        NetId = _fikaPlayer.NetId,
                        Type = EFirearmSubPacketType.ReloadMag,
                        SubPacket = new ReloadMagPacket()
                        {
                            Reload = true,
                            MagId = _magazine.Id,
                            LocationDescription = locationDescription,
                        }
                    };
                    _fikaPlayer.PacketSender.SendPacket(ref packet);
                }
            }
        }

        private class ReloadCylinderMagazineHandler(FikaPlayer fikaPlayer, FikaClientFirearmController coopClientFirearmController, bool quickReload, string[] ammoIds, List<int> shellsIndexes, CylinderMagazineItemClass cylinderMagazine)
        {
            private readonly FikaPlayer _fikaPlayer = fikaPlayer;
            private readonly FikaClientFirearmController _coopClientFirearmController = coopClientFirearmController;
            public readonly bool QuickReload = quickReload;
            private readonly string[] _ammoIds = ammoIds;
            public readonly List<int> ShellsIndexes = shellsIndexes;
            private readonly CylinderMagazineItemClass _cylinderMagazine = cylinderMagazine;

            public void Process(IResult result)
            {
                if (_fikaPlayer.HealthController.IsAlive)
                {
                    WeaponPacket packet = new()
                    {
                        NetId = _fikaPlayer.NetId,
                        Type = EFirearmSubPacketType.CylinderMag,
                        SubPacket = new CylinderMagPacket()
                        {
                            Changed = true,
                            CamoraIndex = _cylinderMagazine.CurrentCamoraIndex,
                            HammerClosed = _coopClientFirearmController.Item.CylinderHammerClosed,
                            Reload = true,
                            Status = EReloadWithAmmoStatus.StartReload,
                            AmmoIds = _ammoIds
                        }
                    };
                    _fikaPlayer.PacketSender.SendPacket(ref packet);
                }
            }
        }

        private class ReloadBarrelsHandler(FikaPlayer fikaPlayer, ItemAddress placeToPutContainedAmmoMagazine, AmmoPackReloadingClass ammoPack)
        {
            private readonly FikaPlayer _fikaPlayer = fikaPlayer;
            private readonly ItemAddress _placeToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
            private readonly AmmoPackReloadingClass _ammoPack = ammoPack;

            public void Process(IResult result)
            {
                ItemAddress itemAddress = _placeToPutContainedAmmoMagazine;
                GClass1785 descriptor = itemAddress?.ToDescriptor();
                EFTWriterClass eftWriter = new();
                string[] ammoIds = _ammoPack.GetReloadingAmmoIds();

                byte[] locationDescription;
                if (descriptor != null)
                {
                    eftWriter.WritePolymorph(descriptor);
                    locationDescription = eftWriter.ToArray();
                }
                else
                {
                    locationDescription = [];
                }

                if (_fikaPlayer.HealthController.IsAlive)
                {
                    WeaponPacket packet = new()
                    {
                        NetId = _fikaPlayer.NetId,
                        Type = EFirearmSubPacketType.ReloadBarrels,
                        SubPacket = new ReloadBarrelsPacket()
                        {
                            Reload = true,
                            AmmoIds = ammoIds,
                            LocationDescription = locationDescription
                        }
                    };
                    _fikaPlayer.PacketSender.SendPacket(ref packet);
                }
            }
        }

        private class ReloadWithAmmoHandler(FikaPlayer fikaPlayer, string[] ammoIds)
        {
            private readonly FikaPlayer _fikaPlayer = fikaPlayer;
            private readonly string[] _ammoIds = ammoIds;

            public void Process(IResult result)
            {
                if (_fikaPlayer.HealthController.IsAlive)
                {
                    WeaponPacket packet = new()
                    {
                        NetId = _fikaPlayer.NetId,
                        Type = EFirearmSubPacketType.ReloadWithAmmo,
                        SubPacket = new ReloadWithAmmoPacket()
                        {
                            Reload = true,
                            Status = EReloadWithAmmoStatus.StartReload,
                            AmmoIds = _ammoIds
                        }
                    };
                    _fikaPlayer.PacketSender.SendPacket(ref packet);
                }
            }
        }
    }
}
