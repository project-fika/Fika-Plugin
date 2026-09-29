// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;
using ReloadMagPacket = Fika.Core.Networking.Packets.FirearmController.SubPackets.ReloadMagPacket;
using RollCylinderPacket = Fika.Core.Networking.Packets.FirearmController.SubPackets.RollCylinderPacket;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientFirearmController : Player.FirearmController
{
    protected FikaPlayer _fikaPlayer;
    private bool _isClient;
    private bool _isGrenadeLauncher;

    public static FikaClientFirearmController Create(FikaPlayer player, Weapon weapon)
    {
        var controller = CreateController<FikaClientFirearmController>(player, weapon);
        controller._fikaPlayer = player;
        controller._isClient = FikaBackendUtils.IsClient;
        controller._isGrenadeLauncher = weapon.IsGrenadeLauncher;
        return controller;
    }

    public void SendLightStates(in LightStatesPacket packet)
    {
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void CompassStateHandler(bool isActive)
    {
        //SendCompassState(CompassChangePacket.FromValue(isActive));
        base.CompassStateHandler(isActive);
    }

    public void SendCompassState(in CompassChangePacket packet)
    {
#if DEBUG
        FikaGlobals.LogInfo("Sending CompassPacket");
#endif
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
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
        var operationFactoryDelegates = base.GetOperationFactoryDelegates();
        operationFactoryDelegates[typeof(ReloadInternalMagBase)] = new OperationFactoryDelegate(Weapon1);
        operationFactoryDelegates[typeof(ReloadCylinderMagOperation)] = new OperationFactoryDelegate(Weapon2);
        operationFactoryDelegates[typeof(FireOperation)] = new OperationFactoryDelegate(Weapon3);
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

    public Player.ObjectInHandsOperation Weapon1()
    {
        if (Item.ReloadMode is Weapon.EReloadMode.InternalMagazine && Item.Chambers.Length == 0)
        {
            return new AmmoPackReloadInternalOneChamberOperation(this);
        }
        if (Item.MustBoltBeOpennedForInternalReload)
        {
            return new AmmoPackReloadInternalBoltOpenOperation(this);
        }
        return new AmmoPackReloadInternalOneChamberOperation(this);
    }

    public Player.ObjectInHandsOperation Weapon2()
    {
        return new CylinderReloadOperation(this);
    }

    public Player.ObjectInHandsOperation Weapon3()
    {
        if (Item is RocketLauncher)
        {
            return new RocketLauncherFire(this);
        }
        if (Item.IsFlareGun)
        {
            return new FlareGunFire(this);
        }
        if (Item.IsOneOff)
        {
            return new OneOffGunFire(this);
        }
        if (Item.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
        {
            return new FireOnlyBarrelFireOperation(this);
        }
        if (Item is Revolver)
        {
            return new FireCylinderMagOperation(this);
        }
        if (!Item.BoltAction)
        {
            return new FireOperation(this);
        }
        return new DefaultFireOperation(this);
    }

    public override bool ToggleBipod()
    {
        var success = base.ToggleBipod();
        if (success)
        {
            var packet = new ToggleBipodPacket();
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
        return success;
    }

    public override bool CheckChamber()
    {
        var flag = base.CheckChamber();
        if (flag)
        {
            var packet = new CheckChamberPacket();
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override bool CheckAmmo()
    {
        var flag = base.CheckAmmo();
        if (flag)
        {
            var packet = new CheckAmmoPacket();
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override bool ChangeFireMode(Weapon.EFireMode fireMode)
    {
        var flag = base.ChangeFireMode(fireMode);
        if (flag)
        {
            var packet = new ChangeFireModePacket(fireMode);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void ChangeAimingMode()
    {
        base.ChangeAimingMode();
        var packet = new ToggleAimPacket();
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void SetAim(bool value)
    {
        var isAiming = IsAiming;
        var aimingInterruptedByOverlap = AimingInterruptedByOverlap;
        base.SetAim(value);
        if (IsAiming != isAiming || (aimingInterruptedByOverlap && _fikaPlayer.HealthController.IsAlive))
        {
            var packet = new ToggleAimPacket(IsAiming ? Item.AimIndex.Value : -1);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public override void AimingChanged(bool newValue)
    {
        base.AimingChanged(newValue);
        if (!IsAiming && _fikaPlayer.HealthController.IsAlive)
        {
            var packet = new ToggleAimPacket(-1);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public override bool CheckFireMode()
    {
        var flag = base.CheckFireMode();
        if (flag && _fikaPlayer.HealthController.IsAlive)
        {
            var packet = new CheckFireModePacket();
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);

        }
        return flag;
    }

    public override void DryShot(int chamberIndex = 0, bool underbarrelShot = false)
    {
        base.DryShot(chamberIndex, underbarrelShot);
        var packet = new DryShotPacket(chamberIndex, underbarrelShot);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override bool ExamineWeapon()
    {
        var flag = base.ExamineWeapon();
        if (flag && _fikaPlayer.HealthController.IsAlive)
        {
            var packet = new ExamineWeaponPacket();
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void InitiateShot(IWeapon weapon, Ammo ammo, Vector3 shotPosition, Vector3 shotDirection, Vector3 fireportPosition, int chamberIndex, float overheat)
    {
        var malfState = weapon.MalfState;
        var shotType = malfState.State switch
        {
            Weapon.EMalfunctionState.None => EShotType.RegularShot,
            Weapon.EMalfunctionState.Misfire => EShotType.Misfire,
            Weapon.EMalfunctionState.Jam => EShotType.JamedShot,
            Weapon.EMalfunctionState.HardSlide => EShotType.HardSlidedShot,
            Weapon.EMalfunctionState.SoftSlide => EShotType.SoftSlidedShot,
            Weapon.EMalfunctionState.Feed => EShotType.Feed,
            _ => EShotType.RegularShot
        };

        var player = _fikaPlayer;
        var netManager = player.PacketSender.NetworkManager;
        var netId = player.NetId;
        var ammoTemplateId = ammo.TemplateId;

        if (shotType.IsMisfire())
        {
            var packet = new MisfirePacket(ammoTemplateId, overheat, shotType);
            netManager.SendFirearmPacket(in packet, netId, DeliveryMethod.ReliableOrdered, true);
        }
        else
        {
            var isLauncherActive = Weapon.IsUnderBarrelDeviceActive || _isGrenadeLauncher;
            var packet = new ShotInfoPacket(shotPosition, shotDirection, ammoTemplateId, overheat,
                malfState.LastShotOverheat, malfState.LastShotTime, Weapon.Repairable.Durability, chamberIndex,
                isLauncherActive, malfState.SlideOnOverheatReached);
            netManager.SendFirearmPacket(in packet, netId, DeliveryMethod.ReliableOrdered, true);
        }

        player.StatisticsManager.OnShot(Weapon, ammo);

        base.InitiateShot(weapon, ammo, shotPosition, shotDirection, fireportPosition, chamberIndex, overheat);
    }

    public override void QuickReloadMag(Magazine magazine, Callback callback)
    {
        if (CanStartReload())
        {
            base.QuickReloadMag(magazine, callback);
            var packet = new QuickReloadMagPacket(magazine.Id, true);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
            return;
        }

        callback?.Fail("Can't start QuickReloadMag");
    }

    public override void ReloadBarrels(AmmoPack ammoPack, ItemAddress placeToPutContainedAmmoMagazine, Callback callback)
    {
        if (CanStartReload() && ammoPack.AmmoCount > 0)
        {
            ReloadBarrelsHandler handler = new(_fikaPlayer, placeToPutContainedAmmoMagazine, ammoPack);
            CurrentOperation.ReloadBarrels(ammoPack, placeToPutContainedAmmoMagazine, callback, handler.Process);
            return;
        }

        callback?.Fail("Can't start ReloadBarrels");
    }

    public override void ReloadCylinderMagazine(AmmoPack ammoPack, Callback callback, bool quickReload = false)
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
            [], (CylinderMagazine)Item.GetCurrentMagazine());
            Weapon.GetShellsIndexes(handler.ShellsIndexes);
            CurrentOperation.ReloadCylinderMagazine(ammoPack, callback, handler.Process, handler.QuickReload);
            return;
        }

        callback?.Fail("Can't start ReloadCylinderMagazine");
    }

    public override void ReloadGrenadeLauncher(AmmoPack ammoPack, Callback callback)
    {
        if (CanStartReload())
        {
            var reloadingAmmoIds = ammoPack.GetReloadingAmmoIds();
            var packet = new ReloadLauncherPacket(true, reloadingAmmoIds);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);

            CurrentOperation.ReloadGrenadeLauncher(ammoPack, callback);
            return;
        }

        callback?.Fail("Can't start ReloadGrenadeLauncher");
    }

    public override void ReloadMag(Magazine magazine, ItemAddress itemAddress, Callback callback)
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

    public override void ReloadWithAmmo(AmmoPack ammoPack, Callback callback)
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

    public override bool SetLightsState(LightsState[] lightsStates, bool force = false, bool animated = true)
    {
        if (force || CurrentOperation.CanChangeLightState(lightsStates))
        {
            var packet = new LightStatesPacket(lightsStates.Length, lightsStates);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }

        return base.SetLightsState(lightsStates, force, animated);
    }

    public override void SetScopeMode(ScopeState[] scopeStates)
    {
        SendScopeStates(scopeStates);
        base.SetScopeMode(scopeStates);
    }
    public override void OpticCalibrationSwitchUp(ScopeState[] scopeStates)
    {
        SendScopeStates(scopeStates);
        base.OpticCalibrationSwitchUp(scopeStates);
    }

    public override void OpticCalibrationSwitchDown(ScopeState[] scopeStates)
    {
        SendScopeStates(scopeStates);
        base.OpticCalibrationSwitchDown(scopeStates);
    }

    private void SendScopeStates(ScopeState[] scopeStates)
    {
        if (!CurrentOperation.CanChangeScopeStates(scopeStates))
        {
            return;
        }

        var packet = new ScopeStatesPacket(scopeStates.Length, scopeStates);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void ShotMisfired(Ammo ammo, Weapon.EMalfunctionState malfunctionState, float overheat)
    {
        var shotType = malfunctionState switch
        {
            Weapon.EMalfunctionState.None => EShotType.RegularShot,
            Weapon.EMalfunctionState.Misfire => EShotType.Misfire,
            Weapon.EMalfunctionState.Jam => EShotType.JamedShot,
            Weapon.EMalfunctionState.HardSlide => EShotType.HardSlidedShot,
            Weapon.EMalfunctionState.SoftSlide => EShotType.SoftSlidedShot,
            Weapon.EMalfunctionState.Feed => EShotType.Feed,
            _ => EShotType.RegularShot
        };

        var packet = new MisfirePacket(ammo.TemplateId, overheat, shotType);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);

        base.ShotMisfired(ammo, malfunctionState, overheat);
    }

    public override bool ToggleLauncher(Action callback = null)
    {
        var flag = base.ToggleLauncher(callback);
        if (flag)
        {
            var packet = new ToggleLauncherPacket();
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void Loot(bool p)
    {
        base.Loot(p);
        var packet = new FirearmLootPacket();
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void SetInventoryOpened(bool opened)
    {
        base.SetInventoryOpened(opened);
        var packet = new ToggleInventoryPacket(opened);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void ChangeLeftStance()
    {
        base.ChangeLeftStance();
        var packet = new LeftStanceChangePacket(_fikaPlayer.MovementContext.LeftStanceEnabled);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void SendStartOneShotFire()
    {
        var packet = new FlareShotPacket(default, default, default, true);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void CreateFlareShot(Ammo flareItem, Vector3 shotPosition, Vector3 forward)
    {
        var packet = new FlareShotPacket(shotPosition, forward, flareItem.TemplateId, false);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.CreateFlareShot(flareItem, shotPosition, forward);
    }

    public override void CreateRocketShot(Ammo rocketItem, Vector3 shotPosition, Vector3 forward, Transform smokeport = null)
    {
        var packet = new RocketShotPacket(shotPosition, forward, rocketItem.TemplateId);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        base.CreateRocketShot(rocketItem, shotPosition, forward, smokeport);
    }

    private void SendAbortReloadPacket(int amount)
    {
        var packet = new ReloadWithAmmoPacket(EReloadWithAmmoStatus.AbortReload, amount);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    public override void RollCylinder(bool rollToZeroCamora)
    {
        if (Blindfire || IsAiming)
        {
            return;
        }

        var packet = new RollCylinderPacket(rollToZeroCamora);
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);

        CurrentOperation.RollCylinder(null, rollToZeroCamora);
    }

    private void SendEndReloadPacket(int amount)
    {
        if (_fikaPlayer.HealthController.IsAlive)
        {
            var packet = new ReloadWithAmmoPacket(EReloadWithAmmoStatus.EndReload, amount);
            _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
        }
    }

    private void SendBoltActionReloadPacket()
    {
        var packet = new ReloadBoltActionPacket();
        _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
    }

    private class CylinderReloadOperation(Player.FirearmController controller) : Player.FirearmController.ReloadCylinderMagOperation(controller)
    {
        public override void SetTriggerPressed(bool pressed)
        {
            var bool_ = ReloadAborted;
            base.SetTriggerPressed(pressed);
            if (ReloadAborted && !bool_)
            {
                coopClientFirearmController.SendAbortReloadPacket(AmmoToLoadIntoMag);
            }
        }

        public override void SwitchToIdle()
        {
            coopClientFirearmController.SendEndReloadPacket(AmmoToLoadIntoMag);
            EndReload();
            base.SwitchToIdle();
        }

        private readonly FikaClientFirearmController coopClientFirearmController = (FikaClientFirearmController)controller;
    }

    private class AmmoPackReloadInternalOneChamberOperation(Player.FirearmController controller) : Player.FirearmController.ReloadInternalMagOperation(controller)
    {
        public override void SetTriggerPressed(bool pressed)
        {
            var bool_ = ReloadAborted;
            base.SetTriggerPressed(pressed);
            if (ReloadAborted && !bool_)
            {
                _coopClientFirearmController.SendAbortReloadPacket(AmmoToLoadIntoMag);
            }
        }

        public override void SwitchToIdle()
        {
            _coopClientFirearmController.SendEndReloadPacket(AmmoToLoadIntoMag);
            base.SwitchToIdle();
        }

        private readonly FikaClientFirearmController _coopClientFirearmController = (FikaClientFirearmController)controller;
    }

    private class AmmoPackReloadInternalBoltOpenOperation(Player.FirearmController controller) : Player.FirearmController.ReloadInternalMagWithOpenBoltOperation(controller)
    {
        public override void SetTriggerPressed(bool pressed)
        {
            var bool_ = ReloadAborted;
            base.SetTriggerPressed(pressed);
            if (ReloadAborted && !bool_)
            {
                _coopClientFirearmController.SendAbortReloadPacket(AmmoToLoadIntoMag);
            }
        }

        public override void SwitchToIdle()
        {
            _coopClientFirearmController.SendEndReloadPacket(AmmoToLoadIntoMag);
            base.SwitchToIdle();
        }

        private readonly FikaClientFirearmController _coopClientFirearmController = (FikaClientFirearmController)controller;
    }

    private class DefaultFireOperation(Player.FirearmController controller) : Player.FirearmController.BoltActionFireOperation(controller)
    {
        public override void Start()
        {
            base.Start();
            SendBoltActionReloadPacket(!Controller.IsTriggerPressed);
        }

        public override void SetTriggerPressed(bool pressed)
        {
            base.SetTriggerPressed(pressed);
            SendBoltActionReloadPacket(!Controller.IsTriggerPressed);
        }

        public override void SetInventoryOpened(bool opened)
        {
            base.SetInventoryOpened(opened);
            SendBoltActionReloadPacket(true);
        }

        public override void ReloadMag(Magazine magazine, ItemAddress gridItemAddress, Callback finishCallback, Callback startCallback)
        {
            base.ReloadMag(magazine, gridItemAddress, finishCallback, startCallback);
            SendBoltActionReloadPacket(true);
        }

        public override void QuickReloadMag(Magazine magazine, Callback finishCallback, Callback startCallback)
        {
            base.QuickReloadMag(magazine, finishCallback, startCallback);
            SendBoltActionReloadPacket(true);
        }

        public override void ReloadWithAmmo(AmmoPack ammoPack, Callback finishCallback, Callback startCallback)
        {
            base.ReloadWithAmmo(ammoPack, finishCallback, startCallback);
            SendBoltActionReloadPacket(true);
        }

        public override void ReloadCylinderMagazine(AmmoPack ammoPack, Callback finishCallback, Callback startCallback, bool quickReload = false)
        {
            base.ReloadCylinderMagazine(ammoPack, finishCallback, startCallback, quickReload);
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

        private readonly FikaClientFirearmController _coopClientFirearmController = (FikaClientFirearmController)controller;
        private bool _hasSent;
    }

    private sealed class ReloadMagHandler(FikaPlayer fikaPlayer, ItemAddress gridItemAddress, Magazine magazine)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly ItemAddress _gridItemAddress = gridItemAddress;
        private readonly Magazine _magazine = magazine;

        public void Process(IResult _)
        {
            if (_fikaPlayer.HealthController.IsAlive)
            {
                var packet = new ReloadMagPacket(_magazine.Id, _gridItemAddress);
                _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }

    private sealed class ReloadCylinderMagazineHandler(FikaPlayer fikaPlayer, FikaClientFirearmController coopClientFirearmController, bool quickReload, string[] ammoIds, List<int> shellsIndexes, CylinderMagazine cylinderMagazine)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly FikaClientFirearmController _coopClientFirearmController = coopClientFirearmController;
        public readonly bool QuickReload = quickReload;
        private readonly string[] _ammoIds = ammoIds;
        public readonly List<int> ShellsIndexes = shellsIndexes;
        private readonly CylinderMagazine _cylinderMagazine = cylinderMagazine;

        public void Process(IResult _)
        {
            if (_fikaPlayer.HealthController.IsAlive)
            {
                var packet = new CylinderMagPacket(EReloadWithAmmoStatus.StartReload,
                    _cylinderMagazine.CurrentCamoraIndex, 0, true,
                    _coopClientFirearmController.Item.CylinderHammerClosed, _ammoIds);
                _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }

    private sealed class ReloadBarrelsHandler(FikaPlayer fikaPlayer, ItemAddress placeToPutContainedAmmoMagazine, AmmoPack ammoPack)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly ItemAddress _placeToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
        private readonly AmmoPack _ammoPack = ammoPack;

        public void Process(IResult _)
        {
            if (_fikaPlayer.HealthController.IsAlive)
            {
                var packet = new ReloadBarrelsPacket(_ammoPack.GetReloadingAmmoIds(), _placeToPutContainedAmmoMagazine);
                _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }

    private sealed class ReloadWithAmmoHandler(FikaPlayer fikaPlayer, string[] ammoIds)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly string[] _ammoIds = ammoIds;

        public void Process(IResult _)
        {
            if (_fikaPlayer.HealthController.IsAlive)
            {
                var packet = new ReloadWithAmmoPacket(EReloadWithAmmoStatus.StartReload, ammoIds: _ammoIds);
                _fikaPlayer.PacketSender.NetworkManager.SendFirearmPacket(in packet, _fikaPlayer.NetId, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }
}
