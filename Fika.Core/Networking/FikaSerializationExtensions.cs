using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using static BasePhysicalClass; // Physical struct

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
			GClass1164 eftWriter = new();
			GClass1608 descriptor = GClass1634.SerializeItem(item, GClass1872.Instance);
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
			GClass1159 eftReader = new(reader.GetByteArray());
			return GClass1634.DeserializeItem(Singleton<ItemFactoryClass>.Instance, [], eftReader.ReadEFTItemDescriptor());
		}

		public static Item GetAirdropItem(this NetDataReader reader)
		{
			GClass1159 eftReader = new(reader.GetByteArray());
			Item item = GClass1634.DeserializeItem(Singleton<ItemFactoryClass>.Instance, [], eftReader.ReadEFTItemDescriptor());

			GClass1281 enumerable = [new LootItemPositionClass()];
			enumerable[0].Item = item;
			Item[] array = enumerable.Select(AirdropSynchronizableObject.Class1919.class1919_0.method_0).ToArray();
			ResourceKey[] resourceKeys = array.OfType<GClass2876>().GetAllItemsFromCollections()
				.Concat(array.Where(AirdropSynchronizableObject.Class1919.class1919_0.method_1))
				.SelectMany(AirdropSynchronizableObject.Class1919.class1919_0.method_2)
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
		/// Serializes a <see cref="List{WorldInteractiveObject.GStruct388}"/> of <see cref="WorldInteractiveObject"/> data
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="interactiveObjectsData"></param>
		public static void PutInteractivesStates(this NetDataWriter writer, List<WorldInteractiveObject.GStruct388> interactiveObjectsData)
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
		/// Deserializes a <see cref="List{WorldInteractiveObject.GStruct388}"/> of <see cref="WorldInteractiveObject"/> data
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="List{T}"/> of <see cref="WorldInteractiveObject.GStruct388"/></returns>
		public static List<WorldInteractiveObject.GStruct388> GetInteractivesStates(this NetDataReader reader)
		{
			int amount = reader.GetInt();
			List<WorldInteractiveObject.GStruct388> interactivesStates = new(amount);
			for (int i = 0; i < amount; i++)
			{
				WorldInteractiveObject.GStruct388 data = new()
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
				writer.Put(0);
				return;
			}
			writer.Put(1);
			writer.Put(mongoId.Value.ToString());
		}

		/// <summary>
		/// Deserializes a <see cref="MongoID"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A new <see cref="MongoID"/>? (nullable)</returns>
		public static MongoID? GetMongoID(this NetDataReader reader)
		{
			int value = reader.GetInt();
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

			foreach (KeyValuePair<EBodyPart, Profile.ProfileHealthClass.GClass1861> bodyPart in health.BodyParts)
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
	}
}