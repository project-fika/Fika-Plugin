using Comfort.Common;
using EFT;
using EFT.Airdrop;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.Utils;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static BasePhysicalClass;
using static Fika.Core.Networking.CommonSubPackets;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.GenericSubPackets;
using static Fika.Core.Networking.RequestSubPackets;
using static Fika.Core.Networking.SubPacket;
using static Fika.Core.Networking.SubPackets;

namespace Fika.Core.Networking
{
    /// <summary>
    /// Serialization extensions for Unity/EFT classes to ease writing of packets in Fika
    /// </summary>
    public static class FikaSerializationExtensions
    {
        private static readonly byte[] _byteBuffer = new byte[12];
        private static readonly char[] _charBuffer = new char[24];

        /// <summary>
        /// Serializes a <see cref="Vector3"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vector"></param>
        public static void PutVector3(this NetDataWriter writer, Vector3 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
            writer.Put(vector.z);
        }

        /// <summary>
        /// Deserializes a <see cref="Vector3"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="Vector3"/></returns>
        public static Vector3 GetVector3(this NetDataReader reader)
        {
            return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        /// <summary>
        /// Serializes a <see cref="Vector2"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vector"></param>
        public static void PutVector2(this NetDataWriter writer, Vector2 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
        }

        /// <summary>
        /// Deserializes a <see cref="Vector2"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="Vector2"/></returns>
        public static Vector2 GetVector2(this NetDataReader reader)
        {
            return new Vector2(reader.GetFloat(), reader.GetFloat());
        }

        /// <summary>
        /// Serializes a <see cref="Quaternion"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="quaternion"></param>
        public static void PutQuaternion(this NetDataWriter writer, Quaternion quaternion)
        {
            writer.Put(quaternion.x);
            writer.Put(quaternion.y);
            writer.Put(quaternion.z);
            writer.Put(quaternion.w);
        }

        /// <summary>
        /// Deserializes a <see cref="Quaternion"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="Quaternion"/></returns>
        public static Quaternion GetQuaternion(this NetDataReader reader)
        {
            return new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        /// <summary>
        /// Serializes a <see cref="Color"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="color"></param>
        public static void PutColor(this NetDataWriter writer, Color color)
        {
            writer.Put(color.r);
            writer.Put(color.g);
            writer.Put(color.b);
            writer.Put(color.a);
        }

        /// <summary>
        /// Deserializes a <see cref="Color"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="Color"/>/returns>
        public static Color GetColor(this NetDataReader reader)
        {
            return new Color(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        /// <summary>
        /// Serializes a <see cref="PhysicalStateStruct"/> (Physical) struct
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="physical"></param>
        public static void PutPhysical(this NetDataWriter writer, PhysicalStateStruct physical)
        {
            byte flags = 0;

            if (physical.StaminaExhausted)
            {
                flags |= 1 << 0;
            }

            if (physical.OxygenExhausted)
            {
                flags |= 1 << 1;
            }

            if (physical.HandsExhausted)
            {
                flags |= 1 << 2;
            }

            writer.Put(flags);
        }

        /// <summary>
        /// Deserializes a <see cref="PhysicalStateStruct"/> (Physical) struct
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="PhysicalStateStruct"/> (Physical)</returns>
        public static PhysicalStateStruct GetPhysical(this NetDataReader reader)
        {
            byte flags = reader.GetByte();
            return new()
            {
                StaminaExhausted = (flags & (1 << 0)) != 0,
                OxygenExhausted = (flags & (1 << 1)) != 0,
                HandsExhausted = (flags & (1 << 2)) != 0
            };
        }

        /// <summary>
        /// Serialize a <see cref="byte"/> array
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="bytes"></param>
        public static void PutByteArray(this NetDataWriter writer, byte[] bytes)
        {
            int length = bytes.Length;
            writer.Put(length);
            if (length > 0)
            {
                writer.Put(bytes);
            }
        }

        /// <summary>
        /// Deserializes a <see cref="byte"/> array
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="byte"/> array</returns>
        public static byte[] GetByteArray(this NetDataReader reader)
        {
            int length = reader.GetInt();
            if (length <= 0)
            {
                return [];
            }

            byte[] bytes = new byte[length];
            reader.GetBytes(bytes, length);
            return bytes;
        }

        /// <summary>
        /// Serializes an <see cref="ArraySegment{T}"/> of <see cref="byte"/>[]
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="segment"></param>
        public static void PutByteSegment(this NetDataWriter writer, ArraySegment<byte> segment)
        {
            writer.PutBytesWithLength(segment.Array, segment.Offset, (ushort)segment.Count);
        }

        /// <summary>
        /// Deserializes an <see cref="ArraySegment{T}"/> of <see cref="byte"/>[]
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static byte[] GetByteSegment(this NetDataReader reader)
        {
            return reader.GetBytesWithLength();
        }

        /// <summary>
        /// Writes an enum value to the writer as a single byte
        /// </summary>
        /// <typeparam name="T">The enum type constrained to unmanaged enums</typeparam>
        /// <param name="writer">The writer to write the byte to</param>
        /// <param name="en">The enum value to serialize</param>
        public static void PutEnum<T>(this NetDataWriter writer, T en) where T : unmanaged, Enum
        {
            writer.Put(Unsafe.As<T, byte>(ref en));
        }

        /// <summary>
        /// Reads a byte from the reader and converts it to the enum type
        /// </summary>
        /// <typeparam name="T">The enum type constrained to unmanaged enums</typeparam>
        /// <param name="reader">The reader to read the byte from</param>
        /// <returns>The deserialized enum value</returns>
        public static T GetEnum<T>(this NetDataReader reader) where T : unmanaged, Enum
        {
            byte value = reader.GetByte();
            return Unsafe.As<byte, T>(ref value);
        }

        /// <summary>
        /// Serializes a <see cref="DateTime"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="dateTime"></param>
        public static void PutDateTime(this NetDataWriter writer, DateTime dateTime)
        {
            writer.Put(dateTime.ToOADate());
        }

        /// <summary>
        /// Deserializes a <see cref="DateTime"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="DateTime"/></returns>
        public static DateTime GetDateTime(this NetDataReader reader)
        {
            return DateTime.FromOADate(reader.GetDouble());
        }

        /// <summary>
        /// This write and serializes an <see cref="Item"/>, which can be cast to different types of inherited classes. Casting should be handled inside packet for consistency.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="item">The <see cref="Item"/> to serialize</param>
        public static void PutItem(this NetDataWriter writer, Item item)
        {
            EFTWriterClass eftWriter = new();
            InventoryDescriptorClass descriptor = EFTItemSerializerClass.SerializeItem(item, FikaGlobals.SearchControllerSerializer);
            eftWriter.WriteEFTItemDescriptor(descriptor);
            writer.PutByteArray(eftWriter.ToArray());
        }

        /// <summary>
        /// Gets a serialized <see cref="Item"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>An <see cref="Item"/> (cast to type inside packet)</returns>
        public static Item GetItem(this NetDataReader reader)
        {
            using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(reader.GetByteArray());
            return EFTItemSerializerClass.DeserializeItem(eftReader.ReadEFTItemDescriptor(), Singleton<ItemFactoryClass>.Instance, []);
        }

        /// <summary>
        /// Reads an <see cref="InventoryEquipment"/> serialized from <see cref="PutItem(NetDataWriter, Item)"/> and converts it into an <see cref="Inventory"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>An <see cref="Inventory"/></returns>
        public static Inventory GetInventoryFromEquipment(this NetDataReader reader)
        {
            using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(reader.GetByteArray());
            return new EFTInventoryClass()
            {
                Equipment = eftReader.ReadEFTItemDescriptor()
            }.ToInventory();
        }

        /// <summary>
        /// Writes an <see cref="InventoryDescriptorClass"/>
        /// </summary>
        /// <param name="writer">The writer to write data to</param>
        /// <param name="descriptor">The <see cref="InventoryDescriptorClass"/> instance to serialize</param>
        public static void PutItemDescriptor(this NetDataWriter writer, InventoryDescriptorClass descriptor)
        {
            EFTWriterClass eftWriter = new();
            eftWriter.WriteEFTItemDescriptor(descriptor);
            byte[] compressed = NetworkUtils.CompressBytes(eftWriter.ToArray());
            writer.PutByteArray(compressed);
        }

        /// <summary>
        /// Reads and returns an <see cref="InventoryDescriptorClass"/>
        /// </summary>
        /// <param name="reader">The reader to read the serialized inventory descriptor data from</param>
        /// <returns>The deserialized <see cref="InventoryDescriptorClass"/> instance</returns>
        public static InventoryDescriptorClass GetItemDescriptor(this NetDataReader reader)
        {
            byte[] decomprssed = NetworkUtils.DecompressBytes(reader.GetByteArray());
            using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(decomprssed);
            return eftReader.ReadEFTItemDescriptor();
        }

        /// <summary>
        /// Deserializes and retrieves an <see cref="Item"/>
        /// </summary>
        /// <param name="reader">The reader to read the serialized item data from</param>
        /// <returns>The deserialized <see cref="Item"/> instance representing the airdrop item</returns>
        public static Item GetAirdropItem(this NetDataReader reader)
        {
            using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(reader.GetByteArray());
            Item item = EFTItemSerializerClass.DeserializeItem(eftReader.ReadEFTItemDescriptor(), Singleton<ItemFactoryClass>.Instance, []);

            GClass1398 enumerable = [new LootItemPositionClass()];
            enumerable[0].Item = item;
            Item[] array = [.. enumerable.Select(FikaGlobals.GetLootItemPositionItem)];
            ResourceKey[] resourceKeys = [.. array.OfType<GClass3118>().GetAllItemsFromCollections()
                .Concat(array.Where(AirdropSynchronizableObject.Class2036.class2036_0.method_1))
                .SelectMany(AirdropSynchronizableObject.Class2036.class2036_0.method_2)];
            Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(PoolManagerClass.PoolsCategory.Raid, PoolManagerClass.AssemblyType.Online,
                resourceKeys, JobPriorityClass.Immediate, null, default).HandleExceptions();

            return item;
        }

        /// <summary>
        /// Serializes a <see cref="List{T}"/> of <see cref="SmokeGrenadeDataPacketStruct"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="throwables"></param>
        public static void PutThrowableData(this NetDataWriter writer, List<SmokeGrenadeDataPacketStruct> throwables)
        {
            writer.Put(throwables.Count);
            foreach (SmokeGrenadeDataPacketStruct data in throwables)
            {
                writer.Put(data.Id);
                writer.PutVector3(data.Position);
                writer.Put(data.Template);
                writer.Put(data.Time);
                writer.PutQuaternion(data.Orientation);
                writer.Put(data.PlatformId);
            }
        }

        /// <summary>
        /// Serializes a <see cref="Profile"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="profile"></param>
        public static void PutProfile(this NetDataWriter writer, Profile profile)
        {
            EFTWriterClass eftWriter = new();
            eftWriter.WriteEFTProfileDescriptor(new(profile, FikaGlobals.SearchControllerSerializer));
            byte[] compressed = NetworkUtils.CompressBytes(eftWriter.ToArray());
            writer.PutByteArray(compressed);
        }

        /// <summary>
        /// Deserializes a <see cref="Profile"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="Profile"/></returns>
        public static Profile GetProfile(this NetDataReader reader)
        {
            byte[] decompressed = NetworkUtils.DecompressBytes(reader.GetByteArray());
            using GClass1277 eftReader = PacketToEFTReaderAbstractClass.Get(decompressed);
            return new(eftReader.ReadEFTProfileDescriptor());
        }

        /// <summary>
        /// Deserializes a <see cref="List{T}"/> of <see cref="SmokeGrenadeDataPacketStruct"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="List{T}"/> of <see cref="SmokeGrenadeDataPacketStruct"/></returns>
        public static List<SmokeGrenadeDataPacketStruct> GetThrowableData(this NetDataReader reader)
        {
            int amount = reader.GetInt();
            List<SmokeGrenadeDataPacketStruct> throwables = new(amount);
            for (int i = 0; i < amount; i++)
            {
                SmokeGrenadeDataPacketStruct data = new()
                {
                    Id = reader.GetString(),
                    Position = reader.GetVector3(),
                    Template = reader.GetString(),
                    Time = reader.GetInt(),
                    Orientation = reader.GetQuaternion(),
                    PlatformId = reader.GetShort()
                };
                throwables.Add(data);
            }

            return throwables;
        }

        /// <summary>
        /// Serializes a <see cref="List{WorldInteractiveObject.WorldInteractiveDataPacketStruct}"/> of <see cref="WorldInteractiveObject"/> data
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="interactiveObjectsData"></param>
        public static void PutInteractivesStates(this NetDataWriter writer, List<WorldInteractiveObject.WorldInteractiveDataPacketStruct> interactiveObjectsData)
        {
            writer.Put(interactiveObjectsData.Count);
            for (int i = 0; i < interactiveObjectsData.Count; i++)
            {
                writer.Put(interactiveObjectsData[i].NetId);
                writer.Put(interactiveObjectsData[i].State);
                writer.Put(interactiveObjectsData[i].IsBroken);
            }
        }

        /// <summary>
        /// Deserializes a <see cref="List{WorldInteractiveObject.WorldInteractiveDataPacketStruct}"/> of <see cref="WorldInteractiveObject"/> data
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="List{T}"/> of <see cref="WorldInteractiveObject.WorldInteractiveDataPacketStruct"/></returns>
        public static List<WorldInteractiveObject.WorldInteractiveDataPacketStruct> GetInteractivesStates(this NetDataReader reader)
        {
            int amount = reader.GetInt();
            List<WorldInteractiveObject.WorldInteractiveDataPacketStruct> interactivesStates = new(amount);
            for (int i = 0; i < amount; i++)
            {
                WorldInteractiveObject.WorldInteractiveDataPacketStruct data = new()
                {
                    NetId = reader.GetInt(),
                    State = reader.GetByte(),
                    IsBroken = reader.GetBool()
                };
                interactivesStates.Add(data);
            }

            return interactivesStates;
        }

        /// <summary>
        /// Serializes a <see cref="Dictionary{int, byte}"/> of <see cref="LampController"/> information
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="lampStates"></param>
        public static void PutLampStates(this NetDataWriter writer, Dictionary<int, byte> lampStates)
        {
            int amount = lampStates.Count;
            writer.Put(amount);
            foreach (KeyValuePair<int, byte> lampState in lampStates)
            {
                writer.Put(lampState.Key);
                writer.Put(lampState.Value);
            }
        }

        /// <summary>
        /// Deserializes a <see cref="Dictionary{int, byte}"/> of <see cref="LampController"/> information
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> of information for <see cref="LampController"/>s</returns>
        public static Dictionary<int, byte> GetLampStates(this NetDataReader reader)
        {
            int amount = reader.GetInt();
            Dictionary<int, byte> states = new(amount);
            for (int i = 0; i < amount; i++)
            {
                states.Add(reader.GetInt(), reader.GetByte());
            }

            return states;
        }

        /// <summary>
        /// Serializes a <see cref="Dictionary{int, Vector3}"/> of <see cref="WindowBreaker"/> information
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="windowBreakerStates"></param>
        public static void PutWindowBreakerStates(this NetDataWriter writer, Dictionary<int, Vector3> windowBreakerStates)
        {
            int amount = windowBreakerStates.Count;
            writer.Put(amount);
            foreach (KeyValuePair<int, Vector3> windowBreakerState in windowBreakerStates)
            {
                writer.Put(windowBreakerState.Key);
                writer.PutVector3(windowBreakerState.Value);
            }
        }

        /// <summary>
        /// Deserializes a <see cref="Dictionary{int, Vector3}"/> of <see cref="WindowBreaker"/> information
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> of information for <see cref="WindowBreaker"/>s</returns>
        public static Dictionary<int, Vector3> GetWindowBreakerStates(this NetDataReader reader)
        {
            int amount = reader.GetInt();
            Dictionary<int, Vector3> states = new(amount);
            for (int i = 0; i < amount; i++)
            {
                states.Add(reader.GetInt(), reader.GetVector3());
            }

            return states;
        }

        /// <summary>
        /// Serializes a <see cref="MongoID"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="mongoId"></param>
        public static void PutMongoID(this NetDataWriter writer, MongoID? mongoId)
        {
            writer.Put(mongoId.HasValue);
            if (mongoId.HasValue)
            {
                writer.Put(HexStringToBytes(mongoId.Value), 0, 12);
            }
        }

        /// <summary>
        /// Deserializes a <see cref="MongoID"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A new <see cref="MongoID"/>? (nullable)</returns>
        public static MongoID? GetMongoID(this NetDataReader reader)
        {
            if (!reader.GetBool())
            {
                return null;
            }

            reader.GetBytes(_byteBuffer, 0, 12);
            return new(BytesToHexString(_byteBuffer));
        }

        /// <summary>
        /// Serializes a <see cref="TraderServicesClass"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="traderService"></param>
        public static void PutTraderService(this NetDataWriter writer, TraderServicesClass traderService)
        {
            writer.PutMongoID(traderService.TraderId);
            writer.Put((byte)traderService.ServiceType);
            writer.Put(traderService.CanAfford);
            writer.Put(traderService.WasPurchasedInThisRaid);
            writer.Put(traderService.ItemsToPay.Count);
            foreach (KeyValuePair<MongoID, int> pair in traderService.ItemsToPay)
            {
                writer.PutMongoID(pair.Key);
                writer.Put(pair.Value);
            }
            int uniqueAmount = traderService.UniqueItems.Length;
            writer.Put(uniqueAmount);
            for (int i = 0; i < uniqueAmount; i++)
            {
                writer.PutMongoID(traderService.UniqueItems[i]);
            }
            writer.Put(traderService.SubServices.Count);
            foreach (KeyValuePair<string, int> pair in traderService.SubServices)
            {
                writer.Put(pair.Key);
                writer.Put(pair.Value);
            }
        }

        /// <summary>
        /// Deserializes a <see cref="TraderServicesClass"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="TraderServicesClass"/></returns>
        public static TraderServicesClass GetTraderService(this NetDataReader reader)
        {
            TraderServicesClass traderService = new()
            {
                TraderId = reader.GetMongoID().Value,
                ServiceType = (ETraderServiceType)reader.GetByte(),
                CanAfford = reader.GetBool(),
                WasPurchasedInThisRaid = reader.GetBool()
            };
            int toPayAmount = reader.GetInt();
            traderService.ItemsToPay = new(toPayAmount);
            for (int i = 0; i < toPayAmount; i++)
            {
                traderService.ItemsToPay[reader.GetMongoID().Value] = reader.GetInt();
            }
            int uniqueAmount = reader.GetInt();
            traderService.UniqueItems = new MongoID[uniqueAmount];
            for (int i = 0; i < uniqueAmount; i++)
            {
                traderService.UniqueItems[i] = reader.GetMongoID().Value;
            }
            int subAmount = reader.GetInt();
            traderService.SubServices = new(subAmount);
            for (int i = 0; i < subAmount; i++)
            {
                traderService.SubServices[reader.GetString()] = reader.GetInt();
            }
            return traderService;
        }

        /// <summary>
        /// Writes a <see cref="Profile.ProfileHealthClass"/> into a raw <see cref="byte"/>[]
        /// </summary>
        /// <param name="health"></param>
        /// <returns><see cref="byte"/>[]</returns>
        public static byte[] SerializeHealthInfo(this Profile.ProfileHealthClass health)
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.WriteValueInfo(health.Energy);
            writer.WriteValueInfo(health.Hydration);
            writer.WriteValueInfo(health.Temperature);
            writer.WriteValueInfo(health.Poison);
            float standard = 1f;
            // Heal Rate
            writer.Write(standard);
            // Damage Rate
            writer.Write(standard);
            // Damage Multiplier
            writer.Write(standard);
            // Energy Rate
            writer.Write(standard);
            // Hydration Rate
            writer.Write(standard);
            // Temperate Rate
            writer.Write(standard);
            // Damage Coeff
            writer.Write(standard);
            // Stamina Coeff
            writer.Write(standard);

            foreach (KeyValuePair<EBodyPart, Profile.ProfileHealthClass.ProfileBodyPartHealthClass> bodyPart in health.BodyParts)
            {
                Profile.ProfileHealthClass.ValueInfo bodyPartInfo = bodyPart.Value.Health;
                writer.Write(bodyPartInfo.Current <= bodyPartInfo.Minimum);
                writer.Write(bodyPartInfo.Current);
                writer.Write(bodyPartInfo.Maximum);
            }

            // Effect Amount - Set to 0 as it's a fresh profile
            short effectAmount = 0;
            writer.Write(effectAmount);
            byte end = 42;
            writer.Write(end);

            return stream.ToArray();
        }

        /// <summary>
        /// Writes a <see cref="Profile.ProfileHealthClass.ValueInfo"/> into <see cref="byte"/>s
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="valueInfo"></param>
        public static void WriteValueInfo(this BinaryWriter writer, Profile.ProfileHealthClass.ValueInfo valueInfo)
        {
            writer.Write(valueInfo.Current);
            writer.Write(valueInfo.Minimum);
            writer.Write(valueInfo.Maximum);
        }

        /// <summary>
        /// Serializes a <see cref="ArtilleryPacketStruct"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="artilleryStruct"></param>
        public static void PutArtilleryStruct(this NetDataWriter writer, ArtilleryPacketStruct artilleryStruct)
        {
            writer.Put(artilleryStruct.id);
            writer.PutVector3(artilleryStruct.position);
            writer.Put(artilleryStruct.explosion);
        }

        /// <summary>
        /// Deserializes a <see cref="ArtilleryPacketStruct"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="ArtilleryPacketStruct"/> with data</returns>
        public static ArtilleryPacketStruct GetArtilleryStruct(this NetDataReader reader)
        {
            return new()
            {
                id = reader.GetInt(),
                position = reader.GetVector3(),
                explosion = reader.GetBool()
            };
        }

        /// <summary>
        /// Serializes a <see cref="GrenadeDataPacketStruct"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="grenadeStruct"></param>
        public static void PutGrenadeStruct(this NetDataWriter writer, GrenadeDataPacketStruct grenadeStruct)
        {
            writer.Put(grenadeStruct.Id);
            writer.PutVector3(grenadeStruct.Position);
            writer.PutQuaternion(grenadeStruct.Rotation);
            writer.Put(grenadeStruct.CollisionNumber);
            writer.Put(grenadeStruct.Done);
            if (!grenadeStruct.Done)
            {
                writer.PutVector3(grenadeStruct.Velocity);
                writer.PutVector3(grenadeStruct.AngularVelocity);
            }
        }

        /// <summary>
        /// Serializes a <see cref="AirplaneDataPacketStruct"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="airplaneDataPacketStruct"></param>
        public static void PutAirplaneDataPacketStruct(this NetDataWriter writer, AirplaneDataPacketStruct airplaneDataPacketStruct)
        {
            writer.Put((byte)airplaneDataPacketStruct.ObjectType);
            writer.Put((byte)airplaneDataPacketStruct.ObjectId);

            switch (airplaneDataPacketStruct.ObjectType)
            {
                case SynchronizableObjectType.AirDrop:
                    writer.PutVector3(airplaneDataPacketStruct.Position);
                    writer.PutVector3(airplaneDataPacketStruct.Rotation);
                    writer.Put(airplaneDataPacketStruct.Outdated);
                    writer.Put(airplaneDataPacketStruct.IsStatic);
                    writer.Put((byte)airplaneDataPacketStruct.PacketData.AirdropDataPacket.AirdropType);
                    writer.Put((byte)airplaneDataPacketStruct.PacketData.AirdropDataPacket.FallingStage);
                    writer.Put(airplaneDataPacketStruct.PacketData.AirdropDataPacket.SignalFire);
                    writer.Put(airplaneDataPacketStruct.PacketData.AirdropDataPacket.UniqueId);
                    return;
                case SynchronizableObjectType.AirPlane:
                    writer.PutVector3(airplaneDataPacketStruct.Position);
                    writer.PutVector3(airplaneDataPacketStruct.Rotation);
                    writer.Put(airplaneDataPacketStruct.PacketData.AirplaneDataPacket.AirplanePercent);
                    writer.Put(airplaneDataPacketStruct.Outdated);
                    writer.Put(airplaneDataPacketStruct.IsStatic);
                    return;
                case SynchronizableObjectType.Tripwire:
                    writer.Put((byte)airplaneDataPacketStruct.PacketData.TripwireDataPacket.State);
                    writer.PutVector3(airplaneDataPacketStruct.Position);
                    writer.PutVector3(airplaneDataPacketStruct.Rotation);
                    writer.Put(airplaneDataPacketStruct.IsActive);
                    return;
            }
        }

        /// <summary>
        /// Deserializes a <see cref="AirplaneDataPacketStruct"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static AirplaneDataPacketStruct GetAirplaneDataPacketStruct(this NetDataReader reader)
        {
            AirplaneDataPacketStruct packet = new()
            {
                ObjectType = (SynchronizableObjectType)reader.GetByte(),
                ObjectId = reader.GetByte(),
                PacketData = new()
            };

            switch (packet.ObjectType)
            {
                case SynchronizableObjectType.AirDrop:
                    packet.Position = reader.GetVector3();
                    packet.Rotation = reader.GetVector3();
                    packet.Outdated = reader.GetBool();
                    packet.IsStatic = reader.GetBool();
                    packet.PacketData.AirdropDataPacket = new()
                    {
                        AirdropType = (EAirdropType)reader.GetByte(),
                        FallingStage = (EAirdropFallingStage)reader.GetByte(),
                        SignalFire = reader.GetBool(),
                        UniqueId = reader.GetInt()
                    };
                    break;
                case SynchronizableObjectType.AirPlane:
                    packet.Position = reader.GetVector3();
                    packet.Rotation = reader.GetVector3();
                    packet.PacketData.AirplaneDataPacket = new()
                    {
                        AirplanePercent = reader.GetInt()
                    };
                    packet.Outdated = reader.GetBool();
                    packet.IsStatic = reader.GetBool();
                    break;
                case SynchronizableObjectType.Tripwire:
                    packet.PacketData.TripwireDataPacket = new()
                    {
                        State = (ETripwireState)reader.GetByte()
                    };
                    packet.Position = reader.GetVector3();
                    packet.Rotation = reader.GetVector3();
                    packet.IsActive = reader.GetBool();
                    break;
            }

            return packet;
        }

        /// <summary>
        /// Deserializes a <see cref="GrenadeDataPacketStruct"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="GrenadeDataPacketStruct"/> with data</returns>
        public static GrenadeDataPacketStruct GetGrenadeStruct(this NetDataReader reader)
        {
            GrenadeDataPacketStruct grenadeStruct = new()
            {
                Id = reader.GetInt(),
                Position = reader.GetVector3(),
                Rotation = reader.GetQuaternion(),
                CollisionNumber = reader.GetByte()
            };

            if (!reader.GetBool())
            {
                grenadeStruct.Velocity = reader.GetVector3();
                grenadeStruct.AngularVelocity = reader.GetVector3();
                return grenadeStruct;
            }

            grenadeStruct.Done = true;
            return grenadeStruct;
        }

        /// <summary>
        /// Serializes a <see cref="PlayerInfoPacket"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="packet"></param>
        public static void PutPlayerInfoPacket(this NetDataWriter writer, PlayerInfoPacket packet)
        {
            writer.PutProfile(packet.Profile);
            writer.PutMongoID(packet.ControllerId);
            writer.Put(packet.FirstOperationId);
            writer.PutByteArray(packet.HealthByteArray ?? ([]));
            writer.Put((byte)packet.ControllerType);
            if (packet.ControllerType != EHandsControllerType.None)
            {
                writer.Put(packet.ItemId);
                writer.Put(packet.IsStationary);
            }
            writer.Put(packet.IsZombie);
        }

        /// <summary>
        /// Deserializes a <see cref="PlayerInfoPacket"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="PlayerInfoPacket"/> with data</returns>
        public static PlayerInfoPacket GetPlayerInfoPacket(this NetDataReader reader)
        {
            PlayerInfoPacket packet = new()
            {
                Profile = reader.GetProfile(),
                ControllerId = reader.GetMongoID(),
                FirstOperationId = reader.GetUShort(),
                HealthByteArray = reader.GetByteArray(),
                ControllerType = (EHandsControllerType)reader.GetByte()
            };
            if (packet.ControllerType != EHandsControllerType.None)
            {
                packet.ItemId = reader.GetString();
                packet.IsStationary = reader.GetBool();
            }
            packet.IsZombie = reader.GetBool();
            return packet;
        }

        /// <summary>
        /// Serializes a <see cref="WeatherClass"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="weatherClass"></param>
        public static void PutWeatherClass(this NetDataWriter writer, WeatherClass weatherClass)
        {
            writer.Put(weatherClass.AtmospherePressure);
            writer.Put(weatherClass.Cloudness);
            writer.Put(weatherClass.GlobalFogDensity);
            writer.Put(weatherClass.GlobalFogHeight);
            writer.Put(weatherClass.LyingWater);
            writer.PutVector2(weatherClass.MainWindDirection);
            writer.PutVector2(weatherClass.MainWindPosition);
            writer.Put(weatherClass.Rain);
            writer.Put(weatherClass.RainRandomness);
            writer.Put(weatherClass.ScaterringFogDensity);
            writer.Put(weatherClass.ScaterringFogHeight);
            writer.Put(weatherClass.Temperature);
            writer.Put(weatherClass.Time);
            writer.PutVector2(weatherClass.TopWindDirection);
            writer.PutVector2(weatherClass.TopWindPosition);
            writer.Put(weatherClass.Turbulence);
            writer.Put(weatherClass.Wind);
            writer.Put(weatherClass.WindDirection);
        }

        /// <summary>
        /// Deserializes a <see cref="WeatherClass"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="WeatherClass"/> with data</returns>
        public static WeatherClass GetWeatherClass(this NetDataReader reader)
        {
            return new WeatherClass()
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

        /// <summary>
        /// Serializes a <see cref="CorpseSyncPacket"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="packet"></param>
        public static void PutCorpseSyncPacket(this NetDataWriter writer, CorpseSyncPacket packet)
        {
            writer.Put((int)packet.BodyPartColliderType);
            writer.PutVector3(packet.Direction);
            writer.PutVector3(packet.Point);
            writer.Put(packet.Force);
            writer.PutVector3(packet.OverallVelocity);
            writer.PutItemDescriptor(packet.InventoryDescriptor);
            writer.Put((byte)packet.ItemSlot);
        }

        /// <summary>
        /// Deserializes a <see cref="CorpseSyncPacket"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="CorpseSyncPacket"/> with data</returns>
        public static CorpseSyncPacket GetCorpseSyncPacket(this NetDataReader reader)
        {
            return new CorpseSyncPacket()
            {
                BodyPartColliderType = (EBodyPartColliderType)reader.GetInt(),
                Direction = reader.GetVector3(),
                Point = reader.GetVector3(),
                Force = reader.GetFloat(),
                OverallVelocity = reader.GetVector3(),
                InventoryDescriptor = reader.GetItemDescriptor(),
                ItemSlot = (EquipmentSlot)reader.GetByte()
            };
        }

        /// <summary>
        /// Serializes a <see cref="DeathInfoPacket"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="packet"></param>
        public static void PutDeathInfoPacket(this NetDataWriter writer, DeathInfoPacket packet)
        {
            writer.Put(packet.AccountId);
            writer.Put(packet.ProfileId);
            writer.Put(packet.Nickname);
            writer.Put(packet.KillerAccountId);
            writer.Put(packet.KillerProfileId);
            writer.Put(packet.KillerName);
            writer.Put((byte)packet.Side);
            writer.Put(packet.Level);
            writer.PutDateTime(packet.Time);
            writer.Put(packet.Status);
            writer.Put(packet.WeaponName);
            writer.Put(packet.GroupId);
        }

        /// <summary>
        /// Deserializes a <see cref="DeathInfoPacket"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>A <see cref="DeathInfoPacket"/> with data</returns>
        public static DeathInfoPacket GetDeathInfoPacket(this NetDataReader reader)
        {
            return new()
            {
                AccountId = reader.GetString(),
                ProfileId = reader.GetString(),
                Nickname = reader.GetString(),
                KillerAccountId = reader.GetString(),
                KillerProfileId = reader.GetString(),
                KillerName = reader.GetString(),
                Side = (EPlayerSide)reader.GetByte(),
                Level = reader.GetInt(),
                Time = reader.GetDateTime(),
                Status = reader.GetString(),
                WeaponName = reader.GetString(),
                GroupId = reader.GetString()
            };
        }

        /// <summary>
        /// Writes a <see cref="RagdollPacketStruct"/>
        /// </summary>
        /// <param name="writer">The writer to write data to</param>
        /// <param name="packet">The <see cref="RagdollPacketStruct"/> instance to serialize</param>
        public static void PutRagdollStruct(this NetDataWriter writer, RagdollPacketStruct packet)
        {
            writer.Put(packet.Id);
            writer.PutVector3(packet.Position);
            writer.Put(packet.Done);

            if (packet.Done && packet.TransformSyncs != null)
            {
                GStruct135[] transforms = packet.TransformSyncs;
                for (int i = 0; i < 12; i++)
                {
                    writer.PutVector3(transforms[i].Position);
                    writer.PutQuaternion(transforms[i].Rotation);
                }
            }
        }

        /// <summary>
        /// Reads a <see cref="RagdollPacketStruct"/>
        /// </summary>
        /// <param name="reader">The reader to read data from.</param>
        /// <returns>The deserialized <see cref="RagdollPacketStruct"/></returns>
        public static RagdollPacketStruct GetRagdollStruct(this NetDataReader reader)
        {
            RagdollPacketStruct packet = new()
            {
                Id = reader.GetInt(),
                Position = reader.GetVector3(),
                Done = reader.GetBool()
            };

            if (packet.Done)
            {
                packet.TransformSyncs = new GStruct135[12];
                for (int i = 0; i < 12; i++)
                {
                    packet.TransformSyncs[i] = new()
                    {
                        Position = reader.GetVector3(),
                        Rotation = reader.GetQuaternion()
                    };
                }
            }

            return packet;
        }

        /// <summary>
        /// Writes a <see cref="LootSyncStruct"/>
        /// </summary>
        /// <param name="writer">The writer to write data to</param>
        /// <param name="packet">The <see cref="LootSyncStruct"/> to serialize</param>
        public static void PutLootSyncStruct(this NetDataWriter writer, LootSyncStruct packet)
        {
            writer.Put(packet.Id);
            writer.PutVector3(packet.Position);
            writer.PutQuaternion(packet.Rotation);
            writer.Put(packet.Done);

            if (!packet.Done)
            {
                writer.PutVector3(packet.Velocity);
                writer.PutVector3(packet.AngularVelocity);
            }
        }

        /// <summary>
        /// Reads a <see cref="LootSyncStruct"/>
        /// </summary>
        /// <param name="reader">The reader to read data from</param>
        /// <returns>The deserialized <see cref="LootSyncStruct"/></returns>
        public static LootSyncStruct GetLootSyncStruct(this NetDataReader reader)
        {
            LootSyncStruct data = new()
            {
                Id = reader.GetInt(),
                Position = reader.GetVector3(),
                Rotation = reader.GetQuaternion(),
                Done = reader.GetBool()
            };

            if (!data.Done)
            {
                data.Velocity = reader.GetVector3();
                data.AngularVelocity = reader.GetVector3();
            }

            return data;
        }

        /// <summary>
        /// Compresses a float value into a fixed bit-width integer and writes it to the NetDataWriter. <br/>
        /// Supports 8 or 16 bit compression for network efficiency.
        /// </summary>
        /// <param name="writer">The network data writer</param>
        /// <param name="value">The float value to compress</param>
        /// <param name="min">Minimum expected float value (clamping lower bound)</param>
        /// <param name="max">Maximum expected float value (clamping upper bound)</param>
        /// <param name="compression">Number of bits to use for compression (8 or 16)</param>
        public static void PutPackedFloat(this NetDataWriter writer, float value, float min, float max, EFloatCompression compression = EFloatCompression.Low)
        {
            // Clamp input value to expected range
            float clamped = Mathf.Clamp(value, min, max);

            // Calculate max integer value for the bit width (e.g., 255 for 8 bits, 65535 for 16 bits)
            int maxInt = (1 << (int)compression) - 1;

            // Normalize and quantize using rounding for better precision
            int quantized = Mathf.RoundToInt((clamped - min) / (max - min) * maxInt);

            if (compression is EFloatCompression.High)
            {
                writer.Put((byte)quantized);
            }
            else
            {
                writer.Put((ushort)quantized);
            }
        }

        /// <summary>
        /// Reads a compressed float value from the NetDataReader that was packed with a fixed bit-width integer. <br/>
        /// Supports 8 or 16 bit decompression.
        /// </summary>
        /// <param name="reader">The network data reader</param>
        /// <param name="min">Minimum expected float value (used for decompression range)</param>
        /// <param name="max">Maximum expected float value (used for decompression range)</param>
        /// <param name="compression">Number of bits used during compression (8 or 16)</param>
        /// <returns>The decompressed float value.</returns>
        public static float GetPackedFloat(this NetDataReader reader, float min, float max, EFloatCompression compression = EFloatCompression.Low)
        {
            int maxInt = (1 << (int)compression) - 1;
            int quantized;

            if (compression is EFloatCompression.High)
            {
                quantized = reader.GetByte();
            }
            else
            {
                quantized = reader.GetUShort();
            }

            float normalized = (float)quantized / maxInt;
            return min + normalized * (max - min);
        }

        /// <summary>
        /// Scales an integer value from a specified input range to a target byte range and writes it
        /// </summary>
        /// <param name="writer">The NetDataWriter to write to</param>
        /// <param name="value">The integer value to scale</param>
        /// <param name="minValue">The minimum value of the input integer range</param>
        /// <param name="maxValue">The maximum value of the input integer range</param>
        public static void PutPackedInt(this NetDataWriter writer, int value, int minValue, int maxValue)
        {
            int minTarget = 0;
            int maxTarget = byte.MaxValue;

            int clampedValue = Mathf.Clamp(value, minValue, maxValue) - minValue;
            int rangeInput = maxValue - minValue;
            int rangeTarget = maxTarget - minTarget;

            float normalized = (float)clampedValue / rangeInput;
            int scaled = (int)(minTarget + normalized * rangeTarget);

            byte result = (byte)Mathf.Clamp(scaled, minTarget, maxTarget);
            writer.Put(result);
        }

        /// <summary>
        /// Scales a byte value from a specified input range to a target integer range
        /// </summary>
        /// <param name="reader">The NetDataReader to read from</param>
        /// <param name="minTarget">The minimum value of the target integer range</param>
        /// <param name="maxTarget">The maximum value of the target integer range</param>
        /// <returns>The scaled integer value within the target range</returns>
        public static int GetPackedInt(this NetDataReader reader, int minTarget, int maxTarget)
        {
            int minValue = 0;
            int maxValue = byte.MaxValue;

            byte value = reader.GetByte();

            int rangeInput = maxValue - minValue;
            int clampedValue = Mathf.Clamp(value, minValue, maxValue) - minValue;
            float rangeTarget = maxTarget - minTarget;

            float normalized = (float)clampedValue / rangeInput;
            return (int)(minTarget + normalized * rangeTarget);
        }

        /// <summary>
        /// Writes the head rotation angles, compressing each <see cref="float"/> using high compression.
        /// </summary>
        /// <param name="writer">The network data writer to write to</param>
        /// <param name="rotation">The head rotation</param>
        public static void PutHeadRotation(this NetDataWriter writer, Vector2 rotation)
        {
            writer.PutPackedFloat(rotation.x, -50f, 20f, EFloatCompression.High);
            writer.PutPackedFloat(rotation.y, -40f, 40f, EFloatCompression.High);
        }

        /// <summary>
        /// Reads the head rotation angles, decompressing each <see cref="float"/> using high compression.
        /// </summary>
        /// <param name="reader">The network data reader to read from</param>
        /// <returns>A <see cref="Vector2"/> representing the head rotation</returns>
        public static Vector2 GetHeadRotation(this NetDataReader reader)
        {
            return new()
            {
                x = reader.GetPackedFloat(-50f, 20f, EFloatCompression.High),
                y = reader.GetPackedFloat(-40f, 40f, EFloatCompression.High)
            };
        }

        /// <summary>
        /// Writes the movement direction <see cref="Vector2"/>, compressing each <see cref="float"/> using high compression.
        /// </summary>
        /// <param name="writer">The network data writer to write to</param>
        /// <param name="movementDirection">The movement direction</param>
        public static void PutMovementDirection(this NetDataWriter writer, Vector2 movementDirection)
        {
            writer.PutPackedFloat(movementDirection.x, -1f, 1f, EFloatCompression.High);
            writer.PutPackedFloat(movementDirection.y, -1f, 1f, EFloatCompression.High);
        }

        /// <summary>
        /// Reads the movement direction <see cref="Vector2"/>, decompressing each <see cref="float"/> using high compression.
        /// </summary>
        /// <param name="reader">The network data reader to read from</param>
        /// <returns>A <see cref="Vector2"/> representing movement direction</returns>
        public static Vector2 GetMovementDirection(this NetDataReader reader)
        {
            return new()
            {
                x = reader.GetPackedFloat(-1f, 1f, EFloatCompression.High),
                y = reader.GetPackedFloat(-1f, 1f, EFloatCompression.High)
            };
        }

        /// <summary>
        /// Writes a <see cref="Vector2"/> of the players rotation
        /// </summary>
        /// <param name="writer">The <see cref="NetDataWriter"/> instance to write to</param>
        /// <param name="rotation">
        /// The <see cref="Vector2"/> representing the rotation, where:
        /// <list type="bullet">
        /// <item><description><c>x</c> is stored as a raw float</description></item>
        /// <item><description><c>y</c> is stored as a compressed float in the range [-90, 90] using <see cref="EFloatCompression.High"/></description></item>
        /// </list>
        /// </param>
        public static void PutRotation(this NetDataWriter writer, Vector2 rotation)
        {
            writer.Put(rotation.x);
            writer.PutPackedFloat(rotation.y, -90f, 90f, EFloatCompression.High);
        }

        /// <summary>
        /// Reads a <see cref="Vector2"/> of the players rotation
        /// </summary>
        /// <param name="reader">The <see cref="NetDataReader"/> instance to read from</param>
        /// <returns>
        /// A <see cref="Vector2"/> where:
        /// <list type="bullet">
        /// <item><description><c>x</c> is read as a raw float.</description></item>
        /// <item><description><c>y</c> is read as a compressed float in the range [-90, 90] using <see cref="EFloatCompression.High"/>.</description></item>
        /// </list>
        /// </returns>
        public static Vector2 GetRotation(this NetDataReader reader)
        {
            return new()
            {
                x = reader.GetFloat(),
                y = reader.GetPackedFloat(-90f, 90f, EFloatCompression.High)
            };
        }

        private static byte[] HexStringToBytes(string hex)
        {
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            if (hex.Length != 24)
            {
                throw new ArgumentException("Hex string must be 24 characters long.", nameof(hex));
            }

            for (int i = 0; i < 12; i++)
            {
                int high = FromHexChar(hex[i * 2]);
                int low = FromHexChar(hex[i * 2 + 1]);
                _byteBuffer[i] = (byte)((high << 4) | low);
            }

            return _byteBuffer;
        }

        private static string BytesToHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != 12)
            {
                throw new ArgumentException("Byte array must be 12 bytes long.", nameof(bytes));
            }

            for (int i = 0; i < 12; i++)
            {
                byte b = bytes[i];
                _charBuffer[i * 2] = ToHexChar(b >> 4);
                _charBuffer[i * 2 + 1] = ToHexChar(b & 0xF);
            }

            return new string(_charBuffer);
        }

        private static int FromHexChar(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            throw new ArgumentException($"Invalid hex character: {c}");
        }

        private static char ToHexChar(int val)
        {
            return (char)(val < 10 ? ('0' + val) : ('a' + (val - 10)));
        }

        public static void PutFirearmSubPacket(this NetDataWriter writer, ISubPacket packet, EFirearmSubPacketType type)
        {
            switch (type)
            {
                case EFirearmSubPacketType.ShotInfo:
                case EFirearmSubPacketType.ChangeFireMode:
                case EFirearmSubPacketType.ToggleAim:
                case EFirearmSubPacketType.ToggleLightStates:
                case EFirearmSubPacketType.ToggleScopeStates:
                case EFirearmSubPacketType.ToggleInventory:
                case EFirearmSubPacketType.LeftStanceChange:
                case EFirearmSubPacketType.ReloadMag:
                case EFirearmSubPacketType.QuickReloadMag:
                case EFirearmSubPacketType.ReloadWithAmmo:
                case EFirearmSubPacketType.CylinderMag:
                case EFirearmSubPacketType.ReloadLauncher:
                case EFirearmSubPacketType.ReloadBarrels:
                case EFirearmSubPacketType.Grenade:
                case EFirearmSubPacketType.CompassChange:
                case EFirearmSubPacketType.Knife:
                case EFirearmSubPacketType.FlareShot:
                case EFirearmSubPacketType.RocketShot:
                case EFirearmSubPacketType.RollCylinder:
                    packet.Serialize(writer);
                    break;

                case EFirearmSubPacketType.ToggleLauncher:
                case EFirearmSubPacketType.CancelGrenade:
                case EFirearmSubPacketType.ReloadBoltAction:
                case EFirearmSubPacketType.UnderbarrelSightingRangeUp:
                case EFirearmSubPacketType.UnderbarrelSightingRangeDown:
                case EFirearmSubPacketType.ToggleBipod:
                case EFirearmSubPacketType.ExamineWeapon:
                case EFirearmSubPacketType.CheckAmmo:
                case EFirearmSubPacketType.CheckChamber:
                case EFirearmSubPacketType.CheckFireMode:
                case EFirearmSubPacketType.Loot:
                    break;
                default:
                    FikaPlugin.Instance.FikaLogger.LogError("PutFirearmSubPacket: type was outside of bounds!");
                    break;
            }
        }

        public static ISubPacket GetFirearmSubPacket(this NetDataReader reader, EFirearmSubPacketType type)
        {
            switch (type)
            {
                case EFirearmSubPacketType.ShotInfo:
                    return new ShotInfoPacket(reader);
                case EFirearmSubPacketType.ChangeFireMode:
                    return new ChangeFireModePacket(reader);
                case EFirearmSubPacketType.ToggleAim:
                    return new ToggleAimPacket(reader);
                case EFirearmSubPacketType.ExamineWeapon:
                    return new ExamineWeaponPacket();
                case EFirearmSubPacketType.CheckAmmo:
                    return new CheckAmmoPacket();
                case EFirearmSubPacketType.CheckChamber:
                    return new CheckChamberPacket();
                case EFirearmSubPacketType.CheckFireMode:
                    return new CheckFireModePacket();
                case EFirearmSubPacketType.ToggleLightStates:
                    return new LightStatesPacket(reader);
                case EFirearmSubPacketType.ToggleScopeStates:
                    return new ScopeStatesPacket(reader);
                case EFirearmSubPacketType.ToggleLauncher:
                    return new ToggleLauncherPacket();
                case EFirearmSubPacketType.ToggleInventory:
                    return new ToggleInventoryPacket(reader);
                case EFirearmSubPacketType.Loot:
                    return new FirearmLootPacket();
                case EFirearmSubPacketType.ReloadMag:
                    return new ReloadMagPacket(reader);
                case EFirearmSubPacketType.QuickReloadMag:
                    return new QuickReloadMagPacket(reader);
                case EFirearmSubPacketType.ReloadWithAmmo:
                    return new ReloadWithAmmoPacket(reader);
                case EFirearmSubPacketType.CylinderMag:
                    return new CylinderMagPacket(reader);
                case EFirearmSubPacketType.ReloadLauncher:
                    return new ReloadLauncherPacket(reader);
                case EFirearmSubPacketType.ReloadBarrels:
                    return new ReloadBarrelsPacket(reader);
                case EFirearmSubPacketType.Grenade:
                    return new GrenadePacket(reader);
                case EFirearmSubPacketType.CancelGrenade:
                    return new CancelGrenadePacket();
                case EFirearmSubPacketType.CompassChange:
                    return new CompassChangePacket(reader);
                case EFirearmSubPacketType.Knife:
                    return new KnifePacket(reader);
                case EFirearmSubPacketType.FlareShot:
                    return new FlareShotPacket(reader);
                case EFirearmSubPacketType.RocketShot:
                    return new RocketShotPacket(reader);
                case EFirearmSubPacketType.ReloadBoltAction:
                    return new ReloadBoltActionPacket();
                case EFirearmSubPacketType.RollCylinder:
                    return new RollCylinderPacket(reader);
                case EFirearmSubPacketType.UnderbarrelSightingRangeUp:
                    return new UnderbarrelSightingRangeUpPacket();
                case EFirearmSubPacketType.UnderbarrelSightingRangeDown:
                    return new UnderbarrelSightingRangeDownPacket();
                case EFirearmSubPacketType.ToggleBipod:
                    return new ToggleBipodPacket();
                case EFirearmSubPacketType.LeftStanceChange:
                    return new LeftStanceChangePacket(reader);
                default:
                    FikaPlugin.Instance.FikaLogger.LogError("GetFirearmSubPacket: type was outside of bounds!");
                    return null;
            }
        }

        public static ISubPacket GetCommonSubPacket(this NetDataReader reader, ECommonSubPacketType type)
        {
            switch (type)
            {
                case ECommonSubPacketType.Phrase:
                    return new PhrasePacket(reader);
                case ECommonSubPacketType.WorldInteraction:
                    return new WorldInteractionPacket(reader);
                case ECommonSubPacketType.ContainerInteraction:
                    return new ContainerInteractionPacket(reader);
                case ECommonSubPacketType.Proceed:
                    return new ProceedPacket(reader);
                case ECommonSubPacketType.HeadLights:
                    return new HeadLightsPacket(reader);
                case ECommonSubPacketType.InventoryChanged:
                    return new InventoryChangedPacket(reader);
                case ECommonSubPacketType.Drop:
                    return new DropPacket(reader);
                case ECommonSubPacketType.Stationary:
                    return new StationaryPacket(reader);
                case ECommonSubPacketType.Vault:
                    return new VaultPacket(reader);
                case ECommonSubPacketType.Interaction:
                    return new InteractionPacket(reader);
                case ECommonSubPacketType.Mounting:
                    return new MountingPacket(reader);
                default:
                    FikaPlugin.Instance.FikaLogger.LogError("GetCommonSubPacket: type was outside of bounds!");
                    return null;
            }
        }

        public static ISubPacket GetGenericSubPacket(this NetDataReader reader, EGenericSubPacketType type, int netId)
        {
            switch (type)
            {
                case EGenericSubPacketType.ClientExtract:
                    return new ClientExtract(netId);
                case EGenericSubPacketType.ClientConnected:
                    return new ClientConnected(reader);
                case EGenericSubPacketType.ClientDisconnected:
                    return new ClientDisconnected(reader);
                case EGenericSubPacketType.ExfilCountdown:
                    return new ExfilCountdown(reader);
                case EGenericSubPacketType.ClearEffects:
                    return new ClearEffects(netId);
                case EGenericSubPacketType.UpdateBackendData:
                    return new UpdateBackendData(reader);
                case EGenericSubPacketType.SecretExfilFound:
                    return new SecretExfilFound(reader);
                case EGenericSubPacketType.BorderZone:
                    return new BorderZoneEvent(reader);
                case EGenericSubPacketType.Mine:
                    return new MineEvent(reader);
                case EGenericSubPacketType.DisarmTripwire:
                    return new DisarmTripwire(reader);
                case EGenericSubPacketType.MuffledState:
                    return new MuffledState(reader);
                case EGenericSubPacketType.SpawnBTR:
                    return new BtrSpawn(reader);
                default:
                    FikaPlugin.Instance.FikaLogger.LogError("GetGenericSubPacket: type was outside of bounds!");
                    return null;
            }
        }

        public static IRequestPacket GetRequestSubPacket(this NetDataReader reader, ERequestSubPacketType type)
        {
            switch (type)
            {
                case ERequestSubPacketType.SpawnPoint:
                    return new SpawnPointRequest(reader);
                case ERequestSubPacketType.Weather:
                    return new WeatherRequest(reader);
                case ERequestSubPacketType.Exfiltration:
                    return new ExfiltrationRequest(reader);
                case ERequestSubPacketType.TraderServices:
                    return new TraderServicesRequest(reader);
                case ERequestSubPacketType.CharacterSync:
                    return new RequestCharactersPacket(reader);
                default:
                    FikaPlugin.Instance.FikaLogger.LogError("GetRequestSubPacket: type was outside of bounds!");
                    return null;
            }
        }
    }

    /// <summary>
    /// Specifies the number of bits used for packing a floating-point value.
    /// </summary>
    /// <remarks>
    /// This enum is used to define the precision of the bit-packed float representation. <br/>
    /// Higher bit counts provide greater precision but use more bandwidth.
    /// </remarks>
    public enum EFloatCompression : byte
    {
        /// <summary>
        /// Pack the float using 8 bits (1 byte). <br/>
        /// Provides lower precision but reduces bandwidth usage.
        /// </summary>
        High = 8,

        /// <summary>
        /// Pack the float using 16 bits (2 bytes). <br/>
        /// Provides higher precision than 8 bits but uses more bandwidth.
        /// </summary>
        Low = 16
    }
}