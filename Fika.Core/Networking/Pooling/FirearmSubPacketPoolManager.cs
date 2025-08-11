using System;
using System.Threading;
using static Fika.Core.Networking.Packets.FirearmController.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Pooling
{
    internal sealed class FirearmSubPacketPoolManager : BasePacketPoolManager<EFirearmSubPacketType, IPoolSubPacket>
    {
        private static Lazy<FirearmSubPacketPoolManager> _instance = new(() => new FirearmSubPacketPoolManager(), LazyThreadSafetyMode.None);
        public static FirearmSubPacketPoolManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public static void Release()
        {
            _instance.Value.ClearPool();
            _instance = null;
        }

        private FirearmSubPacketPoolManager()
        {
            _subPacketFactories = new()
            {
                { EFirearmSubPacketType.ShotInfo, ShotInfoPacket.CreateInstance },
                { EFirearmSubPacketType.ChangeFireMode, ChangeFireModePacket.CreateInstance },
                { EFirearmSubPacketType.ToggleAim, ToggleAimPacket.CreateInstance },
                { EFirearmSubPacketType.ExamineWeapon, ExamineWeaponPacket.CreateInstance },
                { EFirearmSubPacketType.CheckAmmo, CheckAmmoPacket.CreateInstance },
                { EFirearmSubPacketType.CheckChamber, CheckChamberPacket.CreateInstance },
                { EFirearmSubPacketType.CheckFireMode, CheckFireModePacket.CreateInstance },
                { EFirearmSubPacketType.ToggleLightStates, LightStatesPacket.CreateInstance },
                { EFirearmSubPacketType.ToggleScopeStates, ScopeStatesPacket.CreateInstance },
                { EFirearmSubPacketType.ToggleLauncher, ToggleLauncherPacket.CreateInstance },
                { EFirearmSubPacketType.ToggleInventory, ToggleInventoryPacket.CreateInstance },
                { EFirearmSubPacketType.Loot, FirearmLootPacket.CreateInstance },
                { EFirearmSubPacketType.ReloadMag, ReloadMagPacket.CreateInstance },
                { EFirearmSubPacketType.QuickReloadMag, QuickReloadMagPacket.CreateInstance },
                { EFirearmSubPacketType.ReloadWithAmmo, ReloadWithAmmoPacket.CreateInstance },
                { EFirearmSubPacketType.CylinderMag, CylinderMagPacket.CreateInstance },
                { EFirearmSubPacketType.ReloadLauncher, ReloadLauncherPacket.CreateInstance },
                { EFirearmSubPacketType.ReloadBarrels, ReloadBarrelsPacket.CreateInstance },
                { EFirearmSubPacketType.Grenade, GrenadePacket.CreateInstance },
                { EFirearmSubPacketType.CancelGrenade, CancelGrenadePacket.CreateInstance },
                { EFirearmSubPacketType.CompassChange, CompassChangePacket.CreateInstance },
                { EFirearmSubPacketType.Knife, KnifePacket.CreateInstance },
                { EFirearmSubPacketType.FlareShot, FlareShotPacket.CreateInstance },
                { EFirearmSubPacketType.RocketShot, RocketShotPacket.CreateInstance },
                { EFirearmSubPacketType.ReloadBoltAction, ReloadBoltActionPacket.CreateInstance },
                { EFirearmSubPacketType.RollCylinder, RollCylinderPacket.CreateInstance },
                { EFirearmSubPacketType.UnderbarrelSightingRangeUp, UnderbarrelSightingRangeUpPacket.CreateInstance },
                { EFirearmSubPacketType.UnderbarrelSightingRangeDown, UnderbarrelSightingRangeDownPacket.CreateInstance },
                { EFirearmSubPacketType.ToggleBipod, ToggleBipodPacket.CreateInstance },
                { EFirearmSubPacketType.LeftStanceChange, LeftStanceChangePacket.CreateInstance },
            };
        }
    }
}