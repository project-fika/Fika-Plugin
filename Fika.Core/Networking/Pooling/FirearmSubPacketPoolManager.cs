using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.FirearmController;
using System;
using System.Threading;

namespace Fika.Core.Networking.Pooling;

internal sealed class FirearmSubPacketPoolManager : BasePacketPoolManager<EFirearmSubPacketType, IPoolSubPacket>
{
    private static readonly Lazy<FirearmSubPacketPoolManager> _instance = new(() => new FirearmSubPacketPoolManager(), LazyThreadSafetyMode.None);
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
    }

    private FirearmSubPacketPoolManager()
    {
        _subPacketFactories =
        [
            ShotInfoPacket.CreateInstance,               // EFirearmSubPacketType.ShotInfo = 0
            ChangeFireModePacket.CreateInstance,          // EFirearmSubPacketType.ChangeFireMode = 1
            ToggleAimPacket.CreateInstance,                // EFirearmSubPacketType.ToggleAim = 2
            ExamineWeaponPacket.CreateInstance,            // EFirearmSubPacketType.ExamineWeapon = 3
            CheckAmmoPacket.CreateInstance,                 // EFirearmSubPacketType.CheckAmmo = 4
            CheckChamberPacket.CreateInstance,              // EFirearmSubPacketType.CheckChamber = 5
            CheckFireModePacket.CreateInstance,             // EFirearmSubPacketType.CheckFireMode = 6
            LightStatesPacket.CreateInstance,               // EFirearmSubPacketType.ToggleLightStates = 7
            ScopeStatesPacket.CreateInstance,               // EFirearmSubPacketType.ToggleScopeStates = 8
            ToggleLauncherPacket.CreateInstance,            // EFirearmSubPacketType.ToggleLauncher = 9
            ToggleInventoryPacket.CreateInstance,           // EFirearmSubPacketType.ToggleInventory = 10
            FirearmLootPacket.CreateInstance,               // EFirearmSubPacketType.Loot = 11
            ReloadMagPacket.CreateInstance,                  // EFirearmSubPacketType.ReloadMag = 12
            QuickReloadMagPacket.CreateInstance,             // EFirearmSubPacketType.QuickReloadMag = 13
            ReloadWithAmmoPacket.CreateInstance,             // EFirearmSubPacketType.ReloadWithAmmo = 14
            CylinderMagPacket.CreateInstance,                 // EFirearmSubPacketType.CylinderMag = 15
            ReloadLauncherPacket.CreateInstance,             // EFirearmSubPacketType.ReloadLauncher = 16
            ReloadBarrelsPacket.CreateInstance,              // EFirearmSubPacketType.ReloadBarrels = 17
            GrenadePacket.CreateInstance,                     // EFirearmSubPacketType.Grenade = 18
            CancelGrenadePacket.CreateInstance,               // EFirearmSubPacketType.CancelGrenade = 19
            CompassChangePacket.CreateInstance,               // EFirearmSubPacketType.CompassChange = 20
            KnifePacket.CreateInstance,                        // EFirearmSubPacketType.Knife = 21
            FlareShotPacket.CreateInstance,                    // EFirearmSubPacketType.FlareShot = 22
            RocketShotPacket.CreateInstance,                   // EFirearmSubPacketType.RocketShot = 23
            ReloadBoltActionPacket.CreateInstance,             // EFirearmSubPacketType.ReloadBoltAction = 24
            RollCylinderPacket.CreateInstance,                  // EFirearmSubPacketType.RollCylinder = 25
            UnderbarrelSightingRangeUpPacket.CreateInstance,    // EFirearmSubPacketType.UnderbarrelSightingRangeUp = 26
            UnderbarrelSightingRangeDownPacket.CreateInstance,  // EFirearmSubPacketType.UnderbarrelSightingRangeDown = 27
            ToggleBipodPacket.CreateInstance,                    // EFirearmSubPacketType.ToggleBipod = 28
            LeftStanceChangePacket.CreateInstance                // EFirearmSubPacketType.LeftStanceChange = 29
        ];
    }
}