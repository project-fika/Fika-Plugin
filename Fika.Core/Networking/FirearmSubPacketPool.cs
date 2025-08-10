using Fika.Core.Networking.Packets.FirearmController;
using System;
using System.Collections.Generic;
using static Fika.Core.Networking.Packets.FirearmController.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking
{
    internal sealed class FirearmSubPacketPool
    {
        public static void CreatePool()
        {
            if (_pool == null)
            {
                _pool = [];
                foreach ((EFirearmSubPacketType key, Func<IPoolSubPacket> value) in _subPacketFactories)
                {
                    _pool[key] = new(4, value);
                }
            }

            _poolExists = true;
        }

        public static void ClearPool()
        {
            if (_pool != null)
            {
                foreach ((EFirearmSubPacketType _, SubPacketPoolManager<IPoolSubPacket> value) in _pool)
                {
                    value.Dispose();
                }
            }

            _pool = null;
            _poolExists = false;
        }

        public static T GetPacket<T>(EFirearmSubPacketType type) where T : IPoolSubPacket
        {
            if (!_poolExists)
            {
                CreatePool();
            }

            return (T)WithdrawPacket(type).Withdraw();
        }

        public static void ReturnPacket(WeaponPacket packet)
        {
            if (!_poolExists)
            {
                return;
            }

            packet.SubPacket.Dispose();
            WithdrawPacket(packet.Type).Return(packet.SubPacket);
        }

        private static SubPacketPoolManager<IPoolSubPacket> WithdrawPacket(EFirearmSubPacketType type)
        {
            if (!_pool.TryGetValue(type, out SubPacketPoolManager<IPoolSubPacket> packet))
            {
                throw new ArgumentException("Could not find given type in the packet pool manager!", nameof(type));
            }

            return packet;
        }

        private static bool _poolExists;
        private static Dictionary<EFirearmSubPacketType, SubPacketPoolManager<IPoolSubPacket>> _pool;

        private static readonly Dictionary<EFirearmSubPacketType, Func<IPoolSubPacket>> _subPacketFactories = new()
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

        private class SubPacketPoolManager<T> : IDisposable
        {
            private readonly Queue<T> _queue;
            private readonly Func<T> _constructor;

            public SubPacketPoolManager(int size, Func<T> constructor)
            {
                _queue = new Queue<T>(size);
                _constructor = constructor;
                for (int i = 0; i < size; i++)
                {
                    T t = _constructor();
                    _queue.Enqueue(t);
                }
            }

            public T Withdraw()
            {
                T t;
                if (_queue.Count > 0)
                {
                    t = _queue.Dequeue();
                }
                else
                {
                    t = _constructor();
                }
                return t;
            }

            public void Return(T t)
            {
                _queue.Enqueue(t);
            }

            public void Dispose()
            {
                _queue.Clear();
            }
        }
    }
}
