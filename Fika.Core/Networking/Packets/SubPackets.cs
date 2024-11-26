// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using System;
using UnityEngine;

namespace Fika.Core.Networking.Packets
{
	/// <summary>
	/// Class containing several static methods to serialize/deserialize sub-packages
	/// </summary>
	public class SubPackets
	{
		public struct PlayerInfoPacket
		{
			public Profile Profile;
			public byte[] HealthByteArray;
			public MongoID? ControllerId;
			public ushort FirstOperationId;
			public EHandsControllerType ControllerType;
			public string ItemId;
			public bool IsStationary;
			public bool IsZombie;
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
		}

		public struct CorpseSyncPacket
		{
			public EBodyPartColliderType BodyPartColliderType;
			public Vector3 Direction;
			public Vector3 Point;
			public float Force;
			public Vector3 OverallVelocity;
			public GClass1659 InventoryDescriptor;
			public EquipmentSlot ItemSlot;
			public Item ItemInHands;
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
		}
	}
}
