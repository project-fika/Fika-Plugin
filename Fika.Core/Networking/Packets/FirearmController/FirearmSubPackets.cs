using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.Players;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Networking
{
	public class FirearmSubPackets
	{
		public struct ToggleAimPacket : ISubPacket
		{
			public int AimingIndex;

			public ToggleAimPacket(NetDataReader reader)
			{
				AimingIndex = reader.GetInt();
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.SetAim(AimingIndex);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(AimingIndex);
			}
		}

		public struct ChangeFireModePacket(NetDataReader reader) : ISubPacket
		{
			public Weapon.EFireMode FireMode = (Weapon.EFireMode)reader.GetByte();

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.ChangeFireMode(FireMode);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put((byte)FireMode);
			}
		}

		public struct ExamineWeaponPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.ExamineWeapon();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct CheckAmmoPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.CheckAmmo();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct ToggleLauncherPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.ToggleLauncher();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct ToggleInventoryPacket : ISubPacket
		{
			public bool Open;

			public ToggleInventoryPacket(NetDataReader reader)
			{
				Open = reader.GetBool();
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.SetInventoryOpened(Open);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Open);
			}
		}

		public struct FirearmLootPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				player.HandsController.Loot(true);
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct CancelGrenadePacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedGrenadeController grenadeController)
				{
					grenadeController.vmethod_3();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct CompassChangePacket : ISubPacket
		{
			public bool Enabled;

			public CompassChangePacket(NetDataReader reader)
			{
				Enabled = reader.GetBool();
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is ItemHandsController handsController)
				{
					handsController.CompassState.Value = Enabled;
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Enabled);
			}
		}

		public struct LeftStanceChangePacket : ISubPacket
		{
			public bool LeftStance;

			public LeftStanceChangePacket(NetDataReader reader)
			{
				LeftStance = reader.GetBool();
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					if (player.MovementContext.LeftStanceEnabled != LeftStance)
					{
						controller.ChangeLeftStance();
					}
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(LeftStance);
			}
		}

		public struct RollCylinderPacket : ISubPacket
		{
			public bool RollToZeroCamora;

			public RollCylinderPacket(NetDataReader reader)
			{
				RollToZeroCamora = reader.GetBool();
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller && controller.Weapon is RevolverItemClass)
				{
					controller.RollCylinder(RollToZeroCamora);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(RollToZeroCamora);
			}
		}

		public struct ReloadBoltActionPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.HandleObservedBoltAction();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct CheckChamberPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.CheckChamber();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct CheckFireModePacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.CheckFireMode();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct UnderbarrelSightingRangeUpPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.UnderbarrelSightingRangeUp();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct UnderbarrelSightingRangeDownPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.UnderbarrelSightingRangeDown();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct ToggleBipodPacket : ISubPacket
		{
			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.ToggleBipod();
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				throw new NotImplementedException();
			}
		}

		public struct ShotInfoPacket : ISubPacket
		{
			public EShotType ShotType;
			public Vector3 ShotPosition;
			public Vector3 ShotDirection;
			public int ChamberIndex;
			public float Overheat;
			public bool UnderbarrelShot;
			public string AmmoTemplate;
			public float LastShotOverheat;
			public float LastShotTime;
			public bool SlideOnOverheatReached;

			public ShotInfoPacket(NetDataReader reader)
			{
				ShotType = (EShotType)reader.GetInt();
				ShotPosition = reader.GetVector3();
				ShotDirection = reader.GetVector3();
				ChamberIndex = reader.GetInt();
				Overheat = reader.GetFloat();
				UnderbarrelShot = reader.GetBool();
				AmmoTemplate = reader.GetString();
				LastShotOverheat = reader.GetFloat();
				LastShotTime = reader.GetFloat();
				SlideOnOverheatReached = reader.GetBool();
			}

			public void Execute(CoopPlayer player)
			{
				if (!player.HealthController.IsAlive)
				{
					FikaPlugin.Instance.FikaLogger.LogError("ShotInfoPacket::Execute: Player was not alive, can not process!");
					return;
				}

				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.HandleShotInfoPacket(ref this, player.InventoryController);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put((int)ShotType);
				writer.Put(ShotPosition);
				writer.Put(ShotDirection);
				writer.Put(ChamberIndex);
				writer.Put(Overheat);
				writer.Put(UnderbarrelShot);
				writer.Put(AmmoTemplate);
				writer.Put(LastShotOverheat);
				writer.Put(LastShotTime);
				writer.Put(SlideOnOverheatReached);
			}
		}

		public struct KnifePacket : ISubPacket
		{
			public bool Examine;
			public bool Kick;
			public bool AltKick;
			public bool BreakCombo;

			public KnifePacket(NetDataReader reader)
			{
				Examine = reader.GetBool();
				Kick = reader.GetBool();
				AltKick = reader.GetBool();
				BreakCombo = reader.GetBool();
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedKnifeController knifeController)
				{
					if (Examine)
					{
						knifeController.ExamineWeapon();
					}

					if (Kick)
					{
						knifeController.MakeKnifeKick();
					}

					if (AltKick)
					{
						knifeController.MakeAlternativeKick();
					}

					if (BreakCombo)
					{
						knifeController.BrakeCombo();
					}
				}
				else
				{
					FikaPlugin.Instance.FikaLogger.LogError($"KnifePacket: HandsController was not of type CoopObservedKnifeController! Was {player.HandsController.GetType().Name}");
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Examine);
				writer.Put(Kick);
				writer.Put(AltKick);
				writer.Put(BreakCombo);
			}
		}

		public struct GrenadePacket : ISubPacket
		{
			public EGrenadePacketType PacketType;
			public bool HasGrenade;
			public Quaternion GrenadeRotation;
			public Vector3 GrenadePosition;
			public Vector3 ThrowForce;
			public bool LowThrow;
			public bool PlantTripwire;
			public bool ChangeToIdle;
			public bool ChangeToPlant;

			public GrenadePacket(NetDataReader reader)
			{
				PacketType = (EGrenadePacketType)reader.GetInt();
				HasGrenade = reader.GetBool();
				if (HasGrenade)
				{
					GrenadeRotation = reader.GetQuaternion();
					GrenadePosition = reader.GetVector3();
					ThrowForce = reader.GetVector3();
					LowThrow = reader.GetBool();
				}
				PlantTripwire = reader.GetBool();
				ChangeToIdle = reader.GetBool();
				ChangeToPlant = reader.GetBool();
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedGrenadeController controller)
				{
					switch (PacketType)
					{
						case EGrenadePacketType.ExamineWeapon:
							{
								controller.ExamineWeapon();
								break;
							}
						case EGrenadePacketType.HighThrow:
							{
								controller.HighThrow();
								break;
							}
						case EGrenadePacketType.LowThrow:
							{
								controller.LowThrow();
								break;
							}
						case EGrenadePacketType.PullRingForHighThrow:
							{
								controller.PullRingForHighThrow();
								break;
							}
						case EGrenadePacketType.PullRingForLowThrow:
							{
								controller.PullRingForLowThrow();
								break;
							}
					}
					if (HasGrenade)
					{
						controller.SpawnGrenade(0f, GrenadePosition, GrenadeRotation, ThrowForce, LowThrow);
					}

					if (PlantTripwire)
					{
						controller.PlantTripwire();
					}

					if (ChangeToIdle)
					{
						controller.ChangeFireMode(Weapon.EFireMode.grenadeThrowing);
					}

					if (ChangeToPlant)
					{
						controller.ChangeFireMode(Weapon.EFireMode.greanadePlanting);
					}
				}
				else if (player.HandsController is CoopObservedQuickGrenadeController quickGrenadeController)
				{
					if (HasGrenade)
					{
						quickGrenadeController.SpawnGrenade(0f, GrenadePosition, GrenadeRotation, ThrowForce, LowThrow);
					}
				}
				else
				{
					FikaPlugin.Instance.FikaLogger.LogError($"GrenadePacket: HandsController was not of type CoopObservedGrenadeController! Was {player.HandsController.GetType().Name}");
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put((int)PacketType);
				writer.Put(HasGrenade);
				if (HasGrenade)
				{
					writer.Put(GrenadeRotation);
					writer.Put(GrenadePosition);
					writer.Put(ThrowForce);
					writer.Put(LowThrow);
				}
				writer.Put(PlantTripwire);
				writer.Put(ChangeToIdle);
				writer.Put(ChangeToPlant);
			}
		}

		public struct LightStatesPacket : ISubPacket
		{
			public int Amount;
			public FirearmLightStateStruct[] States;

			public LightStatesPacket(NetDataReader reader)
			{
				Amount = reader.GetInt();
				if (Amount > 0)
				{
					States = new FirearmLightStateStruct[Amount];
					for (int i = 0; i < Amount; i++)
					{
						States[i] = new()
						{
							Id = reader.GetString(),
							IsActive = reader.GetBool(),
							LightMode = reader.GetInt()
						};
					}
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.SetLightsState(States, true);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Amount);
				if (Amount > 0)
				{
					for (int i = 0; i < Amount; i++)
					{
						writer.Put(States[i].Id);
						writer.Put(States[i].IsActive);
						writer.Put(States[i].LightMode);
					}
				}
			}
		}

		public struct ScopeStatesPacket : ISubPacket
		{
			public int Amount;
			public FirearmScopeStateStruct[] States;

			public ScopeStatesPacket(NetDataReader reader)
			{
				Amount = reader.GetInt();
				if (Amount > 0)
				{
					States = new FirearmScopeStateStruct[Amount];
					for (int i = 0; i < Amount; i++)
					{
						States[i] = new()
						{
							Id = reader.GetString(),
							ScopeMode = reader.GetInt(),
							ScopeIndexInsideSight = reader.GetInt(),
							ScopeCalibrationIndex = reader.GetInt()
						};
					}
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					controller.SetScopeMode(States);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Amount);
				if (Amount > 0)
				{
					for (int i = 0; i < Amount; i++)
					{
						writer.Put(States[i].Id);
						writer.Put(States[i].ScopeMode);
						writer.Put(States[i].ScopeIndexInsideSight);
						writer.Put(States[i].ScopeCalibrationIndex);
					}
				}
			}
		}

		public struct ReloadMagPacket : ISubPacket
		{
			public bool Reload;
			public string MagId;
			public byte[] LocationDescription;

			public ReloadMagPacket(NetDataReader reader)
			{
				Reload = reader.GetBool();
				if (Reload)
				{
					MagId = reader.GetString();
					LocationDescription = reader.GetByteArray();
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					MagazineItemClass magazine = null;
					try
					{
						GStruct448<Item> result = player.FindItemById(MagId);
						if (!result.Succeeded)
						{
							FikaPlugin.Instance.FikaLogger.LogError(result.Error);
							return;
						}
						if (result.Value is MagazineItemClass magazineClass)
						{
							magazine = magazineClass;
						}
						else
						{
							FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: Item was not MagazineClass, it was {result.Value.GetType()}");
						}
					}
					catch (Exception ex)
					{
						FikaPlugin.Instance.FikaLogger.LogError(ex);
						FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
						throw;
					}
					ItemAddress gridItemAddress = null;
					if (LocationDescription != null)
					{
						try
						{
							GClass1193 reader = new(LocationDescription);
							if (LocationDescription.Length != 0)
							{
								GClass1687 descriptor = reader.ReadPolymorph<GClass1687>();
								gridItemAddress = player.InventoryController.ToItemAddress(descriptor);
							}
						}
						catch (GException4 exception2)
						{
							FikaPlugin.Instance.FikaLogger.LogError(exception2);
						}
					}
					if (magazine != null)
					{
						controller.ReloadMag(magazine, gridItemAddress, null);
					}
					else
					{
						FikaPlugin.Instance.FikaLogger.LogError($"ReloadMagPacket: final variables were null! Mag: {magazine}, Address: {gridItemAddress}");
					}
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Reload);
				if (Reload)
				{
					writer.Put(MagId);
					writer.PutByteArray(LocationDescription);
				}
			}
		}

		public struct QuickReloadMagPacket : ISubPacket
		{
			public bool Reload;
			public string MagId;

			public QuickReloadMagPacket(NetDataReader reader)
			{
				Reload = reader.GetBool();
				if (Reload)
				{
					MagId = reader.GetString();
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					try
					{
						GStruct448<Item> result = player.FindItemById(MagId);
						if (!result.Succeeded)
						{
							FikaPlugin.Instance.FikaLogger.LogError(result.Error);
							return;
						}
						if (result.Value is MagazineItemClass magazine)
						{
							controller.QuickReloadMag(magazine, null);
						}
						else
						{
							FikaPlugin.Instance.FikaLogger.LogError($"QuickReloadMagPacket: item was not of type MagazineClass, was {result.Value.GetType()}");
						}
					}
					catch (Exception ex)
					{
						FikaPlugin.Instance.FikaLogger.LogError(ex);
						FikaPlugin.Instance.FikaLogger.LogError($"QuickReloadMagPacket: There is no item {MagId} in profile {player.ProfileId}");
						throw;
					}
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Reload);
				if (Reload)
				{
					writer.Put(MagId);
				}
			}
		}

		public struct ReloadWithAmmoPacket : ISubPacket
		{
			public bool Reload;
			public EReloadWithAmmoStatus Status;
			public int AmmoLoadedToMag;
			public string[] AmmoIds;

			public ReloadWithAmmoPacket(NetDataReader reader)
			{
				Reload = reader.GetBool();
				if (Reload)
				{
					Status = (EReloadWithAmmoStatus)reader.GetByte();
					if (Status == EReloadWithAmmoStatus.StartReload)
					{
						AmmoIds = reader.GetStringArray();
					}
					if (Status is EReloadWithAmmoStatus.EndReload or EReloadWithAmmoStatus.AbortReload)
					{
						AmmoLoadedToMag = reader.GetInt();
					}
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					if (Status == EReloadWithAmmoStatus.AbortReload)
					{
						controller.CurrentOperation.SetTriggerPressed(true);
					}

					if (Reload)
					{
						if (Status == EReloadWithAmmoStatus.StartReload)
						{
							List<AmmoItemClass> bullets = controller.FindAmmoByIds(AmmoIds);
							AmmoPackReloadingClass ammoPack = new(bullets);
							controller.CurrentOperation.ReloadWithAmmo(ammoPack, null, null);
						}
					}
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Reload);
				if (Reload)
				{
					writer.Put((byte)Status);
					if (Status == EReloadWithAmmoStatus.StartReload)
					{
						writer.PutArray(AmmoIds);
					}
					if (AmmoLoadedToMag > 0)
					{
						writer.Put(AmmoLoadedToMag);
					}
				}
			}
		}

		public struct CylinderMagPacket : ISubPacket
		{
			public bool Changed;
			public int CamoraIndex;
			public bool HammerClosed;
			public bool Reload;
			public EReloadWithAmmoStatus Status;
			public int AmmoLoadedToMag;
			public string[] AmmoIds;

			public CylinderMagPacket(NetDataReader reader)
			{
				Changed = reader.GetBool();
				if (Changed)
				{
					CamoraIndex = reader.GetInt();
					HammerClosed = reader.GetBool();
				}
				Reload = reader.GetBool();
				if (Reload)
				{
					Status = (EReloadWithAmmoStatus)reader.GetByte();
					AmmoLoadedToMag = reader.GetInt();
					AmmoIds = reader.GetStringArray();
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					if (Status == EReloadWithAmmoStatus.AbortReload)
					{
						controller.CurrentOperation.SetTriggerPressed(true);
					}

					if (Reload)
					{
						if (Status == EReloadWithAmmoStatus.StartReload)
						{
							List<AmmoItemClass> bullets = controller.FindAmmoByIds(AmmoIds);
							AmmoPackReloadingClass ammoPack = new(bullets);
							controller.CurrentOperation.ReloadCylinderMagazine(ammoPack, null, null);
						}
					}

					if (Changed && controller.Weapon.GetCurrentMagazine() is CylinderMagazineItemClass cylinder)
					{
						cylinder.SetCurrentCamoraIndex(CamoraIndex);
						controller.Weapon.CylinderHammerClosed = HammerClosed;
					}
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Changed);
				if (Changed)
				{
					writer.Put(CamoraIndex);
					writer.Put(HammerClosed);
				}
				writer.Put(Reload);
				if (Reload)
				{
					writer.Put((byte)Status);
					writer.Put(AmmoLoadedToMag);
					writer.PutArray(AmmoIds);
				}
			}
		}

		public struct ReloadLauncherPacket : ISubPacket
		{
			public bool Reload;
			public string[] AmmoIds;

			public ReloadLauncherPacket(NetDataReader reader)
			{
				Reload = reader.GetBool();
				if (Reload)
				{
					AmmoIds = reader.GetStringArray();
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					List<AmmoItemClass> ammo = controller.FindAmmoByIds(AmmoIds);
					AmmoPackReloadingClass ammoPack = new(ammo);
					controller.ReloadGrenadeLauncher(ammoPack, null);
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Reload);
				if (Reload)
				{
					writer.PutArray(AmmoIds);
				}
			}
		}

		public struct ReloadBarrelsPacket : ISubPacket
		{
			public bool Reload;
			public string[] AmmoIds;
			public byte[] LocationDescription;

			public ReloadBarrelsPacket(NetDataReader reader)
			{
				Reload = reader.GetBool();
				if (Reload)
				{
					AmmoIds = reader.GetStringArray();
					LocationDescription = reader.GetByteArray();
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					List<AmmoItemClass> ammo = controller.FindAmmoByIds(AmmoIds);
					AmmoPackReloadingClass ammoPack = new(ammo);
					ItemAddress gridItemAddress = null;

					GClass1193 reader = new(LocationDescription);
					try
					{
						if (LocationDescription.Length > 0)
						{
							GClass1687 descriptor = reader.ReadPolymorph<GClass1687>();
							gridItemAddress = player.InventoryController.ToItemAddress(descriptor);
						}
					}
					catch (GException4 exception2)
					{
						FikaPlugin.Instance.FikaLogger.LogError(exception2);
					}

					if (ammoPack != null)
					{
						controller.ReloadBarrels(ammoPack, gridItemAddress, null);
					}
					else
					{
						FikaPlugin.Instance.FikaLogger.LogError($"ReloadBarrelsPacket: final variables were null! Ammo: {ammoPack}, Address: {gridItemAddress}");
					}
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(Reload);
				if (Reload)
				{
					writer.PutArray(AmmoIds);
					writer.PutByteArray(LocationDescription);
				}
			}
		}

		public struct FlareShotPacket : ISubPacket
		{
			public bool StartOneShotFire;
			public Vector3 ShotPosition;
			public Vector3 ShotForward;
			public string AmmoTemplateId;

			public FlareShotPacket(NetDataReader reader)
			{
				StartOneShotFire = reader.GetBool();
				if (!StartOneShotFire)
				{
					ShotPosition = reader.GetVector3();
					ShotForward = reader.GetVector3();
					AmmoTemplateId = reader.GetString();
				}
			}

			public void Execute(CoopPlayer player)
			{
				if (player.HandsController is CoopObservedFirearmController controller)
				{
					if (StartOneShotFire)
					{
						controller.FirearmsAnimator.SetFire(true);

						if (controller.Weapon is not RevolverItemClass)
						{
							controller.FirearmsAnimator.Animator.Play(controller.FirearmsAnimator.FullFireStateName, 1, 0f);
							controller.Weapon.Repairable.Durability = 0;
						}
						else
						{
							controller.FirearmsAnimator.Animator.Play(controller.FirearmsAnimator.FullDoubleActionFireStateName, 1, 0f);
						}
					}
					else
					{
						AmmoItemClass bulletClass = (AmmoItemClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), AmmoTemplateId, null);
						controller.InitiateFlare(bulletClass, ShotPosition, ShotForward);
						bulletClass.IsUsed = true;
						if (controller.WeaponPrefab.ObjectInHands is WeaponManagerClass weaponEffectsManager)
						{
							weaponEffectsManager.MoveAmmoFromChamberToShellPort(bulletClass.IsUsed, 0);
						}
						bulletClass = null;
						controller.FirearmsAnimator.SetFire(false);
					}
				}
			}

			public void Serialize(NetDataWriter writer)
			{
				writer.Put(StartOneShotFire);
				if (!StartOneShotFire)
				{
					writer.Put(ShotPosition);
					writer.Put(ShotForward);
					writer.Put(AmmoTemplateId);
				}
			}
		}
	}
}
