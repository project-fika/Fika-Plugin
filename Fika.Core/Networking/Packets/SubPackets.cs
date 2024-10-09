// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using EFT.Vaulting;
using System;
using UnityEngine;

namespace Fika.Core.Networking.Packets
{
	/// <summary>
	/// Class containing several static methods to serialize/deserialize sub-packages
	/// </summary>
	public class SubPackets
	{
		public struct PlayerInfoPacket(Profile profile, MongoID? controllerId, ushort firstOperationId)
		{
			public Profile Profile = profile;
			public byte[] HealthByteArray = [];
			public MongoID? ControllerId = controllerId;
			public ushort FirstOperationId = firstOperationId;
			public EHandsControllerType ControllerType;
			public string ItemId;
			public bool IsStationary;
		}

		public struct ItemControllerExecutePacket
		{
			public uint CallbackId;
			public byte[] OperationBytes;
		}

		public struct WorldInteractionPacket
		{
			public string InteractiveId;
			public EInteractionType InteractionType;
			public EInteractionStage InteractionStage;
			public string ItemId;
		}

		public struct ContainerInteractionPacket
		{
			public string InteractiveId;
			public EInteractionType InteractionType;
		}

		public struct HeadLightsPacket
		{
			public int Amount;
			public bool IsSilent;
			public FirearmLightStateStruct[] LightStates;
		}

		public struct ProceedPacket()
		{
			public EProceedType ProceedType;
			public string ItemId = string.Empty;
			public float Amount = 0f;
			public int AnimationVariant = 0;
			public bool Scheduled = false;
			public EBodyPart BodyPart = EBodyPart.Common;
		}

		public struct DropPacket
		{
			public bool FastDrop;
		}

		public struct StationaryPacket
		{
			public EStationaryCommand Command;
			public string Id;
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

		public struct VaultPacket
		{
			public EVaultingStrategy VaultingStrategy;
			public Vector3 VaultingPoint;
			public float VaultingHeight;
			public float VaultingLength;
			public float VaultingSpeed;
			public float BehindObstacleHeight;
			public float AbsoluteForwardVelocity;
		}

		public struct CorpseSyncPacket
		{
			public EBodyPartColliderType BodyPartColliderType;
			public Vector3 Direction;
			public Vector3 Point;
			public float Force;
			public Vector3 OverallVelocity;
			public InventoryEquipment Equipment;
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

		public struct MountingPacket(GStruct179.EMountingCommand command)
		{
			public GStruct179.EMountingCommand Command = command;
			public bool IsMounted;
			public Vector3 MountDirection;
			public Vector3 MountingPoint;
			public Vector3 TargetPos;
			public float TargetPoseLevel;
			public float TargetHandsRotation;
			public Vector2 PoseLimit;
			public Vector2 PitchLimit;
			public Vector2 YawLimit;
			public Quaternion TargetBodyRotation;
			public float CurrentMountingPointVerticalOffset;
			public short MountingDirection;
			public float TransitionTime;
		}

		public enum EStationaryCommand
		{
			Occupy,
			Leave,
			Denied
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
			UsableItem,
			Weapon,
			Stationary,
			Knife,
			TryProceed
		}
	}
}
