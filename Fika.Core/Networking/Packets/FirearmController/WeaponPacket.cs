// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using LiteNetLib.Utils;
using static Fika.Core.Networking.FikaSerialization;

namespace Fika.Core.Networking
{
    public struct WeaponPacket(int netId) : INetSerializable
    {
        public int NetId = netId;
        public bool HasShotInfo = false;
        public ShotInfoPacket ShotInfoPacket;
        public bool ChangeFireMode = false;
        public Weapon.EFireMode FireMode;
        public bool ToggleAim = false;
        public int AimingIndex;
        public bool ExamineWeapon = false;
        public bool CheckAmmo = false;
        public bool CheckChamber = false;
        public bool CheckFireMode = false;
        public bool ToggleTacticalCombo = false;
        public LightStatesPacket LightStatesPacket;
        public bool ChangeSightMode = false;
        public ScopeStatesPacket ScopeStatesPacket;
        public bool ToggleLauncher = false;
        public EGesture Gesture = EGesture.None;
        public bool EnableInventory = false;
        public bool InventoryStatus = false;
        public bool Loot = false;
        public bool HasReloadMagPacket = false;
        public ReloadMagPacket ReloadMagPacket;
        public bool HasQuickReloadMagPacket = false;
        public QuickReloadMagPacket QuickReloadMagPacket;
        public bool HasReloadWithAmmoPacket = false;
        public ReloadWithAmmoPacket ReloadWithAmmo;
        public bool HasCylinderMagPacket = false;
        public CylinderMagPacket CylinderMag;
        public bool HasReloadLauncherPacket = false;
        public ReloadLauncherPacket ReloadLauncher;
        public bool HasReloadBarrelsPacket = false;
        public ReloadBarrelsPacket ReloadBarrels;
        public bool HasGrenadePacket = false;
        public GrenadePacket GrenadePacket;
        public bool CancelGrenade = false;
        public bool HasCompassChange = false;
        public bool CompassState;
        public bool HasKnifePacket = false;
        public KnifePacket KnifePacket;
        public bool HasStanceChange = false;
        public bool LeftStanceState;
        public bool HasFlareShot = false;
        public FlareShotPacket FlareShotPacket;
        public bool ReloadBoltAction = false;
        public bool HasRollCylinder = false;
        public bool RollToZeroCamora = false;
        public bool UnderbarrelSightingRangeUp = false;
        public bool UnderbarrelSightingRangeDown = false;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            HasShotInfo = reader.GetBool();
            if (HasShotInfo)
                ShotInfoPacket = ShotInfoPacket.Deserialize(reader);
            ChangeFireMode = reader.GetBool();
            FireMode = (Weapon.EFireMode)reader.GetInt();
            ToggleAim = reader.GetBool();
            if (ToggleAim)
                AimingIndex = reader.GetInt();
            ExamineWeapon = reader.GetBool();
            CheckAmmo = reader.GetBool();
            CheckChamber = reader.GetBool();
            CheckFireMode = reader.GetBool();
            ToggleTacticalCombo = reader.GetBool();
            if (ToggleTacticalCombo)
                LightStatesPacket = LightStatesPacket.Deserialize(reader);
            ChangeSightMode = reader.GetBool();
            if (ChangeSightMode)
                ScopeStatesPacket = ScopeStatesPacket.Deserialize(reader);
            ToggleLauncher = reader.GetBool();
            Gesture = (EGesture)reader.GetInt();
            EnableInventory = reader.GetBool();
            InventoryStatus = reader.GetBool();
            Loot = reader.GetBool();
            HasReloadMagPacket = reader.GetBool();
            if (HasReloadMagPacket)
            {
                ReloadMagPacket = ReloadMagPacket.Deserialize(reader);
            }
            HasQuickReloadMagPacket = reader.GetBool();
            if (HasQuickReloadMagPacket)
                QuickReloadMagPacket = QuickReloadMagPacket.Deserialize(reader);
            HasReloadWithAmmoPacket = reader.GetBool();
            if (HasReloadWithAmmoPacket)
                ReloadWithAmmo = ReloadWithAmmoPacket.Deserialize(reader);
            HasCylinderMagPacket = reader.GetBool();
            if (HasCylinderMagPacket)
                CylinderMag = CylinderMagPacket.Deserialize(reader);
            HasReloadLauncherPacket = reader.GetBool();
            if (HasReloadLauncherPacket)
                ReloadLauncher = ReloadLauncherPacket.Deserialize(reader);
            HasReloadBarrelsPacket = reader.GetBool();
            if (HasReloadBarrelsPacket)
            {
                ReloadBarrels = ReloadBarrelsPacket.Deserialize(reader);
            }
            HasGrenadePacket = reader.GetBool();
            if (HasGrenadePacket)
                GrenadePacket = GrenadePacket.Deserialize(reader);
            CancelGrenade = reader.GetBool();
            HasCompassChange = reader.GetBool();
            if (HasCompassChange)
                CompassState = reader.GetBool();
            HasKnifePacket = reader.GetBool();
            if (HasKnifePacket)
                KnifePacket = KnifePacket.Deserialize(reader);
            HasStanceChange = reader.GetBool();
            if (HasStanceChange)
                LeftStanceState = reader.GetBool();
            HasFlareShot = reader.GetBool();
            if (HasFlareShot)
                FlareShotPacket = FlareShotPacket.Deserialize(reader);
            ReloadBoltAction = reader.GetBool();
            HasRollCylinder = reader.GetBool();
            if (HasRollCylinder)
                RollToZeroCamora = reader.GetBool();
            UnderbarrelSightingRangeUp = reader.GetBool();
            UnderbarrelSightingRangeDown = reader.GetBool();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(HasShotInfo);
            if (HasShotInfo)
                ShotInfoPacket.Serialize(writer, ShotInfoPacket);
            writer.Put(ChangeFireMode);
            writer.Put((int)FireMode);
            writer.Put(ToggleAim);
            if (ToggleAim)
                writer.Put(AimingIndex);
            writer.Put(ExamineWeapon);
            writer.Put(CheckAmmo);
            writer.Put(CheckChamber);
            writer.Put(CheckFireMode);
            writer.Put(ToggleTacticalCombo);
            if (ToggleTacticalCombo)
                LightStatesPacket.Serialize(writer, LightStatesPacket);
            writer.Put(ChangeSightMode);
            if (ChangeSightMode)
                ScopeStatesPacket.Serialize(writer, ScopeStatesPacket);
            writer.Put(ToggleLauncher);
            writer.Put((int)Gesture);
            writer.Put(EnableInventory);
            writer.Put(InventoryStatus);
            writer.Put(Loot);
            writer.Put(HasReloadMagPacket);
            if (HasReloadMagPacket)
                ReloadMagPacket.Serialize(writer, ReloadMagPacket);
            writer.Put(HasQuickReloadMagPacket);
            if (HasQuickReloadMagPacket)
                QuickReloadMagPacket.Serialize(writer, QuickReloadMagPacket);
            writer.Put(HasReloadWithAmmoPacket);
            if (HasReloadWithAmmoPacket)
                ReloadWithAmmoPacket.Serialize(writer, ReloadWithAmmo);
            writer.Put(HasCylinderMagPacket);
            if (HasCylinderMagPacket)
                CylinderMagPacket.Serialize(writer, CylinderMag);
            writer.Put(HasReloadLauncherPacket);
            if (HasReloadLauncherPacket)
                ReloadLauncherPacket.Serialize(writer, ReloadLauncher);
            writer.Put(HasReloadBarrelsPacket);
            if (HasReloadBarrelsPacket)
                ReloadBarrelsPacket.Serialize(writer, ReloadBarrels);
            writer.Put(HasGrenadePacket);
            if (HasGrenadePacket)
                GrenadePacket.Serialize(writer, GrenadePacket);
            writer.Put(CancelGrenade);
            writer.Put(HasCompassChange);
            if (HasCompassChange)
                writer.Put(CompassState);
            writer.Put(HasKnifePacket);
            if (HasKnifePacket)
                KnifePacket.Serialize(writer, KnifePacket);
            writer.Put(HasStanceChange);
            if (HasStanceChange)
                writer.Put(LeftStanceState);
            writer.Put(HasFlareShot);
            if (HasFlareShot)
                FlareShotPacket.Serialize(writer, FlareShotPacket);
            writer.Put(ReloadBoltAction);
            writer.Put(HasRollCylinder);
            if (HasRollCylinder)
                writer.Put(RollToZeroCamora);
            writer.Put(UnderbarrelSightingRangeUp);
            writer.Put(UnderbarrelSightingRangeDown);
        }
    }
}
