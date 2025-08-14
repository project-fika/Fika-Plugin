// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.FirearmController;
using Fika.Core.Networking.Packets.FirearmController.SubPackets;
using Fika.Core.Networking.Pooling;
using System;
using System.Collections.Generic;

namespace Fika.Core.Main.ClientClasses.HandsControllers;

public class FikaClientFirearmController : Player.FirearmController
{
    protected FikaPlayer _fikaPlayer;
    protected WeaponPacket _packet;
    private bool _isClient;
    private bool _isGrenadeLauncher;

    public static FikaClientFirearmController Create(FikaPlayer player, Weapon weapon)
    {
        FikaClientFirearmController controller = smethod_6<FikaClientFirearmController>(player, weapon);
        controller._fikaPlayer = player;
        controller._isClient = FikaBackendUtils.IsClient;
        controller._isGrenadeLauncher = weapon.IsGrenadeLauncher;
        controller._packet = new()
        {
            NetId = player.NetId
        };
        return controller;
    }

    public void SendLightStates(LightStatesPacket packet)
    {
        _packet.Type = EFirearmSubPacketType.ToggleLightStates;
        _packet.SubPacket = packet;
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public void SendCompassState(CompassChangePacket packet)
    {
        _packet.Type = EFirearmSubPacketType.CompassChange;
        _packet.SubPacket = packet;
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void Destroy()
    {
        _packet = null;
        base.Destroy();
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
            return new AmmoPackReloadInternalOneChamberOperation(this);
        }
        if (Item.MustBoltBeOpennedForInternalReload)
        {
            return new AmmoPackReloadInternalBoltOpenOperation(this);
        }
        return new AmmoPackReloadInternalOneChamberOperation(this);
    }

    public Player.BaseAnimationOperationClass Weapon2()
    {
        return new CylinderReloadOperation(this);
    }

    public Player.BaseAnimationOperationClass Weapon3()
    {
        if (Item is RocketLauncherItemClass)
        {
            return new GClass1869(this);
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
        if (Item is RevolverItemClass)
        {
            return new RevolverFireOperationClass(this);
        }
        if (!Item.BoltAction)
        {
            return new GenericFireOperationClass(this);
        }
        return new DefaultFireOperation(this);
    }

    public override bool ToggleBipod()
    {
        bool success = base.ToggleBipod();
        if (success)
        {
            _packet.Type = EFirearmSubPacketType.ToggleBipod;
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
        return success;
    }

    public override bool CheckChamber()
    {
        bool flag = base.CheckChamber();
        if (flag)
        {
            _packet.Type = EFirearmSubPacketType.CheckChamber;
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override bool CheckAmmo()
    {
        bool flag = base.CheckAmmo();
        if (flag)
        {
            _packet.Type = EFirearmSubPacketType.CheckAmmo;
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override bool ChangeFireMode(Weapon.EFireMode fireMode)
    {
        bool flag = base.ChangeFireMode(fireMode);
        if (flag)
        {
            _packet.Type = EFirearmSubPacketType.ChangeFireMode;
            _packet.SubPacket = ChangeFireModePacket.FromValue(fireMode);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void ChangeAimingMode()
    {
        base.ChangeAimingMode();
        _packet.Type = EFirearmSubPacketType.ToggleAim;
        _packet.SubPacket = FirearmSubPacketPoolManager.Instance.GetPacket<IPoolSubPacket>(EFirearmSubPacketType.ToggleAim);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void SetAim(bool value)
    {
        bool isAiming = IsAiming;
        bool aimingInterruptedByOverlap = AimingInterruptedByOverlap;
        base.SetAim(value);
        if (IsAiming != isAiming || aimingInterruptedByOverlap && _fikaPlayer.HealthController.IsAlive)
        {
            _packet.Type = EFirearmSubPacketType.ToggleAim;
            _packet.SubPacket = ToggleAimPacket.FromValue(IsAiming ? Item.AimIndex.Value : -1);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public override void AimingChanged(bool newValue)
    {
        base.AimingChanged(newValue);
        if (!IsAiming && _fikaPlayer.HealthController.IsAlive)
        {
            _packet.Type = EFirearmSubPacketType.ToggleAim;
            _packet.SubPacket = ToggleAimPacket.FromValue(-1);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
    }

    public override bool CheckFireMode()
    {
        bool flag = base.CheckFireMode();
        if (flag && _fikaPlayer.HealthController.IsAlive)
        {
            _packet.Type = EFirearmSubPacketType.CheckFireMode;
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);

        }
        return flag;
    }

    public override void DryShot(int chamberIndex = 0, bool underbarrelShot = false)
    {
        base.DryShot(chamberIndex, underbarrelShot);
        _packet.Type = EFirearmSubPacketType.ShotInfo;
        _packet.SubPacket = ShotInfoPacket.FromDryShot(chamberIndex, underbarrelShot, EShotType.DryFire);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override bool ExamineWeapon()
    {
        bool flag = base.ExamineWeapon();
        if (flag && _fikaPlayer.HealthController.IsAlive)
        {
            _packet.Type = EFirearmSubPacketType.ExamineWeapon;
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
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

        _packet.Type = EFirearmSubPacketType.ShotInfo;
        _packet.SubPacket = ShotInfoPacket.FromShot(shotPosition, shotDirection, ammo.TemplateId, overheat,
            weapon.MalfState.LastShotOverheat, weapon.MalfState.LastShotTime, Weapon.Repairable.Durability,
            chamberIndex, Weapon.IsUnderBarrelDeviceActive || _isGrenadeLauncher,
            weapon.MalfState.SlideOnOverheatReached, shotType);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        _fikaPlayer.StatisticsManager.OnShot(Weapon, ammo);

        base.InitiateShot(weapon, ammo, shotPosition, shotDirection, fireportPosition, chamberIndex, overheat);
    }

    public override void QuickReloadMag(MagazineItemClass magazine, Callback callback)
    {
        if (CanStartReload())
        {
            base.QuickReloadMag(magazine, callback);
            _packet.Type = EFirearmSubPacketType.QuickReloadMag;
            _packet.SubPacket = QuickReloadMagPacket.FromValue(magazine.Id, true);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
            return;
        }

        callback?.Fail("Can't start QuickReloadMag");
    }

    public override void ReloadBarrels(AmmoPackReloadingClass ammoPack, ItemAddress placeToPutContainedAmmoMagazine, Callback callback)
    {
        if (CanStartReload() && ammoPack.AmmoCount > 0)
        {
            ReloadBarrelsHandler handler = new(_fikaPlayer, this, placeToPutContainedAmmoMagazine, ammoPack);
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
            _packet.Type = EFirearmSubPacketType.ReloadLauncher;
            _packet.SubPacket = ReloadLauncherPacket.FromValue(true, reloadingAmmoIds);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);

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
            ReloadMagHandler handler = new(_fikaPlayer, this, itemAddress, magazine);
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
            ReloadWithAmmoHandler handler = new(_fikaPlayer, this, ammoPack.GetReloadingAmmoIds());
            CurrentOperation.ReloadWithAmmo(ammoPack, callback, handler.Process);
            return;
        }

        callback?.Fail("Can't start ReloadWithAmmo");
    }

    public override bool SetLightsState(FirearmLightStateStruct[] lightsStates, bool force = false, bool animated = true)
    {
        if (force || CurrentOperation.CanChangeLightState(lightsStates))
        {
            _packet.Type = EFirearmSubPacketType.ToggleLightStates;
            _packet.SubPacket = LightStatesPacket.FromValue(lightsStates.Length, lightsStates);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
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

        _packet.Type = EFirearmSubPacketType.ToggleScopeStates;
        _packet.SubPacket = ScopeStatesPacket.FromValue(scopeStates.Length, scopeStates);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
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

        _packet.Type = EFirearmSubPacketType.ShotInfo;
        _packet.SubPacket = ShotInfoPacket.FromMisfire(ammo.TemplateId, overheat, shotType);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);

        base.ShotMisfired(ammo, malfunctionState, overheat);
    }

    public override bool ToggleLauncher(Action callback = null)
    {
        bool flag = base.ToggleLauncher(callback);
        if (flag)
        {
            _packet.Type = EFirearmSubPacketType.ToggleLauncher;
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
        return flag;
    }

    public override void Loot(bool p)
    {
        base.Loot(p);
        _packet.Type = EFirearmSubPacketType.Loot;
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void SetInventoryOpened(bool opened)
    {
        base.SetInventoryOpened(opened);
        _packet.Type = EFirearmSubPacketType.ToggleInventory;
        _packet.SubPacket = ToggleInventoryPacket.FromValue(opened);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void ChangeLeftStance()
    {
        base.ChangeLeftStance();
        _packet.Type = EFirearmSubPacketType.LeftStanceChange;
        _packet.SubPacket = LeftStanceChangePacket.FromValue(_fikaPlayer.MovementContext.LeftStanceEnabled);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void SendStartOneShotFire()
    {
        _packet.Type = EFirearmSubPacketType.FlareShot;
        _packet.SubPacket = FlareShotPacket.FromValue(default, default, default, true);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void CreateFlareShot(AmmoItemClass flareItem, Vector3 shotPosition, Vector3 forward)
    {
        _packet.Type = EFirearmSubPacketType.FlareShot;
        _packet.SubPacket = FlareShotPacket.FromValue(shotPosition, forward, flareItem.TemplateId, false);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.CreateFlareShot(flareItem, shotPosition, forward);
    }

    public override void CreateRocketShot(AmmoItemClass rocketItem, Vector3 shotPosition, Vector3 forward, Transform smokeport = null)
    {
        _packet.Type = EFirearmSubPacketType.RocketShot;
        _packet.SubPacket = RocketShotPacket.FromValue(shotPosition, forward, rocketItem.TemplateId);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        base.CreateRocketShot(rocketItem, shotPosition, forward, smokeport);
    }

    private void SendAbortReloadPacket(int amount)
    {
        _packet.Type = EFirearmSubPacketType.ReloadWithAmmo;
        _packet.SubPacket = ReloadWithAmmoPacket.FromValue(true, EReloadWithAmmoStatus.AbortReload, amount);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    public override void RollCylinder(bool rollToZeroCamora)
    {
        if (Blindfire || IsAiming)
        {
            return;
        }

        _packet.Type = EFirearmSubPacketType.RollCylinder;
        _packet.SubPacket = RollCylinderPacket.FromValue(rollToZeroCamora);
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);

        CurrentOperation.RollCylinder(null, rollToZeroCamora);
    }

    private void SendEndReloadPacket(int amount)
    {
        if (_fikaPlayer.HealthController.IsAlive)
        {
            _packet.Type = EFirearmSubPacketType.ReloadWithAmmo;
            _packet.SubPacket = ReloadWithAmmoPacket.FromValue(true, EReloadWithAmmoStatus.EndReload, amount);
            _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
        }
    }

    private void SendBoltActionReloadPacket()
    {
        _packet.Type = EFirearmSubPacketType.ReloadBoltAction;
        _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _packet, DeliveryMethod.ReliableOrdered, true);
    }

    private class CylinderReloadOperation(Player.FirearmController controller) : CylinderReloadOperationClass(controller)
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

    private class AmmoPackReloadInternalOneChamberOperation(Player.FirearmController controller) : AmmoPackReloadInternalOneChamberOperationClass(controller)
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

    private class AmmoPackReloadInternalBoltOpenOperation(Player.FirearmController controller) : AmmoPackReloadInternalBoltOpenOperationClass(controller)
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

    private class DefaultFireOperation(Player.FirearmController controller) : DefaultWeaponOperationClass(controller)
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

    private class ReloadMagHandler(FikaPlayer fikaPlayer, FikaClientFirearmController coopClientFirearmController, ItemAddress gridItemAddress, MagazineItemClass magazine)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly FikaClientFirearmController _coopClientFirearmController = coopClientFirearmController;
        private readonly ItemAddress _gridItemAddress = gridItemAddress;
        private readonly MagazineItemClass _magazine = magazine;

        public void Process(IResult result)
        {
            ItemAddress itemAddress = _gridItemAddress;
            GClass1785 descriptor = itemAddress?.ToDescriptor();
            EFTWriterClass eftWriter = WriterPoolManager.GetWriter();

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

            WriterPoolManager.ReturnWriter(eftWriter);

            if (_fikaPlayer.HealthController.IsAlive)
            {
                _coopClientFirearmController._packet.Type = EFirearmSubPacketType.ReloadMag;
                _coopClientFirearmController._packet.SubPacket = ReloadMagPacket.FromValue(_magazine.Id, locationDescription, true);
                _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _coopClientFirearmController._packet, DeliveryMethod.ReliableOrdered, true);
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
                _coopClientFirearmController._packet.Type = EFirearmSubPacketType.CylinderMag;
                _coopClientFirearmController._packet.SubPacket = CylinderMagPacket.FromValue(EReloadWithAmmoStatus.StartReload,
                    _cylinderMagazine.CurrentCamoraIndex, 0, true,
                    _coopClientFirearmController.Item.CylinderHammerClosed, true, _ammoIds);
                _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _coopClientFirearmController._packet, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }

    private class ReloadBarrelsHandler(FikaPlayer fikaPlayer, FikaClientFirearmController coopClientFirearmController, ItemAddress placeToPutContainedAmmoMagazine, AmmoPackReloadingClass ammoPack)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly FikaClientFirearmController _coopClientFirearmController = coopClientFirearmController;
        private readonly ItemAddress _placeToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
        private readonly AmmoPackReloadingClass _ammoPack = ammoPack;

        public void Process(IResult result)
        {
            ItemAddress itemAddress = _placeToPutContainedAmmoMagazine;
            GClass1785 descriptor = itemAddress?.ToDescriptor();
            EFTWriterClass eftWriter = WriterPoolManager.GetWriter();
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

            WriterPoolManager.ReturnWriter(eftWriter);

            if (_fikaPlayer.HealthController.IsAlive)
            {
                _coopClientFirearmController._packet.Type = EFirearmSubPacketType.ReloadBarrels;
                _coopClientFirearmController._packet.SubPacket = ReloadBarrelsPacket.FromValue(true, ammoIds, locationDescription);
                _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _coopClientFirearmController._packet, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }

    private class ReloadWithAmmoHandler(FikaPlayer fikaPlayer, FikaClientFirearmController coopClientFirearmController, string[] ammoIds)
    {
        private readonly FikaPlayer _fikaPlayer = fikaPlayer;
        private readonly FikaClientFirearmController _coopClientFirearmController = coopClientFirearmController;
        private readonly string[] _ammoIds = ammoIds;

        public void Process(IResult result)
        {
            if (_fikaPlayer.HealthController.IsAlive)
            {
                _coopClientFirearmController._packet.Type = EFirearmSubPacketType.ReloadWithAmmo;
                _coopClientFirearmController._packet.SubPacket = ReloadWithAmmoPacket.FromValue(true, EReloadWithAmmoStatus.StartReload, ammoIds: _ammoIds);
                _fikaPlayer.PacketSender.NetworkManager.SendNetReusable(ref _coopClientFirearmController._packet, DeliveryMethod.ReliableOrdered, true);
            }
        }
    }
}
