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

		public struct LightStatesPacket
		{
			public int Amount;
			public FirearmLightStateStruct[] LightStates;
		}

		public struct HeadLightsPacket
		{
			public int Amount;
			public bool IsSilent;
			public FirearmLightStateStruct[] LightStates;
		}

		public struct ScopeStatesPacket
		{
			public int Amount;
			public FirearmScopeStateStruct[] FirearmScopeStateStruct;
		}

		public struct ReloadMagPacket
		{
			public bool Reload;
			public string MagId;
			public byte[] LocationDescription;
		}

		public struct QuickReloadMagPacket
		{
			public bool Reload;
			public string MagId;
		}

		public struct ReloadWithAmmoPacket
		{
			public bool Reload;
			public EReloadWithAmmoStatus Status;
			public int AmmoLoadedToMag;
			public string[] AmmoIds;
		}

		public struct CylinderMagPacket
		{
			public bool Changed;
			public int CamoraIndex;
			public bool HammerClosed;
		}

		public struct ReloadLauncherPacket
		{
			public bool Reload;
			public string[] AmmoIds;
		}

		public struct ReloadBarrelsPacket
		{
			public bool Reload;
			public string[] AmmoIds;
			public byte[] LocationDescription;
		}

		public struct GrenadePacket()
		{
			public GrenadePacketType PacketType;
			public bool HasGrenade = false;
			public Quaternion GrenadeRotation;
			public Vector3 GrenadePosition;
			public Vector3 ThrowForce;
			public bool LowThrow;
			public bool PlantTripwire = false;
			public bool ChangeToIdle = false;
			public bool ChangeToPlant = false;
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

		public struct KnifePacket()
		{
			public bool Examine = false;
			public bool Kick = false;
			public bool AltKick = false;
			public bool BreakCombo = false;
		}

		public struct ShotInfoPacket()
		{

			public EShotType ShotType = EShotType.Unknown;
			public Vector3 ShotPosition = Vector3.zero;
			public Vector3 ShotDirection = Vector3.zero;
			public int ChamberIndex = 0;
			public float Overheat = 0f;
			public bool UnderbarrelShot = false;
			public string AmmoTemplate;
			public float LastShotOverheat;
			public float LastShotTime;
			public bool SlideOnOverheatReached;
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

		public struct FlareShotPacket
		{
			public bool StartOneShotFire;
			public Vector3 ShotPosition;
			public Vector3 ShotForward;
			public string AmmoTemplateId;
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

		public enum GrenadePacketType
		{
			None,
			ExamineWeapon,
			HighThrow,
			LowThrow,
			PullRingForHighThrow,
			PullRingForLowThrow
		};

		public enum EStationaryCommand
		{
			Occupy,
			Leave,
			Denied
		}

		public enum EReloadWithAmmoStatus
		{
			None = 0,
			StartReload,
			EndReload,
			AbortReload
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
