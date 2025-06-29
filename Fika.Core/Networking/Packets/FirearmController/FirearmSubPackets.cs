using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.Players;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using static Fika.Core.Networking.SubPacket;

namespace Fika.Core.Networking
{
    public class FirearmSubPackets
    {
        public struct ToggleAimPacket(NetDataReader reader) : ISubPacket
        {
            public int AimingIndex = reader.GetInt();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.SetAim(AimingIndex);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(AimingIndex);
            }
        }

        public struct ChangeFireModePacket(NetDataReader reader) : ISubPacket
        {
            public Weapon.EFireMode FireMode = reader.GetEnum<Weapon.EFireMode>();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.ChangeFireMode(FireMode);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(FireMode);
            }
        }

        public struct ExamineWeaponPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.ExamineWeapon();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct CheckAmmoPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.CheckAmmo();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct ToggleLauncherPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.ToggleLauncher();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct ToggleInventoryPacket(NetDataReader reader) : ISubPacket
        {
            public bool Open = reader.GetBool();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.SetInventoryOpened(Open);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Open);
            }
        }

        public struct FirearmLootPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                player.HandsController.Loot(true);
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct CancelGrenadePacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedGrenadeController grenadeController)
                {
                    grenadeController.vmethod_3();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct CompassChangePacket(NetDataReader reader) : ISubPacket
        {
            public bool Enabled = reader.GetBool();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is ItemHandsController handsController)
                {
                    handsController.CompassState.Value = Enabled;
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Enabled);
            }
        }

        public struct LeftStanceChangePacket(NetDataReader reader) : ISubPacket
        {
            public bool LeftStance = reader.GetBool();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    if (player.MovementContext.LeftStanceEnabled != LeftStance)
                    {
                        controller.ChangeLeftStance();
                    }
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(LeftStance);
            }
        }

        public struct RollCylinderPacket(NetDataReader reader) : ISubPacket
        {
            public bool RollToZeroCamora = reader.GetBool();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller && controller.Weapon is RevolverItemClass)
                {
                    controller.RollCylinder(RollToZeroCamora);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(RollToZeroCamora);
            }
        }

        public struct ReloadBoltActionPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.HandleObservedBoltAction();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct CheckChamberPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.CheckChamber();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct CheckFireModePacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.CheckFireMode();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct UnderbarrelSightingRangeUpPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.UnderbarrelSightingRangeUp();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct UnderbarrelSightingRangeDownPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.UnderbarrelSightingRangeDown();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct ToggleBipodPacket : ISubPacket
        {
            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.ToggleBipod();
                }
            }

            public void Serialize(NetDataWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct ShotInfoPacket(NetDataReader reader) : ISubPacket
        {
            public Vector3 ShotPosition = reader.GetVector3();
            public Vector3 ShotDirection = reader.GetVector3();
            public MongoID AmmoTemplate = reader.GetMongoID();
            public float Overheat = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
            public float LastShotOverheat = reader.GetPackedFloat(0f, 200f, EFloatCompression.High);
            public float LastShotTime = reader.GetFloat();
            public float Durability = reader.GetPackedFloat(0f, 100f, EFloatCompression.High);
            public int ChamberIndex = reader.GetPackedInt(0, 16);
            public bool UnderbarrelShot = reader.GetBool();
            public bool SlideOnOverheatReached = reader.GetBool();
            public EShotType ShotType = (EShotType)reader.GetByte();

            public void Execute(CoopPlayer player)
            {
                if (!player.HealthController.IsAlive)
                {
                    FikaPlugin.Instance.FikaLogger.LogError("ShotInfoPacket::Execute: Player was not alive, can not process!");
                    return;
                }

                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.HandleShotInfoPacket(in this, player.InventoryController);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.PutVector3(ShotPosition);
                writer.PutVector3(ShotDirection);
                writer.PutMongoID(AmmoTemplate);
                writer.PutPackedFloat(Overheat, 0f, 200f, EFloatCompression.High);
                writer.PutPackedFloat(LastShotOverheat, 0f, 200f, EFloatCompression.High);
                writer.Put(LastShotTime);
                writer.PutPackedFloat(Durability, 0f, 100f, EFloatCompression.High);
                writer.PutPackedInt(ChamberIndex, 0, 16);
                writer.Put(UnderbarrelShot);
                writer.Put(SlideOnOverheatReached);
                writer.Put((byte)ShotType);
            }
        }

        public struct KnifePacket(NetDataReader reader) : ISubPacket
        {
            public bool Examine = reader.GetBool();
            public bool Kick = reader.GetBool();
            public bool AltKick = reader.GetBool();
            public bool BreakCombo = reader.GetBool();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedKnifeController knifeController)
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

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Examine);
                writer.Put(Kick);
                writer.Put(AltKick);
                writer.Put(BreakCombo);
            }
        }

        public struct GrenadePacket : ISubPacket
        {
            public Quaternion GrenadeRotation;
            public Vector3 GrenadePosition;
            public Vector3 ThrowForce;
            public EGrenadePacketType Type;
            public bool HasGrenade;
            public bool LowThrow;
            public bool PlantTripwire;
            public bool ChangeToIdle;
            public bool ChangeToPlant;

            public GrenadePacket(NetDataReader reader)
            {
                Type = reader.GetEnum<EGrenadePacketType>();
                HasGrenade = reader.GetBool();
                if (HasGrenade)
                {
                    GrenadeRotation = reader.GetQuaternion();
                    GrenadePosition = reader.GetVector3();
                    ThrowForce = reader.GetVector3();
                    LowThrow = reader.GetBool();
                }
                PlantTripwire = reader.GetBool();
                ChangeToIdle = reader.GetBool();
                ChangeToPlant = reader.GetBool();
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedGrenadeController controller)
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
                else if (player.HandsController is CoopObservedQuickGrenadeController quickGrenadeController)
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

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.PutEnum(Type);
                writer.Put(HasGrenade);
                if (HasGrenade)
                {
                    writer.PutQuaternion(GrenadeRotation);
                    writer.PutVector3(GrenadePosition);
                    writer.PutVector3(ThrowForce);
                    writer.Put(LowThrow);
                }
                writer.Put(PlantTripwire);
                writer.Put(ChangeToIdle);
                writer.Put(ChangeToPlant);
            }
        }

        public struct LightStatesPacket : ISubPacket
        {
            public int Amount;
            public FirearmLightStateStruct[] States;

            public LightStatesPacket(NetDataReader reader)
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

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.SetLightsState(States, true);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
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
        }

        public struct ScopeStatesPacket : ISubPacket
        {
            public int Amount;
            public FirearmScopeStateStruct[] States;

            public ScopeStatesPacket(NetDataReader reader)
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

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    controller.SetScopeMode(States);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
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
        }

        public struct ReloadMagPacket : ISubPacket
        {
            public MongoID MagId;
            public byte[] LocationDescription;
            public bool Reload;

            public ReloadMagPacket(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    MagId = reader.GetMongoID();
                    LocationDescription = reader.GetByteArray();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
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
                            using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(LocationDescription);
                            if (LocationDescription.Length != 0)
                            {
                                GClass1783 descriptor = eftReader.ReadPolymorph<GClass1783>();
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

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutMongoID(MagId);
                    writer.PutByteArray(LocationDescription);
                }
            }
        }

        public struct QuickReloadMagPacket : ISubPacket
        {
            public MongoID MagId;
            public bool Reload;

            public QuickReloadMagPacket(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    MagId = reader.GetMongoID();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
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

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutMongoID(MagId);
                }
            }
        }

        public struct ReloadWithAmmoPacket : ISubPacket
        {
            public bool Reload;
            public EReloadWithAmmoStatus Status;
            public int AmmoLoadedToMag;
            public string[] AmmoIds;

            public ReloadWithAmmoPacket(NetDataReader reader)
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

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
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

            public readonly void Serialize(NetDataWriter writer)
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
        }

        public struct CylinderMagPacket : ISubPacket
        {
            public EReloadWithAmmoStatus Status;
            public int CamoraIndex;
            public int AmmoLoadedToMag;
            public bool Changed;
            public bool HammerClosed;
            public bool Reload;
            public string[] AmmoIds;

            public CylinderMagPacket(NetDataReader reader)
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
                    Status = (EReloadWithAmmoStatus)reader.GetByte();
                    AmmoLoadedToMag = reader.GetInt();
                    AmmoIds = reader.GetStringArray();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
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

            public readonly void Serialize(NetDataWriter writer)
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
                    writer.Put((byte)Status);
                    writer.Put(AmmoLoadedToMag);
                    writer.PutArray(AmmoIds);
                }
            }
        }

        public struct ReloadLauncherPacket : ISubPacket
        {
            public string[] AmmoIds;
            public bool Reload;

            public ReloadLauncherPacket(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    AmmoIds = reader.GetStringArray();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    List<AmmoItemClass> ammo = controller.FindAmmoByIds(AmmoIds);
                    AmmoPackReloadingClass ammoPack = new(ammo);
                    controller.FastForwardCurrentState();
                    controller.ReloadGrenadeLauncher(ammoPack, null);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutArray(AmmoIds);
                }
            }
        }

        public struct ReloadBarrelsPacket : ISubPacket
        {
            public string[] AmmoIds;
            public byte[] LocationDescription;
            public bool Reload;

            public ReloadBarrelsPacket(NetDataReader reader)
            {
                Reload = reader.GetBool();
                if (Reload)
                {
                    AmmoIds = reader.GetStringArray();
                    LocationDescription = reader.GetByteArray();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    List<AmmoItemClass> ammo = controller.FindAmmoByIds(AmmoIds);
                    AmmoPackReloadingClass ammoPack = new(ammo);
                    ItemAddress gridItemAddress = null;

                    using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(LocationDescription);
                    try
                    {
                        if (LocationDescription.Length > 0)
                        {
                            GClass1783 descriptor = eftReader.ReadPolymorph<GClass1783>();
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

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(Reload);
                if (Reload)
                {
                    writer.PutArray(AmmoIds);
                    writer.PutByteArray(LocationDescription);
                }
            }
        }

        public struct FlareShotPacket : ISubPacket
        {
            public Vector3 ShotPosition;
            public Vector3 ShotForward;
            public MongoID AmmoTemplateId;
            public bool StartOneShotFire;

            public FlareShotPacket(NetDataReader reader)
            {
                StartOneShotFire = reader.GetBool();
                if (!StartOneShotFire)
                {
                    ShotPosition = reader.GetVector3();
                    ShotForward = reader.GetVector3();
                    AmmoTemplateId = reader.GetMongoID();
                }
            }

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
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
                        bulletClass = null;
                        controller.FirearmsAnimator.SetFire(false);
                    }
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.Put(StartOneShotFire);
                if (!StartOneShotFire)
                {
                    writer.PutVector3(ShotPosition);
                    writer.PutVector3(ShotForward);
                    writer.PutMongoID(AmmoTemplateId);
                }
            }
        }

        public struct RocketShotPacket(NetDataReader reader) : ISubPacket
        {
            public Vector3 ShotPosition = reader.GetVector3();
            public Vector3 ShotForward = reader.GetVector3();
            public MongoID AmmoTemplateId = reader.GetMongoID();

            public readonly void Execute(CoopPlayer player)
            {
                if (player.HandsController is CoopObservedFirearmController controller)
                {
                    AmmoItemClass rocketClass = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
                    controller.HandleRocketShot(rocketClass, in ShotPosition, in ShotForward);
                }
            }

            public readonly void Serialize(NetDataWriter writer)
            {
                writer.PutVector3(ShotPosition);
                writer.PutVector3(ShotForward);
                writer.PutMongoID(AmmoTemplateId);
            }
        }
    }
}
