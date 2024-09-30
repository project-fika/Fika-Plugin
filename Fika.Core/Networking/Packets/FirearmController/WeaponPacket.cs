// © 2024 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using LiteNetLib.Utils;
using static Fika.Core.Networking.Packets.SubPackets;

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
		public bool ToggleLightStates = false;
		public LightStatesPacket LightStatesPacket;
		public bool ToggleScopeStates = false;
		public ScopeStatesPacket ScopeStatesPacket;
		public bool ToggleLauncher = false;
		public bool EnableInventory = false;
		public bool InventoryStatus = false;
		public bool Loot = false;
		public ReloadMagPacket ReloadMagPacket;
		public QuickReloadMagPacket QuickReloadMagPacket;
		public ReloadWithAmmoPacket ReloadWithAmmoPacket;
		public CylinderMagPacket CylinderMagPacket;
		public ReloadLauncherPacket ReloadLauncherPacket;
		public ReloadBarrelsPacket ReloadBarrelsPacket;
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
		public bool ToggleBipod = false;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			HasShotInfo = reader.GetBool();
			if (HasShotInfo)
			{
				ShotInfoPacket = reader.GetShotInfoPacket();
			}
			ChangeFireMode = reader.GetBool();
			FireMode = (Weapon.EFireMode)reader.GetInt();
			ToggleAim = reader.GetBool();
			if (ToggleAim)
			{
				AimingIndex = reader.GetInt();
			}
			ExamineWeapon = reader.GetBool();
			CheckAmmo = reader.GetBool();
			CheckChamber = reader.GetBool();
			CheckFireMode = reader.GetBool();
			ToggleLightStates = reader.GetBool();
			if (ToggleLightStates)
			{
				LightStatesPacket = reader.GetLightStatesPacket();
			}

			ToggleScopeStates = reader.GetBool();
			if (ToggleScopeStates)
			{
				ScopeStatesPacket = reader.GetScopeStatesPacket();
			}
			ToggleLauncher = reader.GetBool();
			EnableInventory = reader.GetBool();
			InventoryStatus = reader.GetBool();
			Loot = reader.GetBool();
			ReloadMagPacket = reader.GetReloadMagPacket();
			QuickReloadMagPacket = reader.GetQuickReloadMagPacket();
			ReloadWithAmmoPacket = reader.GetReloadWithAmmoPacket();
			CylinderMagPacket = reader.GetCylinderMagPacket();
			ReloadLauncherPacket = reader.GetReloadLauncherPacket();
			ReloadBarrelsPacket = reader.GetReloadBarrelsPacket();
			HasGrenadePacket = reader.GetBool();
			if (HasGrenadePacket)
			{
				GrenadePacket = reader.GetGrenadePacket();
			}
			CancelGrenade = reader.GetBool();
			HasCompassChange = reader.GetBool();
			if (HasCompassChange)
			{
				CompassState = reader.GetBool();
			}
			HasKnifePacket = reader.GetBool();
			if (HasKnifePacket)
			{
				KnifePacket = reader.GetKnifePacket();
			}
			HasStanceChange = reader.GetBool();
			if (HasStanceChange)
			{
				LeftStanceState = reader.GetBool();
			}
			HasFlareShot = reader.GetBool();
			if (HasFlareShot)
			{
				FlareShotPacket = reader.GetFlareShotPacket();
			}
			ReloadBoltAction = reader.GetBool();
			HasRollCylinder = reader.GetBool();
			if (HasRollCylinder)
			{
				RollToZeroCamora = reader.GetBool();
			}
			UnderbarrelSightingRangeUp = reader.GetBool();
			UnderbarrelSightingRangeDown = reader.GetBool();
			ToggleBipod = reader.GetBool();
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(HasShotInfo);
			if (HasShotInfo)
			{
				writer.PutShotInfoPacket(ShotInfoPacket);
			}
			writer.Put(ChangeFireMode);
			writer.Put((int)FireMode);
			writer.Put(ToggleAim);
			if (ToggleAim)
			{
				writer.Put(AimingIndex);
			}
			writer.Put(ExamineWeapon);
			writer.Put(CheckAmmo);
			writer.Put(CheckChamber);
			writer.Put(CheckFireMode);
			writer.Put(ToggleLightStates);
			if (ToggleLightStates)
			{
				writer.PutLightStatesPacket(LightStatesPacket);
			}
			writer.Put(ToggleScopeStates);
			if (ToggleScopeStates)
			{
				writer.PutScopeStatesPacket(ScopeStatesPacket);
			}
			writer.Put(ToggleLauncher);
			writer.Put(EnableInventory);
			writer.Put(InventoryStatus);
			writer.Put(Loot);
			writer.PutReloadMagPacket(ReloadMagPacket);
			writer.PutQuickReloadMagPacket(QuickReloadMagPacket);
			writer.PutReloadWithAmmoPacket(ReloadWithAmmoPacket);
			writer.PutCylinderMagPacket(CylinderMagPacket);
			writer.PutReloadLauncherPacket(ReloadLauncherPacket);
			writer.PutReloadBarrelsPacket(ReloadBarrelsPacket);
			writer.Put(HasGrenadePacket);
			if (HasGrenadePacket)
			{
				writer.PutGrenadePacket(GrenadePacket);
			}
			writer.Put(CancelGrenade);
			writer.Put(HasCompassChange);
			if (HasCompassChange)
			{
				writer.Put(CompassState);
			}
			writer.Put(HasKnifePacket);
			if (HasKnifePacket)
			{
				writer.PutKnifePacket(KnifePacket);
			}
			writer.Put(HasStanceChange);
			if (HasStanceChange)
			{
				writer.Put(LeftStanceState);
			}
			writer.Put(HasFlareShot);
			if (HasFlareShot)
			{
				writer.PutFlareShotPacket(FlareShotPacket);
			}
			writer.Put(ReloadBoltAction);
			writer.Put(HasRollCylinder);
			if (HasRollCylinder)
			{
				writer.Put(RollToZeroCamora);
			}
			writer.Put(UnderbarrelSightingRangeUp);
			writer.Put(UnderbarrelSightingRangeDown);
			writer.Put(ToggleBipod);
		}
	}
}
