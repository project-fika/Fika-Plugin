using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using static EFT.Player;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking.Packets.FirearmController
{
    public class FirearmSubPackets
    {
        public class ToggleAimPacket : IPoolSubPacket
        {
            private ToggleAimPacket()
            {

            }

            public static ToggleAimPacket FromValue(int aimingIndex)
            {
                ToggleAimPacket packet = FirearmSubPacketPool.GetPacket<ToggleAimPacket>(EFirearmSubPacketType.ToggleAim);
                packet.AimingIndex = aimingIndex;
                return packet;
            }

            public static ToggleAimPacket CreateInstance()
            {
                return new();
            }

            public int AimingIndex;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.SetAim(AimingIndex);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(AimingIndex);
            }

            public void Deserialize(NetDataReader reader)
            {
                AimingIndex = reader.GetInt();
            }

            public void Dispose()
            {
                AimingIndex = 0;
            }
        }

        public class ChangeFireModePacket : IPoolSubPacket
        {
            private ChangeFireModePacket()
            {

            }

            public static ChangeFireModePacket FromValue(Weapon.EFireMode fireMode)
            {
                ChangeFireModePacket packet = FirearmSubPacketPool.GetPacket<ChangeFireModePacket>(EFirearmSubPacketType.ChangeFireMode);
                packet.FireMode = fireMode;
                return packet;
            }

            public static ChangeFireModePacket CreateInstance()
            {
                return new();
            }

            public Weapon.EFireMode FireMode;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.ChangeFireMode(FireMode);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(FireMode);
            }

            public void Deserialize(NetDataReader reader)
            {
                FireMode = reader.GetEnum<Weapon.EFireMode>();
            }

            public void Dispose()
            {
                FireMode = Weapon.EFireMode.fullauto;
            }
        }

        public class ExamineWeaponPacket : IPoolSubPacket
        {
            private ExamineWeaponPacket()
            {

            }

            public static ExamineWeaponPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<ExamineWeaponPacket>(EFirearmSubPacketType.ExamineWeapon);
            }

            public static ExamineWeaponPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.ExamineWeapon();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class CheckAmmoPacket : IPoolSubPacket
        {
            private CheckAmmoPacket()
            {

            }

            public static CheckAmmoPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<CheckAmmoPacket>(EFirearmSubPacketType.CheckAmmo);
            }

            public static CheckAmmoPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.CheckAmmo();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class ToggleLauncherPacket : IPoolSubPacket
        {
            private ToggleLauncherPacket()
            {

            }

            public static ToggleLauncherPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<ToggleLauncherPacket>(EFirearmSubPacketType.ToggleLauncher);
            }

            public static ToggleLauncherPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.ToggleLauncher();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class ToggleInventoryPacket : IPoolSubPacket
        {
            private ToggleInventoryPacket()
            {

            }

            public static ToggleInventoryPacket FromValue(bool open)
            {
                ToggleInventoryPacket packet = FirearmSubPacketPool.GetPacket<ToggleInventoryPacket>(EFirearmSubPacketType.ToggleInventory);
                packet.Open = open;
                return packet;
            }

            public static ToggleInventoryPacket CreateInstance()
            {
                return new();
            }

            public bool Open;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.SetInventoryOpened(Open);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Open);
            }

            public void Deserialize(NetDataReader reader)
            {
                Open = reader.GetBool();
            }

            public void Dispose()
            {
                Open = false;
            }
        }

        public class FirearmLootPacket : IPoolSubPacket
        {
            private FirearmLootPacket()
            {

            }

            public static FirearmLootPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<FirearmLootPacket>(EFirearmSubPacketType.Loot);
            }

            public static FirearmLootPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                player.HandsController.Loot(true);
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class CancelGrenadePacket : IPoolSubPacket
        {
            private CancelGrenadePacket()
            {

            }

            public static CancelGrenadePacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<CancelGrenadePacket>(EFirearmSubPacketType.CancelGrenade);
            }

            public static CancelGrenadePacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedGrenadeController grenadeController)
                {
                    grenadeController.vmethod_3();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class CompassChangePacket : IPoolSubPacket
        {
            private CompassChangePacket()
            {

            }

            public static CompassChangePacket FromValue(bool enabled)
            {
                CompassChangePacket packet = FirearmSubPacketPool.GetPacket<CompassChangePacket>(EFirearmSubPacketType.CompassChange);
                packet.Enabled = enabled;
                return packet;
            }

            public static CompassChangePacket CreateInstance()
            {
                return new();
            }

            public bool Enabled;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ItemHandsController handsController)
                {
                    handsController.CompassState.Value = Enabled;
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Enabled);
            }

            public void Deserialize(NetDataReader reader)
            {
                Enabled = reader.GetBool();
            }

            public void Dispose()
            {
                Enabled = false;
            }
        }

        public class LeftStanceChangePacket : IPoolSubPacket
        {
            private LeftStanceChangePacket()
            {

            }

            public static LeftStanceChangePacket FromValue(bool leftStance)
            {
                LeftStanceChangePacket packet = FirearmSubPacketPool.GetPacket<LeftStanceChangePacket>(EFirearmSubPacketType.LeftStanceChange);
                packet.LeftStance = leftStance;
                return packet;
            }

            public static LeftStanceChangePacket CreateInstance()
            {
                return new();
            }

            public bool LeftStance;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    if (player.MovementContext.LeftStanceEnabled != LeftStance)
                    {
                        controller.ChangeLeftStance();
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(LeftStance);
            }

            public void Deserialize(NetDataReader reader)
            {
                LeftStance = reader.GetBool();
            }

            public void Dispose()
            {
                LeftStance = false;
            }
        }

        public class RollCylinderPacket : IPoolSubPacket
        {
            private RollCylinderPacket()
            {

            }

            public static RollCylinderPacket FromValue(bool rollToZeroCamora)
            {
                RollCylinderPacket packet = FirearmSubPacketPool.GetPacket<RollCylinderPacket>(EFirearmSubPacketType.RollCylinder);
                packet.RollToZeroCamora = rollToZeroCamora;
                return packet;
            }

            public static RollCylinderPacket CreateInstance()
            {
                return new();
            }

            public bool RollToZeroCamora;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller && controller.Weapon is RevolverItemClass)
                {
                    controller.RollCylinder(RollToZeroCamora);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(RollToZeroCamora);
            }

            public void Deserialize(NetDataReader reader)
            {
                RollToZeroCamora = reader.GetBool();
            }

            public void Dispose()
            {
                RollToZeroCamora = false;
            }
        }

        public class ReloadBoltActionPacket : IPoolSubPacket
        {
            private ReloadBoltActionPacket()
            {

            }

            public static ReloadBoltActionPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<ReloadBoltActionPacket>(EFirearmSubPacketType.ReloadBoltAction);
            }

            public static ReloadBoltActionPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.HandleObservedBoltAction();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class CheckChamberPacket : IPoolSubPacket
        {
            private CheckChamberPacket()
            {

            }

            public static CheckChamberPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<CheckChamberPacket>(EFirearmSubPacketType.CheckChamber);
            }

            public static CheckChamberPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.CheckChamber();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class CheckFireModePacket : IPoolSubPacket
        {
            private CheckFireModePacket()
            {

            }

            public static CheckFireModePacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<CheckFireModePacket>(EFirearmSubPacketType.CheckFireMode);
            }

            public static CheckFireModePacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.CheckFireMode();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class UnderbarrelSightingRangeUpPacket : IPoolSubPacket
        {
            private UnderbarrelSightingRangeUpPacket()
            {

            }

            public static UnderbarrelSightingRangeUpPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<UnderbarrelSightingRangeUpPacket>(EFirearmSubPacketType.UnderbarrelSightingRangeUp);
            }

            public static UnderbarrelSightingRangeUpPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.UnderbarrelSightingRangeUp();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class UnderbarrelSightingRangeDownPacket : IPoolSubPacket
        {
            private UnderbarrelSightingRangeDownPacket()
            {

            }

            public static UnderbarrelSightingRangeDownPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<UnderbarrelSightingRangeDownPacket>(EFirearmSubPacketType.UnderbarrelSightingRangeDown);
            }


            public static UnderbarrelSightingRangeDownPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.UnderbarrelSightingRangeDown();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class ToggleBipodPacket : IPoolSubPacket
        {
            private ToggleBipodPacket()
            {

            }

            public static ToggleBipodPacket FromValue()
            {
                return FirearmSubPacketPool.GetPacket<ToggleBipodPacket>(EFirearmSubPacketType.ToggleBipod);
            }


            public static ToggleBipodPacket CreateInstance()
            {
                return new();
            }

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.ToggleBipod();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                // do nothing
            }

            public void Deserialize(NetDataReader reader)
            {
                // do nothing
            }

            public void Dispose()
            {
                // do nothing
            }
        }

        public class ShotInfoPacket : IPoolSubPacket
        {
            private ShotInfoPacket()
            {

            }

            public static ShotInfoPacket CreateInstance()
            {
                return new();
            }

            public Vector3 ShotPosition;
            public Vector3 ShotDirection;
            public MongoID AmmoTemplate;
            public float Overheat;
            public float LastShotOverheat;
            public float LastShotTime;
            public float Durability;
            public int ChamberIndex;
            public bool UnderbarrelShot;
            public bool SlideOnOverheatReached;
            public EShotType ShotType;

            public static ShotInfoPacket FromDryShot(int chamberIndex, bool underbarrelShot, EShotType shotType)
            {
                ShotInfoPacket packet = FirearmSubPacketPool.GetPacket<ShotInfoPacket>(EFirearmSubPacketType.ShotInfo);
                packet.ShotType = shotType;
                packet.ChamberIndex = chamberIndex;
                packet.UnderbarrelShot = underbarrelShot;
                return packet;
            }

            public static ShotInfoPacket FromMisfire(MongoID ammoTemplate, float overheat, EShotType shotType)
            {
                ShotInfoPacket packet = FirearmSubPacketPool.GetPacket<ShotInfoPacket>(EFirearmSubPacketType.ShotInfo);
                packet.AmmoTemplate = ammoTemplate;
                packet.Overheat = overheat;
                packet.ShotType = shotType;
                return packet;
            }

            public static ShotInfoPacket FromShot(Vector3 shotPosition, Vector3 shotDirection, MongoID ammoTemplate, float overheat,
                float lastShotOverheat, float lastShotTime, float durability, int chamberIndex, bool underbarrelShot,
                bool slideOnOverheatReached, EShotType shotType)
            {
                ShotInfoPacket packet = FirearmSubPacketPool.GetPacket<ShotInfoPacket>(EFirearmSubPacketType.ShotInfo);
                packet.ShotPosition = shotPosition;
                packet.ShotDirection = shotDirection;
                packet.AmmoTemplate = ammoTemplate;
                packet.Overheat = overheat;
                packet.LastShotOverheat = lastShotOverheat;
                packet.LastShotTime = lastShotTime;
                packet.Durability = durability;
                packet.ChamberIndex = chamberIndex;
                packet.UnderbarrelShot = underbarrelShot;
                packet.SlideOnOverheatReached = slideOnOverheatReached;
                packet.ShotType = shotType;
                return packet;
            }

            public void Execute(FikaPlayer player)
            {
                if (!player.HealthController.IsAlive)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ShotInfoPacket::Execute: Player was not alive, can not process!");
                    return;
                }

                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.HandleShotInfoPacket(this, player.InventoryController);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(ShotType);
                if (ShotType == EShotType.DryFire)
                {
                    writer.PutPackedInt(ChamberIndex, 0, 16);
                    writer.Put(UnderbarrelShot);
                    return;
                }

                writer.PutStruct(ShotPosition);
                writer.PutStruct(ShotDirection);
                writer.PutMongoID(AmmoTemplate);
                writer.PutPackedFloat(Overheat, 0f, 200f, EFloatCompression.High);
                writer.PutPackedFloat(LastShotOverheat, 0f, 200f, EFloatCompression.High);
                writer.Put(LastShotTime);
                writer.PutPackedFloat(Durability, 0f, 100f, EFloatCompression.High);
                writer.PutPackedInt(ChamberIndex, 0, 16);
                writer.Put(UnderbarrelShot);
                writer.Put(SlideOnOverheatReached);
            }

            public void Deserialize(NetDataReader reader)
            {
                ShotType = reader.GetEnum<EShotType>();
                if (ShotType == EShotType.DryFire)
                {
                    ChamberIndex = reader.GetPackedInt(0, 16);
                    UnderbarrelShot = reader.GetBool();
                    return;
                }

                ShotPosition = reader.GetStruct<Vector3>();
                ShotDirection = reader.GetStruct<Vector3>();
                AmmoTemplate = reader.GetMongoID();
                Overheat = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
                LastShotOverheat = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
                LastShotTime = reader.GetFloat();
                Durability = reader.GetPackedFloat(0f, 100f, EFloatCompression.High);
                ChamberIndex = reader.GetPackedInt(0, 16);
                UnderbarrelShot = reader.GetBool();
                SlideOnOverheatReached = reader.GetBool();
            }

            public void Dispose()
            {
                ShotPosition = default;
                ShotDirection = default;
                AmmoTemplate = default;
                Overheat = 0f;
                LastShotOverheat = 0f;
                LastShotTime = 0f;
                Durability = 0f;
                ChamberIndex = 0;
                UnderbarrelShot = false;
                SlideOnOverheatReached = false;
                ShotType = default;
            }
        }

        public class KnifePacket : IPoolSubPacket
        {
            private KnifePacket()
            {

            }

            public static KnifePacket FromValue(bool examine, bool kick, bool altKick, bool breakCombo)
            {
                KnifePacket packet = FirearmSubPacketPool.GetPacket<KnifePacket>(EFirearmSubPacketType.Knife);
                packet.Examine = examine;
                packet.Kick = kick;
                packet.AltKick = altKick;
                packet.BreakCombo = breakCombo;
                return packet;
            }

            public static KnifePacket CreateInstance()
            {
                return new();
            }

            public bool Examine;
            public bool Kick;
            public bool AltKick;
            public bool BreakCombo;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedKnifeController knifeController)
                {
                    if (Examine)
                    {
                        knifeController.ExamineWeapon();
                    }

                    if (Kick)
                    {
                        knifeController.MakeKnifeKick();
                    }

                    if (AltKick)
                    {
                        knifeController.MakeAlternativeKick();
                    }

                    if (BreakCombo)
                    {
                        knifeController.BrakeCombo();
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"KnifePacket: HandsController was not of type CoopObservedKnifeController! Was {player.HandsController.GetType().Name}");
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Examine);
                writer.Put(Kick);
                writer.Put(AltKick);
                writer.Put(BreakCombo);
            }

            public void Deserialize(NetDataReader reader)
            {
                Examine = reader.GetBool();
                Kick = reader.GetBool();
                AltKick = reader.GetBool();
                BreakCombo = reader.GetBool();
            }

            public void Dispose()
            {
                Examine = false;
                Kick = false;
                AltKick = false;
                BreakCombo = false;
            }
        }

        public class GrenadePacket : IPoolSubPacket
        {
            private GrenadePacket()
            {

            }

            public static GrenadePacket FromValue(Quaternion grenadeRotation, Vector3 grenadePosition, Vector3 throwForce,
                EGrenadePacketType type, bool hasGrenade, bool lowThrow, bool plantTripwire, bool changeToIdle, bool changeToPlant)
            {
                GrenadePacket packet = FirearmSubPacketPool.GetPacket<GrenadePacket>(EFirearmSubPacketType.Grenade);
                packet.GrenadeRotation = grenadeRotation;
                packet.GrenadePosition = grenadePosition;
                packet.ThrowForce = throwForce;
                packet.Type = type;
                packet.HasGrenade = hasGrenade;
                packet.LowThrow = lowThrow;
                packet.PlantTripwire = plantTripwire;
                packet.ChangeToIdle = changeToIdle;
                packet.ChangeToPlant = changeToPlant;
                return packet;
            }

            public static GrenadePacket CreateInstance()
            {
                return new();
            }

            public Quaternion GrenadeRotation;
            public Vector3 GrenadePosition;
            public Vector3 ThrowForce;
            public EGrenadePacketType Type;
            public bool HasGrenade;
            public bool LowThrow;
            public bool PlantTripwire;
            public bool ChangeToIdle;
            public bool ChangeToPlant;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedGrenadeController controller)
                {
                    switch (Type)
                    {
                        case EGrenadePacketType.ExamineWeapon:
                            {
                                controller.ExamineWeapon();
                                break;
                            }
                        case EGrenadePacketType.HighThrow:
                            {
                                controller.HighThrow();
                                break;
                            }
                        case EGrenadePacketType.LowThrow:
                            {
                                controller.LowThrow();
                                break;
                            }
                        case EGrenadePacketType.PullRingForHighThrow:
                            {
                                controller.PullRingForHighThrow();
                                break;
                            }
                        case EGrenadePacketType.PullRingForLowThrow:
                            {
                                controller.PullRingForLowThrow();
                                break;
                            }
                    }
                    if (HasGrenade)
                    {
                        controller.SpawnGrenade(0f, GrenadePosition, GrenadeRotation, ThrowForce, LowThrow);
                    }

                    if (PlantTripwire)
                    {
                        controller.PlantTripwire();
                    }

                    if (ChangeToIdle)
                    {
                        controller.ChangeFireMode(Weapon.EFireMode.grenadeThrowing);
                    }

                    if (ChangeToPlant)
                    {
                        controller.ChangeFireMode(Weapon.EFireMode.greanadePlanting);
                    }
                }
                else if (player.HandsController is ObservedQuickGrenadeController quickGrenadeController)
                {
                    if (HasGrenade)
                    {
                        quickGrenadeController.SpawnGrenade(0f, GrenadePosition, GrenadeRotation, ThrowForce, LowThrow);
                    }
                }
                else
                {
                    FikaPlugin.Instance.FikaLogger.LogError($"GrenadePacket: HandsController was not of type CoopObservedGrenadeController! Was {player.HandsController.GetType().Name}");
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(Type);
                writer.Put(HasGrenade);
                if (HasGrenade)
                {
                    writer.PutStruct(GrenadeRotation);
                    writer.PutStruct(GrenadePosition);
                    writer.PutStruct(ThrowForce);
                    writer.Put(LowThrow);
                }
                writer.Put(PlantTripwire);
                writer.Put(ChangeToIdle);
                writer.Put(ChangeToPlant);
            }

            public void Deserialize(NetDataReader reader)
            {
                Type = reader.GetEnum<EGrenadePacketType>();
                HasGrenade = reader.GetBool();
                if (HasGrenade)
                {
                    GrenadeRotation = reader.GetStruct<Quaternion>();
                    GrenadePosition = reader.GetStruct<Vector3>();
                    ThrowForce = reader.GetStruct<Vector3>();
                    LowThrow = reader.GetBool();
                }
                PlantTripwire = reader.GetBool();
                ChangeToIdle = reader.GetBool();
                ChangeToPlant = reader.GetBool();
            }

            public void Dispose()
            {
                GrenadeRotation = default;
                GrenadePosition = default;
                ThrowForce = default;
                Type = default;
                HasGrenade = false;
                LowThrow = false;
                PlantTripwire = false;
                ChangeToIdle = false;
                ChangeToPlant = false;
            }
        }

        public class LightStatesPacket : IPoolSubPacket
        {
            private LightStatesPacket()
            {

            }

            public static LightStatesPacket FromValue(int amount, FirearmLightStateStruct[] states)
            {
                LightStatesPacket packet = FirearmSubPacketPool.GetPacket<LightStatesPacket>(EFirearmSubPacketType.ToggleLightStates);
                packet.Amount = amount;
                packet.States = states;
                return packet;
            }

            public static LightStatesPacket CreateInstance()
            {
                return new();
            }

            public int Amount;
            public FirearmLightStateStruct[] States;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.SetLightsState(States, true);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Amount);
                if (Amount > 0)
                {
                    for (int i = 0; i < Amount; i++)
                    {
                        writer.Put(States[i].Id);
                        writer.Put(States[i].IsActive);
                        writer.Put(States[i].LightMode);
                    }
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Amount = reader.GetInt();
                if (Amount > 0)
                {
                    States = new FirearmLightStateStruct[Amount];
                    for (int i = 0; i < Amount; i++)
                    {
                        States[i] = new()
                        {
                            Id = reader.GetString(),
                            IsActive = reader.GetBool(),
                            LightMode = reader.GetInt()
                        };
                    }
                }
            }

            public void Dispose()
            {
                Amount = 0;
                States = null;
            }
        }

        public class ScopeStatesPacket : IPoolSubPacket
        {
            private ScopeStatesPacket()
            {

            }
            public static ScopeStatesPacket FromValue(int amount, FirearmScopeStateStruct[] states)
            {
                ScopeStatesPacket packet = FirearmSubPacketPool.GetPacket<ScopeStatesPacket>(EFirearmSubPacketType.ToggleScopeStates);
                packet.Amount = amount;
                packet.States = states;
                return packet;
            }

            public static ScopeStatesPacket CreateInstance()
            {
                return new();
            }

            public int Amount;
            public FirearmScopeStateStruct[] States;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    controller.SetScopeMode(States);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Amount);
                if (Amount > 0)
                {
                    for (int i = 0; i < Amount; i++)
                    {
                        writer.Put(States[i].Id);
                        writer.Put(States[i].ScopeMode);
                        writer.Put(States[i].ScopeIndexInsideSight);
                        writer.Put(States[i].ScopeCalibrationIndex);
                    }
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Amount = reader.GetInt();
                if (Amount > 0)
                {
                    States = new FirearmScopeStateStruct[Amount];
                    for (int i = 0; i < Amount; i++)
                    {
                        States[i] = new()
                        {
                            Id = reader.GetString(),
                            ScopeMode = reader.GetInt(),
                            ScopeIndexInsideSight = reader.GetInt(),
                            ScopeCalibrationIndex = reader.GetInt()
                        };
                    }
                }
            }

            public void Dispose()
            {
                Amount = 0;
                States = null;
            }
        }

        public class ReloadMagPacket : IPoolSubPacket
        {
            private ReloadMagPacket()
            {

            }

            public static ReloadMagPacket FromValue(MongoID magId, byte[] locationDescription, bool reload)
            {
                ReloadMagPacket packet = FirearmSubPacketPool.GetPacket<ReloadMagPacket>(EFirearmSubPacketType.ReloadMag);
                packet.MagId = magId;
                packet.LocationDescription = locationDescription;
                packet.Reload = reload;
                return packet;
            }

            public static ReloadMagPacket CreateInstance()
            {
                return new();
            }

            public MongoID MagId;
            public byte[] LocationDescription;
            public bool Reload;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    MagazineItemClass magazine = null;
                    try
                    {
                        GStruct461<Item> result = player.FindItemById(MagId);
                        if (!result.Succeeded)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                            return;
                        }
                        if (result.Value is MagazineItemClass magazineClass)
                        {
                            magazine = magazineClass;
                        }
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: Item was not MagazineClass, it was {result.Value.GetType()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError(ex);
                        FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
                        throw;
                    }
                    ItemAddress gridItemAddress = null;
                    if (LocationDescription != null)
                    {
                        try
                        {
                            using GClass1278 eftReader = PacketToEFTReaderAbstractClass.Get(LocationDescription);
                            if (LocationDescription.Length != 0)
                            {
                                GClass1785 descriptor = eftReader.ReadPolymorph<GClass1785>();
                                gridItemAddress = player.InventoryController.ToItemAddress(descriptor);
                            }
                        }
                        catch (GException4 exception2)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError(exception2);
                        }
                    }
                    if (magazine != null)
                    {
                        controller.FastForwardCurrentState();
                        controller.ReloadMag(magazine, gridItemAddress, null);
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: final variables were null! Mag: {magazine}, Address: {gridItemAddress}");
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutMongoID(MagId);
                    writer.PutByteArray(LocationDescription);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    MagId = reader.GetMongoID();
                    LocationDescription = reader.GetByteArray();
                }
            }

            public void Dispose()
            {
                MagId = default;
                LocationDescription = null;
                Reload = false;
            }
        }

        public class QuickReloadMagPacket : IPoolSubPacket
        {
            private QuickReloadMagPacket()
            {

            }

            public static QuickReloadMagPacket FromValue(MongoID magId, bool reload)
            {
                QuickReloadMagPacket packet = FirearmSubPacketPool.GetPacket<QuickReloadMagPacket>(EFirearmSubPacketType.QuickReloadMag);
                packet.MagId = magId;
                packet.Reload = reload;
                return packet;
            }

            public static QuickReloadMagPacket CreateInstance()
            {
                return new();
            }

            public MongoID MagId;
            public bool Reload;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    try
                    {
                        GStruct461<Item> result = player.FindItemById(MagId);
                        if (!result.Succeeded)
                        {
                            FikaPlugin.Instance.FikaLogger.LogError(result.Error);
                            return;
                        }
                        if (result.Value is MagazineItemClass magazine)
                        {
                            controller.FastForwardCurrentState();
                            controller.QuickReloadMag(magazine, null);
                        }
                        else
                        {
                            FikaPlugin.Instance.FikaLogger.LogError($"QuickReloadMagPacket: item was not of type MagazineClass, was {result.Value.GetType()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError(ex);
                        FikaPlugin.Instance.FikaLogger.LogError($"QuickReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
                        throw;
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutMongoID(MagId);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    MagId = reader.GetMongoID();
                }
            }

            public void Dispose()
            {
                MagId = default;
                Reload = false;
            }
        }

        public class ReloadWithAmmoPacket : IPoolSubPacket
        {
            private ReloadWithAmmoPacket()
            {

            }

            public static ReloadWithAmmoPacket FromValue(bool reload, EReloadWithAmmoStatus status, int ammoLoadedToMag = 0, string[] ammoIds = null)
            {
                ReloadWithAmmoPacket packet = FirearmSubPacketPool.GetPacket<ReloadWithAmmoPacket>(EFirearmSubPacketType.ReloadWithAmmo);
                packet.Reload = reload;
                packet.Status = status;
                packet.AmmoLoadedToMag = ammoLoadedToMag;
                packet.AmmoIds = ammoIds;
                return packet;
            }

            public static ReloadWithAmmoPacket CreateInstance()
            {
                return new();
            }

            public bool Reload;
            public EReloadWithAmmoStatus Status;
            public int AmmoLoadedToMag;
            public string[] AmmoIds;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    if (Status == EReloadWithAmmoStatus.AbortReload)
                    {
                        controller.CurrentOperation.SetTriggerPressed(true);
                    }

                    if (Reload)
                    {
                        if (Status == EReloadWithAmmoStatus.StartReload)
                        {
                            List<AmmoItemClass> bullets = controller.FindAmmoByIds(AmmoIds);
                            AmmoPackReloadingClass ammoPack = new(bullets);
                            controller.FastForwardCurrentState();
                            controller.CurrentOperation.ReloadWithAmmo(ammoPack, null, null);
                        }
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutEnum(Status);
                    if (Status == EReloadWithAmmoStatus.StartReload)
                    {
                        writer.PutArray(AmmoIds);
                    }
                    if (AmmoLoadedToMag > 0)
                    {
                        writer.Put(AmmoLoadedToMag);
                    }
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    Status = reader.GetEnum<EReloadWithAmmoStatus>();
                    if (Status == EReloadWithAmmoStatus.StartReload)
                    {
                        AmmoIds = reader.GetStringArray();
                    }
                    if (Status is EReloadWithAmmoStatus.EndReload or EReloadWithAmmoStatus.AbortReload)
                    {
                        AmmoLoadedToMag = reader.GetInt();
                    }
                }
            }

            public void Dispose()
            {
                Reload = false;
                Status = EReloadWithAmmoStatus.None;
                AmmoLoadedToMag = 0;
                AmmoIds = null;
            }
        }

        public class CylinderMagPacket : IPoolSubPacket
        {
            private CylinderMagPacket()
            {

            }

            public static CylinderMagPacket FromValue(EReloadWithAmmoStatus status, int camoraIndex, int ammoLoadedToMag, bool changed, bool hammerClosed, bool reload, string[] ammoIds)
            {
                CylinderMagPacket packet = FirearmSubPacketPool.GetPacket<CylinderMagPacket>(EFirearmSubPacketType.CylinderMag);
                packet.Status = status;
                packet.CamoraIndex = camoraIndex;
                packet.AmmoLoadedToMag = ammoLoadedToMag;
                packet.Changed = changed;
                packet.HammerClosed = hammerClosed;
                packet.Reload = reload;
                packet.AmmoIds = ammoIds;
                return packet;
            }

            public static CylinderMagPacket CreateInstance()
            {
                return new();
            }

            public EReloadWithAmmoStatus Status;
            public int CamoraIndex;
            public int AmmoLoadedToMag;
            public bool Changed;
            public bool HammerClosed;
            public bool Reload;
            public string[] AmmoIds;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    if (Status == EReloadWithAmmoStatus.AbortReload)
                    {
                        controller.CurrentOperation.SetTriggerPressed(true);
                    }

                    if (Reload)
                    {
                        if (Status == EReloadWithAmmoStatus.StartReload)
                        {
                            List<AmmoItemClass> bullets = controller.FindAmmoByIds(AmmoIds);
                            AmmoPackReloadingClass ammoPack = new(bullets);
                            controller.FastForwardCurrentState();
                            controller.CurrentOperation.ReloadCylinderMagazine(ammoPack, null, null);
                        }
                    }

                    if (Changed && controller.Weapon.GetCurrentMagazine() is CylinderMagazineItemClass cylinder)
                    {
                        cylinder.SetCurrentCamoraIndex(CamoraIndex);
                        controller.Weapon.CylinderHammerClosed = HammerClosed;
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Changed);
                if (Changed)
                {
                    writer.Put(CamoraIndex);
                    writer.Put(HammerClosed);
                }
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutEnum(Status);
                    writer.Put(AmmoLoadedToMag);
                    writer.PutArray(AmmoIds);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Changed = reader.GetBool();
                if (Changed)
                {
                    CamoraIndex = reader.GetInt();
                    HammerClosed = reader.GetBool();
                }
                Reload = reader.GetBool();
                if (Reload)
                {
                    Status = reader.GetEnum<EReloadWithAmmoStatus>();
                    AmmoLoadedToMag = reader.GetInt();
                    AmmoIds = reader.GetStringArray();
                }
            }

            public void Dispose()
            {
                Status = default;
                CamoraIndex = 0;
                AmmoLoadedToMag = 0;
                Changed = false;
                HammerClosed = false;
                Reload = false;
                AmmoIds = null;
            }
        }

        public class ReloadLauncherPacket : IPoolSubPacket
        {
            private ReloadLauncherPacket()
            {

            }

            public static ReloadLauncherPacket FromValue(bool reload, string[] ammoIds)
            {
                ReloadLauncherPacket packet = FirearmSubPacketPool.GetPacket<ReloadLauncherPacket>(EFirearmSubPacketType.ReloadLauncher);
                packet.Reload = reload;
                packet.AmmoIds = ammoIds;
                return packet;
            }

            public static ReloadLauncherPacket CreateInstance()
            {
                return new();
            }

            public string[] AmmoIds;
            public bool Reload;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    List<AmmoItemClass> ammo = controller.FindAmmoByIds(AmmoIds);
                    AmmoPackReloadingClass ammoPack = new(ammo);
                    controller.FastForwardCurrentState();
                    controller.ReloadGrenadeLauncher(ammoPack, null);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutArray(AmmoIds);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    AmmoIds = reader.GetStringArray();
                }
            }

            public void Dispose()
            {
                Reload = false;
                AmmoIds = null;
            }
        }

        public class ReloadBarrelsPacket : IPoolSubPacket
        {
            private ReloadBarrelsPacket()
            {

            }

            public static ReloadBarrelsPacket FromValue(bool reload, string[] ammoIds, byte[] locationDescription)
            {
                ReloadBarrelsPacket packet = FirearmSubPacketPool.GetPacket<ReloadBarrelsPacket>(EFirearmSubPacketType.ReloadBarrels);
                packet.Reload = reload;
                packet.AmmoIds = ammoIds;
                packet.LocationDescription = locationDescription;
                return packet;
            }

            public static ReloadBarrelsPacket CreateInstance()
            {
                return new();
            }

            public string[] AmmoIds;
            public byte[] LocationDescription;
            public bool Reload;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    List<AmmoItemClass> ammo = controller.FindAmmoByIds(AmmoIds);
                    AmmoPackReloadingClass ammoPack = new(ammo);
                    ItemAddress gridItemAddress = null;

                    using GClass1278 eftReader = PacketToEFTReaderAbstractClass.Get(LocationDescription);
                    try
                    {
                        if (LocationDescription.Length > 0)
                        {
                            GClass1785 descriptor = eftReader.ReadPolymorph<GClass1785>();
                            gridItemAddress = player.InventoryController.ToItemAddress(descriptor);
                        }
                    }
                    catch (GException4 exception2)
                    {
                        FikaPlugin.Instance.FikaLogger.LogError(exception2);
                    }

                    if (ammoPack != null)
                    {
                        controller.FastForwardCurrentState();
                        controller.ReloadBarrels(ammoPack, gridItemAddress, null);
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogError($"ReloadBarrelsPacket: final variables were null! Ammo: {ammoPack}, Address: {gridItemAddress}");
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutArray(AmmoIds);
                    writer.PutByteArray(LocationDescription);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    AmmoIds = reader.GetStringArray();
                    LocationDescription = reader.GetByteArray();
                }
            }

            public void Dispose()
            {
                Reload = false;
                AmmoIds = null;
                LocationDescription = null;
            }
        }

        public class FlareShotPacket : IPoolSubPacket
        {
            private FlareShotPacket()
            {

            }

            public static FlareShotPacket FromValue(Vector3 shotPosition, Vector3 shotForward, MongoID ammoTemplateId, bool startOneShotFire)
            {
                FlareShotPacket packet = FirearmSubPacketPool.GetPacket<FlareShotPacket>(EFirearmSubPacketType.FlareShot);
                packet.ShotPosition = shotPosition;
                packet.ShotForward = shotForward;
                packet.AmmoTemplateId = ammoTemplateId;
                packet.StartOneShotFire = startOneShotFire;
                return packet;
            }

            public static FlareShotPacket CreateInstance()
            {
                return new();
            }

            public Vector3 ShotPosition;
            public Vector3 ShotForward;
            public MongoID AmmoTemplateId;
            public bool StartOneShotFire;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    if (StartOneShotFire)
                    {
                        controller.FirearmsAnimator.SetFire(true);

                        if (controller.Weapon is not RevolverItemClass)
                        {
                            controller.FirearmsAnimator.Animator.Play(controller.FirearmsAnimator.FullFireStateName, 1, 0f);
                            controller.Weapon.Repairable.Durability = 0;
                        }
                        else
                        {
                            controller.FirearmsAnimator.Animator.Play(controller.FirearmsAnimator.FullDoubleActionFireStateName, 1, 0f);
                        }
                    }
                    else
                    {
                        AmmoItemClass bulletClass = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
                        controller.InitiateFlare(bulletClass, ShotPosition, ShotForward);
                        bulletClass.IsUsed = true;
                        controller.WeaponManager.MoveAmmoFromChamberToShellPort(bulletClass.IsUsed, 0);
                        controller.FirearmsAnimator.SetFire(false);
                    }
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(StartOneShotFire);
                if (!StartOneShotFire)
                {
                    writer.PutStruct(ShotPosition);
                    writer.PutStruct(ShotForward);
                    writer.PutMongoID(AmmoTemplateId);
                }
            }

            public void Deserialize(NetDataReader reader)
            {
                StartOneShotFire = reader.GetBool();
                if (!StartOneShotFire)
                {
                    ShotPosition = reader.GetStruct<Vector3>();
                    ShotForward = reader.GetStruct<Vector3>();
                    AmmoTemplateId = reader.GetMongoID();
                }
            }

            public void Dispose()
            {
                ShotPosition = default;
                ShotForward = default;
                AmmoTemplateId = default;
                StartOneShotFire = false;
            }
        }

        public class RocketShotPacket : IPoolSubPacket
        {
            private RocketShotPacket()
            {

            }

            public static RocketShotPacket FromValue(Vector3 shotPosition, Vector3 shotForward, MongoID ammoTemplate)
            {
                RocketShotPacket packet = FirearmSubPacketPool.GetPacket<RocketShotPacket>(EFirearmSubPacketType.RocketShot);
                packet.ShotPosition = shotPosition;
                packet.ShotForward = shotForward;
                packet.AmmoTemplateId = ammoTemplate;
                return packet;
            }

            public static RocketShotPacket CreateInstance()
            {
                return new();
            }

            public Vector3 ShotPosition;
            public Vector3 ShotForward;
            public MongoID AmmoTemplateId;

            public void Execute(FikaPlayer player)
            {
                if (player.HandsController is ObservedFirearmController controller)
                {
                    AmmoItemClass rocketClass = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
                    controller.HandleRocketShot(rocketClass, in ShotPosition, in ShotForward);
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.PutStruct(ShotPosition);
                writer.PutStruct(ShotForward);
                writer.PutMongoID(AmmoTemplateId);
            }

            public void Deserialize(NetDataReader reader)
            {
                ShotPosition = reader.GetStruct<Vector3>();
                ShotForward = reader.GetStruct<Vector3>();
                AmmoTemplateId = reader.GetMongoID();
            }

            public void Dispose()
            {
                ShotPosition = default;
                ShotForward = default;
                AmmoTemplateId = default;
            }
        }
    }
}
