using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using Fika.Core.Coop.Utils;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static BasePhysicalClass;
using static Fika.Core.Networking.CommonSubPackets;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.Packets.GameWorld.GenericSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;
using static Fika.Core.Networking.Packets.SubPackets;

namespace Fika.Core.Networking
{
	/// <summary>
	/// Serialization extensions for Unity/EFT classes to ease writing of packets in Fika
	/// </summary>
	public static class FikaSerializationExtensions
	{
		/// <summary>
		/// Serializes a <see cref="Vector3"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="vector"></param>
		public static void Put(this NetDataWriter writer, Vector3 vector)
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
		public static void Put(this NetDataWriter writer, Vector2 vector)
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
		public static void Put(this NetDataWriter writer, Quaternion quaternion)
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
		public static void Put(this NetDataWriter writer, Color color)
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
		/// Serializes a <see cref="GStruct36"/> (Physical) struct
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="physical"></param>
		public static void Put(this NetDataWriter writer, GStruct36 physical)
		{
			writer.Put(physical.StaminaExhausted);
			writer.Put(physical.OxygenExhausted);
			writer.Put(physical.HandsExhausted);
		}

		/// <summary>
		/// Deserializes a <see cref="GStruct36"/> (Physical) struct
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="GStruct36"/> (Physical)</returns>
		public static GStruct36 GetPhysical(this NetDataReader reader)
		{
			return new GStruct36() { StaminaExhausted = reader.GetBool(), OxygenExhausted = reader.GetBool(), HandsExhausted = reader.GetBool() };
		}

		/// <summary>
		/// Serialize a <see cref="byte"/> array
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bytes"></param>
		public static void PutByteArray(this NetDataWriter writer, byte[] bytes)
		{
			writer.Put(bytes.Length);
			if (bytes.Length > 0)
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
			if (length > 0)
			{
				byte[] bytes = new byte[length];
				reader.GetBytes(bytes, length);
				return bytes;
			}
			return [];
		}

		/// <summary>
		/// Serializes a <see cref="DateTime"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="dateTime"></param>
		public static void Put(this NetDataWriter writer, DateTime dateTime)
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
			GClass1198 eftWriter = new();
			GClass1659 descriptor = GClass1685.SerializeItem(item, FikaGlobals.SearchControllerSerializer);
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
			GClass1193 eftReader = new(reader.GetByteArray());
			return GClass1685.DeserializeItem(eftReader.ReadEFTItemDescriptor(), Singleton<ItemFactoryClass>.Instance, []);
		}

		/// <summary>
		/// Reads an <see cref="InventoryEquipment"/> serialized from <see cref="PutItem(NetDataWriter, Item)"/> and converts it into an <see cref="Inventory"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>An <see cref="Inventory"/></returns>
		public static Inventory GetInventoryFromEquipment(this NetDataReader reader)
		{
			GClass1193 eftReader = new(reader.GetByteArray());
			return new GClass1651()
			{
				Equipment = eftReader.ReadEFTItemDescriptor()
			}.ToInventory();
		}

		public static void PutItemDescriptor(this NetDataWriter writer, GClass1659 descriptor)
		{
			GClass1198 eftWriter = new();
			eftWriter.WriteEFTItemDescriptor(descriptor);
			writer.PutByteArray(eftWriter.ToArray());
		}

		public static GClass1659 GetItemDescriptor(this NetDataReader reader)
		{
			GClass1193 eftReader = new(reader.GetByteArray());
			return eftReader.ReadEFTItemDescriptor();
		}

		public static Item GetAirdropItem(this NetDataReader reader)
		{
			GClass1193 eftReader = new(reader.GetByteArray());
			Item item = GClass1685.DeserializeItem(eftReader.ReadEFTItemDescriptor(), Singleton<ItemFactoryClass>.Instance, []);

			GClass1315 enumerable = [new LootItemPositionClass()];
			enumerable[0].Item = item;
			Item[] array = enumerable.Select(FikaGlobals.GetLootItemPositionItem).ToArray();
			ResourceKey[] resourceKeys = array.OfType<GClass2981>().GetAllItemsFromCollections()
				.Concat(array.Where(AirdropSynchronizableObject.Class1967.class1967_0.method_1))
				.SelectMany(AirdropSynchronizableObject.Class1967.class1967_0.method_2)
				.ToArray();
			Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid, PoolManager.AssemblyType.Online,
				resourceKeys, JobPriority.Immediate, null, default).HandleExceptions();

			return item;
		}

		/// <summary>
		/// Serializes a <see cref="List{T}"/> of <see cref="GStruct35"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="throwables"></param>
		public static void PutThrowableData(this NetDataWriter writer, List<GStruct35> throwables)
		{
			writer.Put(throwables.Count);
			foreach (GStruct35 data in throwables)
			{
				writer.Put(data.Id);
				writer.Put(data.Position);
				writer.Put(data.Template);
				writer.Put(data.Time);
				writer.Put(data.Orientation);
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
			GClass1198 eftWriter = new();
			eftWriter.WriteEFTProfileDescriptor(new(profile, FikaGlobals.SearchControllerSerializer));
			writer.PutByteArray(eftWriter.ToArray());
		}

		/// <summary>
		/// Deserializes a <see cref="Profile"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="Profile"/></returns>
		public static Profile GetProfile(this NetDataReader reader)
		{
			GClass1193 eftReader = new(reader.GetByteArray());
			return new(eftReader.ReadEFTProfileDescriptor());
		}

		/// <summary>
		/// Deserializes a <see cref="List{T}"/> of <see cref="GStruct35"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="List{T}"/> of <see cref="GStruct35"/></returns>
		public static List<GStruct35> GetThrowableData(this NetDataReader reader)
		{
			int amount = reader.GetInt();
			List<GStruct35> throwables = new(amount);
			for (int i = 0; i < amount; i++)
			{
				GStruct35 data = new()
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
		/// Serializes a <see cref="List{WorldInteractiveObject.GStruct415}"/> of <see cref="WorldInteractiveObject"/> data
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="interactiveObjectsData"></param>
		public static void PutInteractivesStates(this NetDataWriter writer, List<WorldInteractiveObject.GStruct415> interactiveObjectsData)
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
		/// Deserializes a <see cref="List{WorldInteractiveObject.GStruct415}"/> of <see cref="WorldInteractiveObject"/> data
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="List{T}"/> of <see cref="WorldInteractiveObject.GStruct415"/></returns>
		public static List<WorldInteractiveObject.GStruct415> GetInteractivesStates(this NetDataReader reader)
		{
			int amount = reader.GetInt();
			List<WorldInteractiveObject.GStruct415> interactivesStates = new(amount);
			for (int i = 0; i < amount; i++)
			{
				WorldInteractiveObject.GStruct415 data = new()
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
				writer.Put(windowBreakerState.Value);
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
			if (!mongoId.HasValue)
			{
				writer.Put((byte)0);
				return;
			}
			writer.Put((byte)1);
			writer.Put(mongoId.Value.ToString());
		}

		/// <summary>
		/// Deserializes a <see cref="MongoID"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A new <see cref="MongoID"/>? (nullable)</returns>
		public static MongoID? GetMongoID(this NetDataReader reader)
		{
			byte value = reader.GetByte();
			if (value == 0)
			{
				return null;
			}
			return new(reader.GetString());
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

			foreach (KeyValuePair<EBodyPart, Profile.ProfileHealthClass.GClass1940> bodyPart in health.BodyParts)
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
		/// Serializes a <see cref="GStruct130"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="artilleryStruct"></param>
		public static void PutArtilleryStruct(this NetDataWriter writer, GStruct130 artilleryStruct)
		{
			writer.Put(artilleryStruct.id);
			writer.Put(artilleryStruct.position);
			writer.Put(artilleryStruct.explosion);
		}

		/// <summary>
		/// Deserializes a <see cref="GStruct130"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="GStruct130"/> with data</returns>
		public static GStruct130 GetArtilleryStruct(this NetDataReader reader)
		{
			return new()
			{
				id = reader.GetInt(),
				position = reader.GetVector3(),
				explosion = reader.GetBool()
			};
		}

		/// <summary>
		/// Serializes a <see cref="GStruct131"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="grenadeStruct"></param>
		public static void PutGrenadeStruct(this NetDataWriter writer, GStruct131 grenadeStruct)
		{
			writer.Put(grenadeStruct.Id);
			writer.Put(grenadeStruct.Position);
			writer.Put(grenadeStruct.Rotation);
			writer.Put(grenadeStruct.CollisionNumber);
			writer.Put(grenadeStruct.Done);
			if (!grenadeStruct.Done)
			{
				writer.Put(grenadeStruct.Velocity);
				writer.Put(grenadeStruct.AngularVelocity);
			}
		}

		/// <summary>
		/// Deserializes a <see cref="GStruct131"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="GStruct131"/> with data</returns>
		public static GStruct131 GetGrenadeStruct(this NetDataReader reader)
		{
			GStruct131 grenadeStruct = new()
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
			writer.Put(packet.Direction);
			writer.Put(packet.Point);
			writer.Put(packet.Force);
			writer.Put(packet.OverallVelocity);
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
			writer.Put(packet.Time);
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

		public static void PutRagdollStruct(this NetDataWriter writer, GStruct129 packet)
		{
			writer.Put(packet.Id);
			writer.Put(packet.Position);
			writer.Put(packet.Done);

			if (packet.Done && packet.TransformSyncs != null)
			{
				GStruct107[] transforms = packet.TransformSyncs;
				for (int i = 0; i < 12; i++)
				{
					writer.Put(transforms[i].Position);
					writer.Put(transforms[i].Rotation);
				}
			}
		}

		public static GStruct129 GetRagdollStruct(this NetDataReader reader)
		{
			GStruct129 packet = new()
			{
				Id = reader.GetInt(),
				Position = reader.GetVector3(),
				Done = reader.GetBool()
			};

			if (packet.Done)
			{
				packet.TransformSyncs = new GStruct107[12];
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
					FikaPlugin.Instance.FikaLogger.LogError("IFirearmSubPacket: type was outside of bounds!");
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

		public static void PutCommonSubPacket(this NetDataWriter writer, ISubPacket packet)
		{
			packet.Serialize(writer);
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
					break;
			}
			return null;
		}

		public static ISubPacket GetGenericSubPacket(this NetDataReader reader, EGenericSubPacketType type, int netId)
		{
			switch (type)
			{
				case EGenericSubPacketType.ClientExtract:
					return new ClientExtract(netId);
				case EGenericSubPacketType.ExfilCountdown:
					return new ExfilCountdown(reader);
				case EGenericSubPacketType.ClearEffects:
					return new ClearEffects(netId);
				case EGenericSubPacketType.UpdateBackendData:
					return new UpdateBackendData(reader);
				default:
					FikaPlugin.Instance.FikaLogger.LogError("GetGenericSubPacket: type was outside of bounds!");
					break;
			}
			return null;
		}
	}
}