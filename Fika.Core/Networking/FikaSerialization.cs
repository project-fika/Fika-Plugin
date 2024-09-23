// © 2024 Lacyway All Rights Reserved

using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.InventoryLogic;
using EFT.Vaulting;
using LiteNetLib.Utils;
using System;
using UnityEngine;

namespace Fika.Core.Networking
{
	/// <summary>
	/// Class containing several static methods to serialize/deserialize sub-packages
	/// </summary>
	public class FikaSerialization
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

			public static void Serialize(NetDataWriter writer, PlayerInfoPacket packet)
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

			public static PlayerInfoPacket Deserialize(NetDataReader reader)
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
		}

		public struct LightStatesPacket
		{
			public int Amount;
			public FirearmLightStateStruct[] LightStates;

			public static LightStatesPacket Deserialize(NetDataReader reader)
			{
				LightStatesPacket packet = new()
				{
					Amount = reader.GetInt()
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
			public bool IsSilent;
			public FirearmLightStateStruct[] LightStates;
			public static HeadLightsPacket Deserialize(NetDataReader reader)
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

			public static void Serialize(NetDataWriter writer, HeadLightsPacket packet)
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
		}

		public struct ScopeStatesPacket
		{
			public int Amount;
			public FirearmScopeStateStruct[] FirearmScopeStateStruct;
			public static ScopeStatesPacket Deserialize(NetDataReader reader)
			{
				ScopeStatesPacket packet = new();
				packet.Amount = reader.GetInt();
				if (packet.Amount > 0)
				{
					packet.FirearmScopeStateStruct = new FirearmScopeStateStruct[packet.Amount];
					for (int i = 0; i < packet.Amount; i++)
					{
						packet.FirearmScopeStateStruct[i] = new()
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
						writer.Put(packet.FirearmScopeStateStruct[i].Id);
						writer.Put(packet.FirearmScopeStateStruct[i].ScopeMode);
						writer.Put(packet.FirearmScopeStateStruct[i].ScopeIndexInsideSight);
						writer.Put(packet.FirearmScopeStateStruct[i].ScopeCalibrationIndex);
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
				CylinderMagPacket packet = new();
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
			public bool PlantTripwire = false;
			public bool ChangeToIdle = false;
			public bool ChangeToPlant = false;

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
				packet.PlantTripwire = reader.GetBool();
				packet.ChangeToIdle = reader.GetBool();
				packet.ChangeToPlant = reader.GetBool();
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
				writer.Put(packet.PlantTripwire);
				writer.Put(packet.ChangeToIdle);
				writer.Put(packet.ChangeToPlant);
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
					OperationBytes = reader.GetByteArray()
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
				{
					packet.ItemId = reader.GetString();
				}

				return packet;
			}

			public static void Serialize(NetDataWriter writer, WorldInteractionPacket packet)
			{
				writer.Put(packet.InteractiveId);
				writer.Put((byte)packet.InteractionType);
				writer.Put((byte)packet.InteractionStage);
				if (packet.InteractionType == EInteractionType.Unlock)
				{
					writer.Put(packet.ItemId);
				}
			}
		}

		public struct ContainerInteractionPacket
		{
			public string InteractiveId;
			public EInteractionType InteractionType;

			public static ContainerInteractionPacket Deserialize(NetDataReader reader)
			{
				ContainerInteractionPacket packet = new()
				{
					InteractiveId = reader.GetString(),
					InteractionType = (EInteractionType)reader.GetInt()
				};
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
			public string ItemId = string.Empty;
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
				writer.Put(packet.Amount);
				writer.Put(packet.AnimationVariant);
				writer.Put(packet.Scheduled);
				writer.Put((int)packet.BodyPart);
			}

		}

		public struct DropPacket
		{
			public bool FastDrop;

			public static DropPacket Deserialize(NetDataReader reader)
			{
				return new DropPacket
				{
					FastDrop = reader.GetBool()
				};
			}
			public static void Serialize(NetDataWriter writer, DropPacket packet)
			{
				writer.Put(packet.FastDrop);
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
				{
					packet.Id = reader.GetString();
				}

				return packet;
			}
			public static void Serialize(NetDataWriter writer, StationaryPacket packet)
			{
				writer.Put((byte)packet.Command);
				if (packet.Command == EStationaryCommand.Occupy && !string.IsNullOrEmpty(packet.Id))
				{
					writer.Put(packet.Id);
				}
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

			public EShotType ShotType = EShotType.Unknown;
			public int AmmoAfterShot = 0;
			public Vector3 ShotPosition = Vector3.zero;
			public Vector3 ShotDirection = Vector3.zero;
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
					ShotType = (EShotType)reader.GetInt(),
					AmmoAfterShot = reader.GetInt(),
					ShotPosition = reader.GetVector3(),
					ShotDirection = reader.GetVector3(),
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
				writer.Put((int)packet.ShotType);
				writer.Put(packet.AmmoAfterShot);
				writer.Put(packet.ShotPosition);
				writer.Put(packet.ShotDirection);
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
				SearchPacket packet = new()
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
					gunsBlockRotation = reader.GetFloat(),
					turretRotation = reader.GetFloat(),
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

			public static CorpseSyncPacket Deserialize(NetDataReader reader)
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

			public static void Serialize(NetDataWriter writer, CorpseSyncPacket packet)
			{
				writer.Put((int)packet.BodyPartColliderType);
				writer.Put(packet.Direction);
				writer.Put(packet.Point);
				writer.Put(packet.Force);
				writer.Put(packet.OverallVelocity);
				writer.PutItem(packet.Equipment);
				writer.Put((byte)packet.ItemSlot);
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

		public struct MountingPacket(GStruct173.EMountingCommand command)
		{
			public GStruct173.EMountingCommand Command = command;
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

			public static MountingPacket Deserialize(NetDataReader reader)
			{
				MountingPacket packet = new()
				{
					Command = (GStruct173.EMountingCommand)reader.GetByte()
				};
				if (packet.Command == GStruct173.EMountingCommand.Update)
				{
					packet.CurrentMountingPointVerticalOffset = reader.GetFloat();
				}
				if (packet.Command is GStruct173.EMountingCommand.Enter or GStruct173.EMountingCommand.Exit)
				{
					packet.IsMounted = reader.GetBool();
				};
				if (packet.Command == GStruct173.EMountingCommand.Enter)
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

			public static void Serialize(NetDataWriter writer, MountingPacket packet)
			{
				writer.Put((byte)packet.Command);
				if (packet.Command == GStruct173.EMountingCommand.Update)
				{
					writer.Put(packet.CurrentMountingPointVerticalOffset);
				}
				if (packet.Command is GStruct173.EMountingCommand.Enter or GStruct173.EMountingCommand.Exit)
				{
					writer.Put(packet.IsMounted);
				}
				if (packet.Command == GStruct173.EMountingCommand.Enter)
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
