// © 2024 Lacyway All Rights Reserved

using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.Vaulting;
using LiteNetLib.Utils;
using System;
using UnityEngine;

// GClass2800

namespace Fika.Core.Networking
{
    public class FikaSerialization
    {
        // Do not use and do not remove

        /*public class AddressUtils
        {
            public static void SerializeGridItemAddressDescriptor(NetDataWriter writer, GClass1528 gridItemAddressDescriptor)
            {
                SerializeLocationInGrid(writer, gridItemAddressDescriptor.LocationInGrid);
            }

            public static GClass1528 DeserializeGridItemAddressDescriptor(NetDataReader reader)
            {
                return new GClass1528()
                {
                    LocationInGrid = DeserializeLocationInGrid(reader),
                    Container = DeserializeContainerDescriptor(reader)
                };
            }

            public static void SerializeContainerDescriptor(NetDataWriter writer, GClass1530 containerDescriptor)
            {
                writer.Put(containerDescriptor.ParentId);
                writer.Put(containerDescriptor.ContainerId);
            }

            public static GClass1530 DeserializeContainerDescriptor(NetDataReader reader)
            {
                return new GClass1530()
                {
                    ParentId = reader.GetString(),
                    ContainerId = reader.GetString(),
                };
            }

            public static void SerializeItemInGridDescriptor(NetDataWriter writer, GClass1495 itemInGridDescriptor)
            {
                GClass1081 polyWriter = new();
                SerializeLocationInGrid(writer, itemInGridDescriptor.Location);
                SerializeItemDescriptor(polyWriter, itemInGridDescriptor.Item);
                writer.Put(polyWriter.ToArray());
            }

            public static GClass1495 DeserializeItemInGridDescriptor(NetDataReader reader)
            {
                GClass1076 polyReader = new(reader.RawData);
                return new GClass1495()
                {
                    Location = DeserializeLocationInGrid(reader),
                    Item = DeserializeItemDescriptor(polyReader)
                };
            }

            public static void SerializeLocationInGrid(NetDataWriter writer, LocationInGrid locationInGrid)
            {
                writer.Put(locationInGrid.x);
                writer.Put(locationInGrid.y);
                writer.Put((int)locationInGrid.r);
                writer.Put(locationInGrid.isSearched);
            }

            public static LocationInGrid DeserializeLocationInGrid(NetDataReader reader)
            {
                return new LocationInGrid()
                {
                    x = reader.GetInt(),
                    y = reader.GetInt(),
                    r = (ItemRotation)reader.GetInt(),
                    isSearched = reader.GetBool()
                };
            }

            public static void SerializeItemDescriptor(GClass1081 writer, GClass1499 itemDescriptor)
            {
                writer.WriteString(itemDescriptor.Id);
                writer.WriteString(itemDescriptor.TemplateId);
                writer.WriteInt(itemDescriptor.StackCount);
                writer.WriteBool(itemDescriptor.SpawnedInSession);
                writer.WriteByte(itemDescriptor.ActiveCamora);
                writer.WriteBool(itemDescriptor.IsUnderBarrelDeviceActive);
                for (int i = 0; i < itemDescriptor.Components.Count; i++)
                {
                    writer.WritePolymorph(itemDescriptor.Components[i]);
                }
                writer.WriteInt(itemDescriptor.Slots.Count);
                for (int j = 0; j < itemDescriptor.Slots.Count; j++)
                {
                    writer.WriteEFTSlotDescriptor(itemDescriptor.Slots[j]);
                }
                writer.WriteInt(itemDescriptor.ShellsInWeapon.Count);
                for (int k = 0; k < itemDescriptor.ShellsInWeapon.Count; k++)
                {
                    writer.WriteEFTShellTemplateDescriptor(itemDescriptor.ShellsInWeapon[k]);
                }
                writer.WriteInt(itemDescriptor.ShellsInUnderbarrelWeapon.Count);
                for (int l = 0; l < itemDescriptor.ShellsInUnderbarrelWeapon.Count; l++)
                {
                    writer.WriteEFTShellTemplateDescriptor(itemDescriptor.ShellsInUnderbarrelWeapon[l]);
                }
                writer.WriteInt(itemDescriptor.Grids.Count);
                for (int m = 0; m < itemDescriptor.Grids.Count; m++)
                {
                    writer.WriteEFTGridDescriptor(itemDescriptor.Grids[m]);
                }
                writer.WriteInt(itemDescriptor.StackSlots.Count);
                for (int n = 0; n < itemDescriptor.StackSlots.Count; n++)
                {
                    writer.WriteEFTStackSlotDescriptor(itemDescriptor.StackSlots[n]);
                }
                writer.WriteInt(itemDescriptor.Malfunction.Count);
                for (int num = 0; num < itemDescriptor.Malfunction.Count; num++)
                {
                    writer.WriteEFTMalfunctionDescriptor(itemDescriptor.Malfunction[num]);
                }
            }

            public static GClass1499 DeserializeItemDescriptor(GClass1076 reader)
            {
                GClass1499 itemDescriptor = new();
                itemDescriptor.Id = reader.ReadString();
                itemDescriptor.TemplateId = reader.ReadString();
                itemDescriptor.StackCount = reader.ReadInt();
                itemDescriptor.SpawnedInSession = reader.ReadBool();
                itemDescriptor.ActiveCamora = reader.ReadByte();
                itemDescriptor.IsUnderBarrelDeviceActive = reader.ReadBool();
                int num = reader.ReadInt();
                itemDescriptor.Components = new List<GClass1500>(num);
                for (int i = 0; i < num; i++)
                {
                    itemDescriptor.Components.Add(reader.ReadPolymorph<GClass1500>());
                }
                int num2 = reader.ReadInt();
                itemDescriptor.Slots = new List<GClass1492>(num2);
                for (int j = 0; j < num2; j++)
                {
                    itemDescriptor.Slots.Add(reader.ReadEFTSlotDescriptor());
                }
                int num3 = reader.ReadInt();
                itemDescriptor.ShellsInWeapon = new List<GClass1493>(num3);
                for (int k = 0; k < num3; k++)
                {
                    itemDescriptor.ShellsInWeapon.Add(reader.ReadEFTShellTemplateDescriptor());
                }
                int num4 = reader.ReadInt();
                itemDescriptor.ShellsInUnderbarrelWeapon = new List<GClass1493>(num4);
                for (int l = 0; l < num4; l++)
                {
                    itemDescriptor.ShellsInUnderbarrelWeapon.Add(reader.ReadEFTShellTemplateDescriptor());
                }
                int num5 = reader.ReadInt();
                itemDescriptor.Grids = new List<GClass1496>(num5);
                for (int m = 0; m < num5; m++)
                {
                    itemDescriptor.Grids.Add(reader.ReadEFTGridDescriptor());
                }
                int num6 = reader.ReadInt();
                itemDescriptor.StackSlots = new List<GClass1497>(num6);
                for (int n = 0; n < num6; n++)
                {
                    itemDescriptor.StackSlots.Add(reader.ReadEFTStackSlotDescriptor());
                }
                int num7 = reader.ReadInt();
                itemDescriptor.Malfunction = new List<GClass1494>(num7);
                for (int num8 = 0; num8 < num7; num8++)
                {
                    itemDescriptor.Malfunction.Add(reader.ReadEFTMalfunctionDescriptor());
                }
                return itemDescriptor;
            }

            public static void SerializeSlotItemAddressDescriptor(NetDataWriter writer, GClass1526 slotItemAddressDescriptor)
            {
                SerializeContainerDescriptor(writer, slotItemAddressDescriptor.Container);
            }

            public static GClass1526 DeserializeSlotItemAddressDescriptor(NetDataReader reader)
            {
                return new GClass1526()
                {
                    Container = DeserializeContainerDescriptor(reader)
                };
            }

            public static void SerializeStackSlotItemAddressDescriptor(NetDataWriter writer, GClass1527 stackSlotItemAddressDescriptor)
            {
                SerializeContainerDescriptor(writer, stackSlotItemAddressDescriptor.Container);
            }

            public static GClass1527 DeserializeStackSlotItemAddressDescriptor(NetDataReader reader)
            {
                return new GClass1527()
                {
                    Container = DeserializeContainerDescriptor(reader)
                };
            }
        }*/

        /*public class PlayerUtils
        {
            public static void SerializeProfile(NetDataWriter writer, Profile profile)
            {
                byte[] profileBytes = SimpleZlib.CompressToBytes(profile.ToJson(), 9, null);
                writer.Put(profileBytes);
                Profile profile2 = SimpleZlib.Decompress(profileBytes, null).ParseJsonTo<Profile>();
            }

            public static Profile DeserializeProfile(byte[] profileBytes)
            {
                Profile profile = SimpleZlib.Decompress(profileBytes, null).ParseJsonTo<Profile>();
                return profile;
            }

            public static void SerializeInventory(NetDataWriter writer, Inventory inventory)
            {
                GClass1489 inventoryDescriptor = new GClass1489()
                {
                    Equipment = GClass1524.SerializeItem(inventory.Equipment),
                    Stash = GClass1524.SerializeItem(inventory.Stash),
                    QuestRaidItems = GClass1524.SerializeItem(inventory.QuestRaidItems),
                    QuestStashItems = GClass1524.SerializeItem(inventory.QuestStashItems),
                    SortingTable = GClass1524.SerializeItem(inventory.SortingTable),
                    FastAccess = GClass1524.SerializeFastAccess(inventory.FastAccess),
                    DiscardLimits = GClass1524.SerializeDiscardLimits(inventory.DiscardLimits)
                };
                GClass1081 polyWriter = new();
                polyWriter.WriteEFTInventoryDescriptor(inventoryDescriptor);
                writer.Put(polyWriter.ToArray());
            }

            public static Inventory DeserializeInventory(byte[] inventoryBytes)
            {
                using MemoryStream memoryStream = new(inventoryBytes);
                BinaryReader polyReader = new(memoryStream);
                Inventory inventory = GClass1524.DeserializeInventory(Singleton<ItemFactory>.Instance, polyReader.ReadEFTInventoryDescriptor());
                return inventory;
            }
        }*/

        public struct PlayerInfoPacket()
        {
            public Profile Profile;

            public static void Serialize(NetDataWriter writer, PlayerInfoPacket packet)
            {
                byte[] profileBytes = SimpleZlib.CompressToBytes(packet.Profile.ToJson(), 4, null);
                writer.PutByteArray(profileBytes);
            }

            public static PlayerInfoPacket Deserialize(NetDataReader reader)
            {
                byte[] profileBytes = reader.GetByteArray();
                PlayerInfoPacket packet = new()
                {
                    Profile = SimpleZlib.Decompress(profileBytes, null).ParseJsonTo<Profile>()
                };
                return packet;
            }
        }

        public struct LightStatesPacket
        {
            public int Amount;
            public GStruct163[] LightStates;
            public static LightStatesPacket Deserialize(NetDataReader reader)
            {
                LightStatesPacket packet = new();
                packet.Amount = reader.GetInt();
                if (packet.Amount > 0)
                {
                    packet.LightStates = new GStruct163[packet.Amount];
                    for (int i = 0; i < packet.Amount; i++)
                    {
                        packet.LightStates[i] = new()
                        {
                            Id = reader.GetString(),
                            IsActive = reader.GetBool(),
                            LightMode = reader.GetInt()
                        };
                    }
                }
                return packet;
            }

            public static void Serialize(NetDataWriter writer, LightStatesPacket packet)
            {
                writer.Put(packet.Amount);
                if (packet.Amount > 0)
                {
                    for (int i = 0; i < packet.Amount; i++)
                    {
                        writer.Put(packet.LightStates[i].Id);
                        writer.Put(packet.LightStates[i].IsActive);
                        writer.Put(packet.LightStates[i].LightMode);
                    }
                }
            }
        }

        public struct HeadLightsPacket
        {
            public int Amount;
            public GStruct163[] LightStates;
            public static HeadLightsPacket Deserialize(NetDataReader reader)
            {
                HeadLightsPacket packet = new();
                packet.Amount = reader.GetInt();
                if (packet.Amount > 0)
                {
                    packet.LightStates = new GStruct163[packet.Amount];
                    for (int i = 0; i < packet.Amount; i++)
                    {
                        packet.LightStates[i] = new()
                        {
                            Id = reader.GetString(),
                            IsActive = reader.GetBool(),
                            LightMode = reader.GetInt()
                        };
                    }
                }
                return packet;
            }

            public static void Serialize(NetDataWriter writer, HeadLightsPacket packet)
            {
                writer.Put(packet.Amount);
                if (packet.Amount > 0)
                {
                    for (int i = 0; i < packet.Amount; i++)
                    {
                        writer.Put(packet.LightStates[i].Id);
                        writer.Put(packet.LightStates[i].IsActive);
                        writer.Put(packet.LightStates[i].LightMode);
                    }
                }
            }
        }

        public struct ScopeStatesPacket
        {
            public int Amount;
            public GStruct164[] GStruct164;
            public static ScopeStatesPacket Deserialize(NetDataReader reader)
            {
                ScopeStatesPacket packet = new();
                packet.Amount = reader.GetInt();
                if (packet.Amount > 0)
                {
                    packet.GStruct164 = new GStruct164[packet.Amount];
                    for (int i = 0; i < packet.Amount; i++)
                    {
                        packet.GStruct164[i] = new()
                        {
                            Id = reader.GetString(),
                            ScopeMode = reader.GetInt(),
                            ScopeIndexInsideSight = reader.GetInt(),
                            ScopeCalibrationIndex = reader.GetInt()
                        };
                    }
                }
                return packet;
            }

            public static void Serialize(NetDataWriter writer, ScopeStatesPacket packet)
            {
                writer.Put(packet.Amount);
                if (packet.Amount > 0)
                {
                    for (int i = 0; i < packet.Amount; i++)
                    {
                        writer.Put(packet.GStruct164[i].Id);
                        writer.Put(packet.GStruct164[i].ScopeMode);
                        writer.Put(packet.GStruct164[i].ScopeIndexInsideSight);
                        writer.Put(packet.GStruct164[i].ScopeCalibrationIndex);
                    }
                }
            }
        }

        public struct ReloadMagPacket
        {
            public bool Reload;
            public string MagId;
            public byte[] LocationDescription;

            public static ReloadMagPacket Deserialize(NetDataReader reader)
            {
                ReloadMagPacket packet = new()
                {
                    Reload = reader.GetBool()
                };
                if (packet.Reload)
                {
                    packet.MagId = reader.GetString();
                    packet.LocationDescription = reader.GetByteArray();
                }
                return packet;
            }
            public static void Serialize(NetDataWriter writer, ReloadMagPacket packet)
            {
                writer.Put(packet.Reload);
                if (packet.Reload)
                {
                    writer.Put(packet.MagId);
                    writer.PutByteArray(packet.LocationDescription);
                }
            }
        }

        public struct QuickReloadMagPacket
        {
            public bool Reload;
            public string MagId;

            public static QuickReloadMagPacket Deserialize(NetDataReader reader)
            {
                QuickReloadMagPacket packet = new()
                {
                    Reload = reader.GetBool()
                };
                if (packet.Reload)
                {
                    packet.MagId = reader.GetString();
                }
                return packet;
            }

            public static void Serialize(NetDataWriter writer, QuickReloadMagPacket packet)
            {
                writer.Put(packet.Reload);
                if (packet.Reload)
                {
                    writer.Put(packet.MagId);
                }
            }
        }

        public struct ReloadWithAmmoPacket
        {
            public bool Reload;
            public EReloadWithAmmoStatus Status;
            public int AmmoLoadedToMag;
            public string[] AmmoIds;

            public enum EReloadWithAmmoStatus
            {
                None = 0,
                StartReload,
                EndReload,
                AbortReload
            }

            public static ReloadWithAmmoPacket Deserialize(NetDataReader reader)
            {
                ReloadWithAmmoPacket packet = new()
                {
                    Reload = reader.GetBool()
                };
                if (packet.Reload)
                {
                    packet.Status = (EReloadWithAmmoStatus)reader.GetInt();
                    if (packet.Status == EReloadWithAmmoStatus.StartReload)
                    {
                        packet.AmmoIds = reader.GetStringArray();
                    }
                    if (packet.Status is EReloadWithAmmoStatus.EndReload or EReloadWithAmmoStatus.AbortReload)
                    {
                        packet.AmmoLoadedToMag = reader.GetInt();
                    }
                }
                return packet;
            }

            public static void Serialize(NetDataWriter writer, ReloadWithAmmoPacket packet)
            {
                writer.Put(packet.Reload);
                if (packet.Reload)
                {
                    writer.Put((int)packet.Status);
                    if (packet.Status == EReloadWithAmmoStatus.StartReload)
                    {
                        writer.PutArray(packet.AmmoIds);
                    }
                    if (packet.AmmoLoadedToMag > 0)
                    {
                        writer.Put(packet.AmmoLoadedToMag);
                    }
                }
            }
        }

        public struct CylinderMagPacket
        {
            public bool Changed;
            public int CamoraIndex;
            public bool HammerClosed;

            public static CylinderMagPacket Deserialize(NetDataReader reader)
            {
                CylinderMagPacket packet = new CylinderMagPacket();
                packet.Changed = reader.GetBool();
                if (packet.Changed)
                {
                    packet.CamoraIndex = reader.GetInt();
                    packet.HammerClosed = reader.GetBool();
                }
                return packet;
            }

            public static void Serialize(NetDataWriter writer, CylinderMagPacket packet)
            {
                writer.Put(packet.Changed);
                if (packet.Changed)
                {
                    writer.Put(packet.CamoraIndex);
                    writer.Put(packet.HammerClosed);
                }
            }
        }

        public struct ReloadLauncherPacket
        {
            public bool Reload;
            public string[] AmmoIds;

            public static ReloadLauncherPacket Deserialize(NetDataReader reader)
            {
                ReloadLauncherPacket packet = new();
                packet.Reload = reader.GetBool();
                if (packet.Reload)
                {
                    packet.AmmoIds = reader.GetStringArray();
                }
                return packet;
            }

            public static void Serialize(NetDataWriter writer, ReloadLauncherPacket packet)
            {
                writer.Put(packet.Reload);
                if (packet.Reload)
                {
                    writer.PutArray(packet.AmmoIds);
                }
            }
        }

        public struct ReloadBarrelsPacket
        {
            public bool Reload;
            public string[] AmmoIds;
            public byte[] LocationDescription;

            public static ReloadBarrelsPacket Deserialize(NetDataReader reader)
            {
                ReloadBarrelsPacket packet = new()
                {
                    Reload = reader.GetBool()
                };
                if (packet.Reload)
                {
                    packet.AmmoIds = reader.GetStringArray();
                    packet.LocationDescription = reader.GetByteArray();
                }
                return packet;
            }
            public static void Serialize(NetDataWriter writer, ReloadBarrelsPacket packet)
            {
                writer.Put(packet.Reload);
                if (packet.Reload)
                {
                    writer.PutArray(packet.AmmoIds);
                    writer.PutByteArray(packet.LocationDescription);
                }
            }
        }

        public struct GrenadePacket()
        {
            public GrenadePacketType PacketType;
            public enum GrenadePacketType
            {
                None,
                ExamineWeapon,
                HighThrow,
                LowThrow,
                PullRingForHighThrow,
                PullRingForLowThrow
            };
            public bool HasGrenade = false;
            public Quaternion GrenadeRotation;
            public Vector3 GrenadePosition;
            public Vector3 ThrowForce;
            public bool LowThrow;

            public static GrenadePacket Deserialize(NetDataReader reader)
            {
                GrenadePacket packet = new()
                {
                    PacketType = (GrenadePacketType)reader.GetInt(),
                    HasGrenade = reader.GetBool()
                };
                if (packet.HasGrenade)
                {
                    packet.GrenadeRotation = reader.GetQuaternion();
                    packet.GrenadePosition = reader.GetVector3();
                    packet.ThrowForce = reader.GetVector3();
                    packet.LowThrow = reader.GetBool();
                }
                return packet;
            }
            public static void Serialize(NetDataWriter writer, GrenadePacket packet)
            {
                writer.Put((int)packet.PacketType);
                writer.Put(packet.HasGrenade);
                if (packet.HasGrenade)
                {
                    writer.Put(packet.GrenadeRotation);
                    writer.Put(packet.GrenadePosition);
                    writer.Put(packet.ThrowForce);
                    writer.Put(packet.LowThrow);
                }
            }
        }

        public struct ApplyShotPacket()
        {
            public EDamageType DamageType;
            public float Damage;
            public EBodyPart BodyPartType;
            public EBodyPartColliderType ColliderType;
            public EArmorPlateCollider ArmorPlateCollider;
            public float Absorbed;
            public Vector3 Direction = Vector3.zero;
            public Vector3 Point = Vector3.zero;
            public Vector3 HitNormal = Vector3.zero;
            public float PenetrationPower = 0f;
            public string BlockedBy;
            public string DeflectedBy;
            public string SourceId;
            public string AmmoId;
            public int FragmentIndex;
            public float ArmorDamage = 0f;
            public string ProfileId;

            public static ApplyShotPacket Deserialize(NetDataReader reader)
            {
                ApplyShotPacket packet = new()
                {
                    DamageType = (EDamageType)reader.GetInt(),
                    Damage = reader.GetFloat(),
                    BodyPartType = (EBodyPart)reader.GetInt(),
                    ColliderType = (EBodyPartColliderType)reader.GetInt(),
                    ArmorPlateCollider = (EArmorPlateCollider)reader.GetInt(),
                    Absorbed = reader.GetFloat(),
                    Direction = reader.GetVector3(),
                    Point = reader.GetVector3(),
                    HitNormal = reader.GetVector3(),
                    PenetrationPower = reader.GetFloat(),
                    BlockedBy = reader.GetString(),
                    DeflectedBy = reader.GetString(),
                    SourceId = reader.GetString(),
                    AmmoId = reader.GetString(),
                    FragmentIndex = reader.GetInt(),
                    ArmorDamage = reader.GetFloat(),
                    ProfileId = reader.GetString()
                };
                return packet;
            }
            public static void Serialize(NetDataWriter writer, ApplyShotPacket packet)
            {
                writer.Put((int)packet.DamageType);
                writer.Put(packet.Damage);
                writer.Put((int)packet.BodyPartType);
                writer.Put((int)packet.ColliderType);
                writer.Put((int)packet.ArmorPlateCollider);
                writer.Put(packet.Absorbed);
                writer.Put(packet.Direction);
                writer.Put(packet.Point);
                writer.Put(packet.HitNormal);
                writer.Put(packet.PenetrationPower);
                writer.Put(packet.BlockedBy);
                writer.Put(packet.DeflectedBy);
                writer.Put(packet.SourceId);
                writer.Put(packet.AmmoId);
                writer.Put(packet.FragmentIndex);
                writer.Put(packet.ArmorDamage);
                writer.Put(packet.ProfileId);
            }
        }

        public struct ItemControllerExecutePacket
        {
            public uint CallbackId;
            public byte[] OperationBytes;
            public static ItemControllerExecutePacket Deserialize(NetDataReader reader)
            {
                ItemControllerExecutePacket packet = new()
                {
                    CallbackId = reader.GetUInt(),
                    OperationBytes = reader.GetByteArray(),
                };
                return packet;
            }
            public static void Serialize(NetDataWriter writer, ItemControllerExecutePacket packet)
            {
                writer.Put(packet.CallbackId);
                writer.PutByteArray(packet.OperationBytes);
            }
        }

        public struct WorldInteractionPacket
        {
            public string InteractiveId;
            public EInteractionType InteractionType;
            public EInteractionStage InteractionStage;
            public string ItemId;

            public static WorldInteractionPacket Deserialize(NetDataReader reader)
            {
                WorldInteractionPacket packet = new()
                {
                    InteractiveId = reader.GetString(),
                    InteractionType = (EInteractionType)reader.GetByte(),
                    InteractionStage = (EInteractionStage)reader.GetByte(),
                };
                if (packet.InteractionType == EInteractionType.Unlock)
                    packet.ItemId = reader.GetString();

                return packet;
            }

            public static void Serialize(NetDataWriter writer, WorldInteractionPacket packet)
            {
                writer.Put(packet.InteractiveId);
                writer.Put((byte)packet.InteractionType);
                writer.Put((byte)packet.InteractionStage);
                if (packet.InteractionType == EInteractionType.Unlock)
                    writer.Put(packet.ItemId);
            }
        }

        public struct ContainerInteractionPacket
        {
            public string InteractiveId;
            public EInteractionType InteractionType;

            public static ContainerInteractionPacket Deserialize(NetDataReader reader)
            {
                ContainerInteractionPacket packet = new();
                packet.InteractiveId = reader.GetString();
                packet.InteractionType = (EInteractionType)reader.GetInt();
                return packet;
            }
            public static void Serialize(NetDataWriter writer, ContainerInteractionPacket packet)
            {
                writer.Put(packet.InteractiveId);
                writer.Put((int)packet.InteractionType);
            }
        }

        public struct ProceedPacket()
        {
            public EProceedType ProceedType;
            public string ItemId = "";
            public string ItemTemplateId = "";
            public float Amount = 0f;
            public int AnimationVariant = 0;
            public bool Scheduled = false;
            public EBodyPart BodyPart = EBodyPart.Common;

            public static ProceedPacket Deserialize(NetDataReader reader)
            {
                return new ProceedPacket
                {
                    ProceedType = (EProceedType)reader.GetInt(),
                    ItemId = reader.GetString(),
                    ItemTemplateId = reader.GetString(),
                    Amount = reader.GetFloat(),
                    AnimationVariant = reader.GetInt(),
                    Scheduled = reader.GetBool(),
                    BodyPart = (EBodyPart)reader.GetInt()
                };
            }
            public static void Serialize(NetDataWriter writer, ProceedPacket packet)
            {
                writer.Put((int)packet.ProceedType);
                writer.Put(packet.ItemId);
                writer.Put(packet.ItemTemplateId);
                writer.Put(packet.Amount);
                writer.Put(packet.AnimationVariant);
                writer.Put(packet.Scheduled);
                writer.Put((int)packet.BodyPart);
            }

        }

        public struct DropPacket
        {
            public bool FastDrop;
            public bool HasItemId;
            public string ItemId;

            public static DropPacket Deserialize(NetDataReader reader)
            {
                DropPacket packet = new()
                {
                    FastDrop = reader.GetBool(),
                    HasItemId = reader.GetBool()
                };
                if (packet.HasItemId)
                    packet.ItemId = reader.GetString();

                return packet;
            }
            public static void Serialize(NetDataWriter writer, DropPacket packet)
            {
                writer.Put(packet.FastDrop);
                writer.Put(packet.HasItemId);
                if (packet.HasItemId)
                    writer.Put(packet.ItemId);
            }
        }

        public struct StationaryPacket
        {
            public EStationaryCommand Command;
            public string Id;
            public enum EStationaryCommand : byte
            {
                Occupy,
                Leave,
                Denied
            }

            public static StationaryPacket Deserialize(NetDataReader reader)
            {
                StationaryPacket packet = new()
                {
                    Command = (EStationaryCommand)reader.GetByte()
                };

                if (packet.Command == EStationaryCommand.Occupy)
                    packet.Id = reader.GetString();

                return packet;
            }
            public static void Serialize(NetDataWriter writer, StationaryPacket packet)
            {
                writer.Put((byte)packet.Command);
                if (packet.Command == EStationaryCommand.Occupy && !string.IsNullOrEmpty(packet.Id))
                    writer.Put(packet.Id);
            }
        }

        public struct KnifePacket()
        {
            public bool Examine = false;
            public bool Kick = false;
            public bool AltKick = false;
            public bool BreakCombo = false;
            public static KnifePacket Deserialize(NetDataReader reader)
            {
                return new KnifePacket()
                {
                    Examine = reader.GetBool(),
                    Kick = reader.GetBool(),
                    AltKick = reader.GetBool(),
                    BreakCombo = reader.GetBool()
                };
            }
            public static void Serialize(NetDataWriter writer, KnifePacket packet)
            {
                writer.Put(packet.Examine);
                writer.Put(packet.Kick);
                writer.Put(packet.AltKick);
                writer.Put(packet.BreakCombo);
            }
        }

        public struct ShotInfoPacket()
        {

            public bool IsPrimaryActive = true;
            public EShotType ShotType = EShotType.Unknown;
            public int AmmoAfterShot = 0;
            public Vector3 ShotPosition = Vector3.zero;
            public Vector3 ShotDirection = Vector3.zero;
            public Vector3 FireportPosition = Vector3.zero;
            public int ChamberIndex = 0;
            public float Overheat = 0f;
            public bool UnderbarrelShot = false;
            public string AmmoTemplate;
            public float LastShotOverheat;
            public float LastShotTime;
            public bool SlideOnOverheatReached;

            public static ShotInfoPacket Deserialize(NetDataReader reader)
            {
                ShotInfoPacket packet = new()
                {
                    IsPrimaryActive = reader.GetBool(),
                    ShotType = (EShotType)reader.GetInt(),
                    AmmoAfterShot = reader.GetInt(),
                    ShotPosition = reader.GetVector3(),
                    ShotDirection = reader.GetVector3(),
                    FireportPosition = reader.GetVector3(),
                    ChamberIndex = reader.GetInt(),
                    Overheat = reader.GetFloat(),
                    UnderbarrelShot = reader.GetBool(),
                    AmmoTemplate = reader.GetString(),
                    LastShotOverheat = reader.GetFloat(),
                    LastShotTime = reader.GetFloat(),
                    SlideOnOverheatReached = reader.GetBool()
                };

                return packet;
            }
            public static void Serialize(NetDataWriter writer, ShotInfoPacket packet)
            {
                writer.Put(packet.IsPrimaryActive);
                writer.Put((int)packet.ShotType);
                writer.Put(packet.AmmoAfterShot);
                writer.Put(packet.ShotPosition);
                writer.Put(packet.ShotDirection);
                writer.Put(packet.FireportPosition);
                writer.Put(packet.ChamberIndex);
                writer.Put(packet.Overheat);
                writer.Put(packet.UnderbarrelShot);
                writer.Put(packet.AmmoTemplate);
                writer.Put(packet.LastShotOverheat);
                writer.Put(packet.LastShotTime);
                writer.Put(packet.SlideOnOverheatReached);
            }
        }

        public struct SearchPacket
        {
            public bool IsStop;
            public string ItemId;
            public int OperationId;

            public static SearchPacket Deserialize(NetDataReader reader)
            {
                SearchPacket packet = new SearchPacket()
                {
                    IsStop = reader.GetBool(),
                    ItemId = reader.GetString(),
                    OperationId = reader.GetInt()
                };
                return packet;
            }

            public static void Serialize(NetDataWriter writer, SearchPacket packet)
            {
                writer.Put(packet.IsStop);
                writer.Put(packet.ItemId);
                writer.Put(packet.OperationId);
            }
        }

        public struct WeatherClassPacket
        {
            public float AtmospherePressure;
            public float Cloudness;
            public float GlobalFogDensity;
            public float GlobalFogHeight;
            public float LyingWater;
            public Vector2 MainWindDirection;
            public Vector2 MainWindPosition;
            public float Rain;
            public float RainRandomness;
            public float ScaterringFogDensity;
            public float ScaterringFogHeight;
            public float Temperature;
            public long Time;
            public Vector2 TopWindDirection;
            public Vector2 TopWindPosition;
            public float Turbulence;
            public float Wind;
            public int WindDirection;

            public static WeatherClassPacket Deserialize(NetDataReader reader)
            {
                return new WeatherClassPacket()
                {
                    AtmospherePressure = reader.GetFloat(),
                    Cloudness = reader.GetFloat(),
                    GlobalFogDensity = reader.GetFloat(),
                    GlobalFogHeight = reader.GetFloat(),
                    LyingWater = reader.GetFloat(),
                    MainWindDirection = reader.GetVector2(),
                    MainWindPosition = reader.GetVector2(),
                    Rain = reader.GetFloat(),
                    RainRandomness = reader.GetFloat(),
                    ScaterringFogDensity = reader.GetFloat(),
                    ScaterringFogHeight = reader.GetFloat(),
                    Temperature = reader.GetFloat(),
                    Time = reader.GetLong(),
                    TopWindDirection = reader.GetVector2(),
                    TopWindPosition = reader.GetVector2(),
                    Turbulence = reader.GetFloat(),
                    Wind = reader.GetFloat(),
                    WindDirection = reader.GetInt()
                };
            }

            public static void Serialize(NetDataWriter writer, WeatherClass weatherClass)
            {
                writer.Put(weatherClass.AtmospherePressure);
                writer.Put(weatherClass.Cloudness);
                writer.Put(weatherClass.GlobalFogDensity);
                writer.Put(weatherClass.GlobalFogHeight);
                writer.Put(weatherClass.LyingWater);
                writer.Put(weatherClass.MainWindDirection);
                writer.Put(weatherClass.MainWindPosition);
                writer.Put(weatherClass.Rain);
                writer.Put(weatherClass.RainRandomness);
                writer.Put(weatherClass.ScaterringFogDensity);
                writer.Put(weatherClass.ScaterringFogHeight);
                writer.Put(weatherClass.Temperature);
                writer.Put(weatherClass.Time);
                writer.Put(weatherClass.TopWindDirection);
                writer.Put(weatherClass.TopWindPosition);
                writer.Put(weatherClass.Turbulence);
                writer.Put(weatherClass.Wind);
                writer.Put(weatherClass.WindDirection);
            }
        }

        public struct FlareShotPacket
        {
            public Vector3 ShotPosition;
            public Vector3 ShotForward;
            public string AmmoTemplateId;

            public static FlareShotPacket Deserialize(NetDataReader reader)
            {
                return new FlareShotPacket()
                {
                    ShotPosition = reader.GetVector3(),
                    ShotForward = reader.GetVector3(),
                    AmmoTemplateId = reader.GetString()
                };
            }

            public static void Serialize(NetDataWriter writer, FlareShotPacket packet)
            {
                writer.Put(packet.ShotPosition);
                writer.Put(packet.ShotForward);
                writer.Put(packet.AmmoTemplateId);
            }
        }

        public struct VaultPacket
        {
            public EVaultingStrategy VaultingStrategy;
            public Vector3 VaultingPoint;
            public float VaultingHeight;
            public float VaultingLength;
            public float VaultingSpeed;
            public float BehindObstacleHeight;
            public float AbsoluteForwardVelocity;

            public static VaultPacket Deserialize(NetDataReader reader)
            {
                return new VaultPacket()
                {
                    VaultingStrategy = (EVaultingStrategy)reader.GetInt(),
                    VaultingPoint = reader.GetVector3(),
                    VaultingHeight = reader.GetFloat(),
                    VaultingLength = reader.GetFloat(),
                    VaultingSpeed = reader.GetFloat(),
                    BehindObstacleHeight = reader.GetFloat(),
                    AbsoluteForwardVelocity = reader.GetFloat()
                };
            }

            public static void Serialize(NetDataWriter writer, VaultPacket packet)
            {
                writer.Put((int)packet.VaultingStrategy);
                writer.Put(packet.VaultingPoint);
                writer.Put(packet.VaultingHeight);
                writer.Put(packet.VaultingLength);
                writer.Put(packet.VaultingSpeed);
                writer.Put(packet.BehindObstacleHeight);
                writer.Put(packet.AbsoluteForwardVelocity);
            }
        }

        public class BTRDataPacketUtils
        {
            public static BTRDataPacket Deserialize(NetDataReader reader)
            {
                return new()
                {
                    position = reader.GetVector3(),
                    BtrBotId = reader.GetInt(),
                    MoveSpeed = reader.GetFloat(),
                    moveDirection = reader.GetByte(),
                    timeToEndPause = reader.GetFloat(),
                    currentSpeed = reader.GetFloat(),
                    RightSlot1State = reader.GetByte(),
                    RightSlot0State = reader.GetByte(),
                    RightSideState = reader.GetByte(),
                    LeftSlot1State = reader.GetByte(),
                    LeftSlot0State = reader.GetByte(),
                    LeftSideState = reader.GetByte(),
                    RouteState = reader.GetByte(),
                    State = reader.GetByte(),
                    gunsBlockRotation = reader.GetQuaternion(),
                    turretRotation = reader.GetQuaternion(),
                    rotation = reader.GetQuaternion()
                };
            }

            public static void Serialize(NetDataWriter writer, BTRDataPacket packet)
            {
                writer.Put(packet.position);
                writer.Put(packet.BtrBotId);
                writer.Put(packet.MoveSpeed);
                writer.Put(packet.moveDirection);
                writer.Put(packet.timeToEndPause);
                writer.Put(packet.currentSpeed);
                writer.Put(packet.RightSlot1State);
                writer.Put(packet.RightSlot0State);
                writer.Put(packet.RightSideState);
                writer.Put(packet.LeftSlot1State);
                writer.Put(packet.LeftSlot0State);
                writer.Put(packet.LeftSideState);
                writer.Put(packet.RouteState);
                writer.Put(packet.State);
                writer.Put(packet.gunsBlockRotation);
                writer.Put(packet.turretRotation);
                writer.Put(packet.rotation);
            }
        }

        public struct RagdollPacket
        {
            public EBodyPartColliderType BodyPartColliderType;
            public Vector3 Direction;
            public Vector3 Point;
            public float Force;
            public Vector3 OverallVelocity;

            public static RagdollPacket Deserialize(NetDataReader reader)
            {
                return new RagdollPacket()
                {
                    BodyPartColliderType = (EBodyPartColliderType)reader.GetInt(),
                    Direction = reader.GetVector3(),
                    Point = reader.GetVector3(),
                    Force = reader.GetFloat(),
                    OverallVelocity = reader.GetVector3()
                };
            }

            public static void Serialize(NetDataWriter writer, RagdollPacket packet)
            {
                writer.Put((int)packet.BodyPartColliderType);
                writer.Put(packet.Direction);
                writer.Put(packet.Point);
                writer.Put(packet.Force);
                writer.Put(packet.OverallVelocity);
            }
        }

        public struct DeathInfoPacket
        {
            public string AccountId;
            public string ProfileId;
            public string Nickname;
            public string KillerAccountId;
            public string KillerProfileId;
            public string KillerName;
            public EPlayerSide Side;
            public int Level;
            public DateTime Time;
            public string Status;
            public string WeaponName;
            public string GroupId;

            public static DeathInfoPacket Deserialize(NetDataReader reader)
            {
                return new()
                {
                    AccountId = reader.GetString(),
                    ProfileId = reader.GetString(),
                    Nickname = reader.GetString(),
                    KillerAccountId = reader.GetString(),
                    KillerProfileId = reader.GetString(),
                    KillerName = reader.GetString(),
                    Side = (EPlayerSide)reader.GetInt(),
                    Level = reader.GetInt(),
                    Time = reader.GetDateTime(),
                    Status = reader.GetString(),
                    WeaponName = reader.GetString(),
                    GroupId = reader.GetString()
                };
            }

            public static void Serialize(NetDataWriter writer, DeathInfoPacket packet)
            {
                writer.Put(packet.AccountId);
                writer.Put(packet.ProfileId);
                writer.Put(packet.Nickname);
                writer.Put(packet.KillerAccountId);
                writer.Put(packet.KillerProfileId);
                writer.Put(packet.KillerName);
                writer.Put((int)packet.Side);
                writer.Put(packet.Level);
                writer.Put(packet.Time);
                writer.Put(packet.Status);
                writer.Put(packet.WeaponName);
                writer.Put(packet.GroupId);
            }
        }

        public enum EProceedType
        {
            EmptyHands,
            FoodClass,
            GrenadeClass,
            MedsClass,
            QuickGrenadeThrow,
            QuickKnifeKick,
            QuickUse,
            Weapon,
            Knife,
            TryProceed
        }
    }
}
