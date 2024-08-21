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
			return Array.Empty<byte>();
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
		/// Same as <see cref="PutItem(NetDataWriter, Item)"/>, however this one is specifically for airdrops to handle bundle loading
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="item">The <see cref="Item"/> to serialize</param>
		public static void PutAirdropItem(this NetDataWriter writer, Item item)
		{
			using MemoryStream memoryStream = new();
			using BinaryWriter binaryWriter = new(memoryStream);
			binaryWriter.Write(GClass1535.SerializeItem(item));
			writer.PutByteArray(memoryStream.ToArray());
		}

		/// <summary>
		/// Same as <see cref="GetItem(NetDataReader)"/>, however this one is specifically for airdrops to handle bundle loading
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>An <see cref="Item"/></returns>
		public static Item GetAirdropItem(this NetDataReader reader)
		{
			using MemoryStream memoryStream = new(reader.GetByteArray());
			using BinaryReader binaryReader = new(memoryStream);

			Item item = GClass1535.DeserializeItem(Singleton<ItemFactory>.Instance, [], binaryReader.ReadEFTItemDescriptor());

			ContainerCollection[] containerCollections = [item as ContainerCollection];
			ResourceKey[] resourceKeys = containerCollections.GetAllItemsFromCollections()
				.Concat(containerCollections.Where(new Func<Item, bool>(AirdropSynchronizableObject.Class1832.class1832_0.method_2)))
				.SelectMany(new Func<Item, IEnumerable<ResourceKey>>(AirdropSynchronizableObject.Class1832.class1832_0.method_3))
				.ToArray();
			Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid, PoolManager.AssemblyType.Online, resourceKeys, JobPriority.Immediate, null, default);

			return item;
		}

		/// <summary>
		/// This write and serializes an <see cref="Item"/>, which can be cast to different types of inherited classes. Casting should be handled inside packet for consistency.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="item">The <see cref="Item"/> to serialize</param>
		public static void PutItem(this NetDataWriter writer, Item item)
		{
			using MemoryStream memoryStream = new();
			using BinaryWriter binaryWriter = new(memoryStream);
			binaryWriter.Write(GClass1535.SerializeItem(item));
			writer.PutByteArray(memoryStream.ToArray());
		}

		/// <summary>
		/// Gets a serialized <see cref="Item"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>An <see cref="Item"/></returns>
		public static Item GetItem(this NetDataReader reader)
		{
			using MemoryStream memoryStream = new(reader.GetByteArray());
			using BinaryReader binaryReader = new(memoryStream);

			return GClass1535.DeserializeItem(Singleton<ItemFactory>.Instance, [], binaryReader.ReadEFTItemDescriptor());
		}

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

		public static void PutInteractivesStates(this NetDataWriter writer, List<WorldInteractiveObject.GStruct384> interactiveObjectsData)
		{
			writer.Put(interactiveObjectsData.Count);
			for (int i = 0; i < interactiveObjectsData.Count; i++)
			{
				writer.Put(interactiveObjectsData[i].NetId);
				writer.Put(interactiveObjectsData[i].State);
				writer.Put(interactiveObjectsData[i].IsBroken);
			}
		}

		public static List<WorldInteractiveObject.GStruct384> GetInteractivesStates(this NetDataReader reader)
		{
			int amount = reader.GetInt();
			List<WorldInteractiveObject.GStruct384> interactivesStates = new(amount);
			for (int i = 0; i < amount; i++)
			{
				WorldInteractiveObject.GStruct384 data = new()
				{
					NetId = reader.GetInt(),
					State = reader.GetByte(),
					IsBroken = reader.GetBool()
				};
				interactivesStates.Add(data);
			}

			return interactivesStates;
		}

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

		public static MongoID? GetMongoID(this NetDataReader reader)
		{
			int value = reader.GetInt();
			if (value == 0)
			{
				return null;
			}
			return new(reader.GetString());
		}
	}
}
