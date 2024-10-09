﻿using Comfort.Common;
using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.SynchronizableObjects;
using EFT.Vaulting;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static BasePhysicalClass;
using static Fika.Core.Networking.FirearmSubPackets;
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
			GClass1175 eftWriter = new();
			GClass1636 descriptor = GClass1662.SerializeItem(item, GClass1901.Instance);
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
			GClass1170 eftReader = new(reader.GetByteArray());
			return GClass1662.DeserializeItem(Singleton<ItemFactoryClass>.Instance, [], eftReader.ReadEFTItemDescriptor());
		}

		public static Item GetAirdropItem(this NetDataReader reader)
		{
			GClass1170 eftReader = new(reader.GetByteArray());
			Item item = GClass1662.DeserializeItem(Singleton<ItemFactoryClass>.Instance, [], eftReader.ReadEFTItemDescriptor());

			GClass1292 enumerable = [new LootItemPositionClass()];
			enumerable[0].Item = item;
			Item[] array = enumerable.Select(AirdropSynchronizableObject.Class1942.class1942_0.method_0).ToArray();
			ResourceKey[] resourceKeys = array.OfType<GClass2906>().GetAllItemsFromCollections()
				.Concat(array.Where(AirdropSynchronizableObject.Class1942.class1942_0.method_1))
				.SelectMany(AirdropSynchronizableObject.Class1942.class1942_0.method_2)
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
		/// Serializes a <see cref="List{WorldInteractiveObject.GStruct395}"/> of <see cref="WorldInteractiveObject"/> data
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="interactiveObjectsData"></param>
		public static void PutInteractivesStates(this NetDataWriter writer, List<WorldInteractiveObject.GStruct395> interactiveObjectsData)
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
		/// Deserializes a <see cref="List{WorldInteractiveObject.GStruct395}"/> of <see cref="WorldInteractiveObject"/> data
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="List{T}"/> of <see cref="WorldInteractiveObject.GStruct395"/></returns>
		public static List<WorldInteractiveObject.GStruct395> GetInteractivesStates(this NetDataReader reader)
		{
			int amount = reader.GetInt();
			List<WorldInteractiveObject.GStruct395> interactivesStates = new(amount);
			for (int i = 0; i < amount; i++)
			{
				WorldInteractiveObject.GStruct395 data = new()
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

			foreach (KeyValuePair<EBodyPart, Profile.ProfileHealthClass.GClass1890> bodyPart in health.BodyParts)
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
		public static void PutArtilleryStruct(this NetDataWriter writer, in GStruct130 artilleryStruct)
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
		public static void PutGrenadeStruct(this NetDataWriter writer, in GStruct131 grenadeStruct)
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
			byte[] profileBytes = SimpleZlib.CompressToBytes(packet.Profile.ToJson(), 4, null);
			writer.PutByteArray(profileBytes);
			writer.PutByteArray(packet.HealthByteArray);
			writer.PutMongoID(packet.ControllerId);
			writer.Put(packet.FirstOperationId);
			writer.Put((byte)packet.ControllerType);
			if (packet.ControllerType != EHandsControllerType.None)
			{
				writer.Put(packet.ItemId);
				writer.Put(packet.IsStationary);
			}
		}

		/// <summary>
		/// Deserializes a <see cref="PlayerInfoPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="PlayerInfoPacket"/> with data</returns>
		public static PlayerInfoPacket GetPlayerInfoPacket(this NetDataReader reader)
		{
			byte[] profileBytes = reader.GetByteArray();
			byte[] healthBytes = reader.GetByteArray();
			PlayerInfoPacket packet = new(SimpleZlib.Decompress(profileBytes, null).ParseJsonTo<Profile>(), reader.GetMongoID(), reader.GetUShort())
			{
				HealthByteArray = healthBytes,
				ControllerType = (EHandsControllerType)reader.GetByte()
			};
			if (packet.ControllerType != EHandsControllerType.None)
			{
				packet.ItemId = reader.GetString();
				packet.IsStationary = reader.GetBool();
			}
			return packet;
		}

		/// <summary>
		/// Serializes a <see cref="HeadLightsPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutHeadLightsPacket(this NetDataWriter writer, HeadLightsPacket packet)
		{
			writer.Put(packet.Amount);
			writer.Put(packet.IsSilent);
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

		/// <summary>
		/// Deserializes a <see cref="HeadLightsPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="HeadLightsPacket"/> with data</returns>
		public static HeadLightsPacket GetHeadLightsPacket(this NetDataReader reader)
		{
			HeadLightsPacket packet = new()
			{
				Amount = reader.GetInt(),
				IsSilent = reader.GetBool()
			};
			if (packet.Amount > 0)
			{
				packet.LightStates = new FirearmLightStateStruct[packet.Amount];
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

		/// <summary>
		/// Serializes a <see cref="ItemControllerExecutePacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutItemControllerExecutePacket(this NetDataWriter writer, ItemControllerExecutePacket packet)
		{
			writer.Put(packet.CallbackId);
			writer.PutByteArray(packet.OperationBytes);
		}

		/// <summary>
		/// Deserializes a <see cref="ItemControllerExecutePacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="ItemControllerExecutePacket"/> with data</returns>
		public static ItemControllerExecutePacket GetItemControllerExecutePacket(this NetDataReader reader)
		{
			ItemControllerExecutePacket packet = new()
			{
				CallbackId = reader.GetUInt(),
				OperationBytes = reader.GetByteArray()
			};
			return packet;
		}


		/// <summary>
		/// Serializes a <see cref="WorldInteractionPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutWorldInteractionPacket(this NetDataWriter writer, WorldInteractionPacket packet)
		{
			writer.Put(packet.InteractiveId);
			writer.Put((byte)packet.InteractionType);
			writer.Put((byte)packet.InteractionStage);
			if (packet.InteractionType == EInteractionType.Unlock)
			{
				writer.Put(packet.ItemId);
			}
		}

		/// <summary>
		/// Deserializes a <see cref="WorldInteractionPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="WorldInteractionPacket"/> with data</returns>
		public static WorldInteractionPacket GetWorldInteractionPacket(this NetDataReader reader)
		{
			WorldInteractionPacket packet = new()
			{
				InteractiveId = reader.GetString(),
				InteractionType = (EInteractionType)reader.GetByte(),
				InteractionStage = (EInteractionStage)reader.GetByte(),
			};
			if (packet.InteractionType == EInteractionType.Unlock)
			{
				packet.ItemId = reader.GetString();
			}
			return packet;
		}

		/// <summary>
		/// Serializes a <see cref="ContainerInteractionPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutContainerInteractionPacket(this NetDataWriter writer, ContainerInteractionPacket packet)
		{
			writer.Put(packet.InteractiveId);
			writer.Put((int)packet.InteractionType);
		}

		/// <summary>
		/// Deserializes a <see cref="ContainerInteractionPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="ContainerInteractionPacket"/> with data</returns>
		public static ContainerInteractionPacket GetContainerInteractionPacket(this NetDataReader reader)
		{
			ContainerInteractionPacket packet = new()
			{
				InteractiveId = reader.GetString(),
				InteractionType = (EInteractionType)reader.GetInt()
			};
			return packet;
		}

		/// <summary>
		/// Serializes a <see cref="ProceedPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutProceedPacket(this NetDataWriter writer, ProceedPacket packet)
		{
			writer.Put((int)packet.ProceedType);
			writer.Put(packet.ItemId);
			writer.Put(packet.Amount);
			writer.Put(packet.AnimationVariant);
			writer.Put(packet.Scheduled);
			writer.Put((int)packet.BodyPart);
		}

		/// <summary>
		/// Deserializes a <see cref="ProceedPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="ProceedPacket"/> with data</returns>
		public static ProceedPacket GetProceedPacket(this NetDataReader reader)
		{
			return new ProceedPacket
			{
				ProceedType = (EProceedType)reader.GetInt(),
				ItemId = reader.GetString(),
				Amount = reader.GetFloat(),
				AnimationVariant = reader.GetInt(),
				Scheduled = reader.GetBool(),
				BodyPart = (EBodyPart)reader.GetInt()
			};
		}

		/// <summary>
		/// Serializes a <see cref="DropPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutDropPacket(this NetDataWriter writer, DropPacket packet)
		{
			writer.Put(packet.FastDrop);
		}

		/// <summary>
		/// Deserializes a <see cref="DropPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="DropPacket"/>with data</returns>
		public static DropPacket GetDropPacket(this NetDataReader reader)
		{
			return new DropPacket
			{
				FastDrop = reader.GetBool()
			};
		}

		/// <summary>
		/// Serializes a <see cref="StationaryPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutStationaryPacket(this NetDataWriter writer, StationaryPacket packet)
		{
			writer.Put((byte)packet.Command);
			if (packet.Command == EStationaryCommand.Occupy && !string.IsNullOrEmpty(packet.Id))
			{
				writer.Put(packet.Id);
			}
		}

		/// <summary>
		/// Deserializes a <see cref="StationaryPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="StationaryPacket"/> with data</returns>
		public static StationaryPacket GetStationaryPacket(this NetDataReader reader)
		{
			StationaryPacket packet = new()
			{
				Command = (EStationaryCommand)reader.GetByte()
			};

			if (packet.Command == EStationaryCommand.Occupy)
			{
				packet.Id = reader.GetString();
			}

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
		/// Serializes a <see cref="VaultPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutVaultPacket(this NetDataWriter writer, VaultPacket packet)
		{
			writer.Put((byte)packet.VaultingStrategy);
			writer.Put(packet.VaultingPoint);
			writer.Put(packet.VaultingHeight);
			writer.Put(packet.VaultingLength);
			writer.Put(packet.VaultingSpeed);
			writer.Put(packet.BehindObstacleHeight);
			writer.Put(packet.AbsoluteForwardVelocity);
		}

		/// <summary>
		/// Deserializes a <see cref="VaultPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="VaultPacket"/> with data</returns>
		public static VaultPacket GetVaultPacket(this NetDataReader reader)
		{
			return new VaultPacket()
			{
				VaultingStrategy = (EVaultingStrategy)reader.GetByte(),
				VaultingPoint = reader.GetVector3(),
				VaultingHeight = reader.GetFloat(),
				VaultingLength = reader.GetFloat(),
				VaultingSpeed = reader.GetFloat(),
				BehindObstacleHeight = reader.GetFloat(),
				AbsoluteForwardVelocity = reader.GetFloat()
			};
		}

		/// <summary>
		/// Serializes a <see cref="BTRDataPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutBTRDataPacket(this NetDataWriter writer, BTRDataPacket packet)
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

		/// <summary>
		/// Deserializes a <see cref="BTRDataPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="BTRDataPacket"/> with data</returns>
		public static BTRDataPacket GetBTRDataPacket(this NetDataReader reader)
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
				gunsBlockRotation = reader.GetFloat(),
				turretRotation = reader.GetFloat(),
				rotation = reader.GetQuaternion()
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
			writer.PutItem(packet.Equipment);
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
				Equipment = (InventoryEquipment)reader.GetItem(),
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

		/// <summary>
		/// Serializes a <see cref="MountingPacket"/>
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="packet"></param>
		public static void PutMountingPacket(this NetDataWriter writer, MountingPacket packet)
		{
			writer.Put((byte)packet.Command);
			if (packet.Command == GStruct179.EMountingCommand.Update)
			{
				writer.Put(packet.CurrentMountingPointVerticalOffset);
			}
			if (packet.Command is GStruct179.EMountingCommand.Enter or GStruct179.EMountingCommand.Exit)
			{
				writer.Put(packet.IsMounted);
			}
			if (packet.Command == GStruct179.EMountingCommand.Enter)
			{
				writer.Put(packet.MountDirection);
				writer.Put(packet.MountingPoint);
				writer.Put(packet.MountingDirection);
				writer.Put(packet.TransitionTime);
				writer.Put(packet.TargetPos);
				writer.Put(packet.TargetPoseLevel);
				writer.Put(packet.TargetHandsRotation);
				writer.Put(packet.TargetBodyRotation);
				writer.Put(packet.PoseLimit);
				writer.Put(packet.PitchLimit);
				writer.Put(packet.YawLimit);
			}
		}

		/// <summary>
		/// Deserializes a <see cref="MountingPacket"/>
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>A <see cref="MountingPacket"/> with data</returns>
		public static MountingPacket GetMountingPacket(this NetDataReader reader)
		{
			MountingPacket packet = new()
			{
				Command = (GStruct179.EMountingCommand)reader.GetByte()
			};
			if (packet.Command == GStruct179.EMountingCommand.Update)
			{
				packet.CurrentMountingPointVerticalOffset = reader.GetFloat();
			}
			if (packet.Command is GStruct179.EMountingCommand.Enter or GStruct179.EMountingCommand.Exit)
			{
				packet.IsMounted = reader.GetBool();
			};
			if (packet.Command == GStruct179.EMountingCommand.Enter)
			{
				packet.MountDirection = reader.GetVector3();
				packet.MountingPoint = reader.GetVector3();
				packet.MountingDirection = reader.GetShort();
				packet.TransitionTime = reader.GetFloat();
				packet.TargetPos = reader.GetVector3();
				packet.TargetPoseLevel = reader.GetFloat();
				packet.TargetHandsRotation = reader.GetFloat();
				packet.TargetBodyRotation = reader.GetQuaternion();
				packet.PoseLimit = reader.GetVector2();
				packet.PitchLimit = reader.GetVector2();
				packet.YawLimit = reader.GetVector2();
			}
			return packet;
		}

		public static void PutFirearmSubPacket(this NetDataWriter writer, IFirearmSubPacket packet, EFirearmSubPacketType type)
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

		public static IFirearmSubPacket GetFirearmSubPacket(this NetDataReader reader, EFirearmSubPacketType type)
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
					FikaPlugin.Instance.FikaLogger.LogError("IFirearmSubPacket: type was outside of bounds!");
					return null;
			}
		}
	}
}