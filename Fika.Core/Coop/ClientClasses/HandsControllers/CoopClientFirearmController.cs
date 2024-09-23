// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
	public class CoopClientFirearmController : Player.FirearmController
	{
		protected CoopPlayer player;

		public static CoopClientFirearmController Create(CoopPlayer player, Weapon weapon)
		{
			CoopClientFirearmController controller = smethod_6<CoopClientFirearmController>(player, weapon);
			controller.player = player;
			return controller;
		}

		public override void SetWeaponOverlapValue(float overlap)
		{
			base.SetWeaponOverlapValue(overlap);
			player.observedOverlap = overlap;
		}

		public override void WeaponOverlapping()
		{
			base.WeaponOverlapping();
			player.leftStanceDisabled = DisableLeftStanceByOverlap;
		}

		public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
		{
			// Check for GClass increments..
			Dictionary<Type, OperationFactoryDelegate> operationFactoryDelegates = base.GetOperationFactoryDelegates();
			operationFactoryDelegates[typeof(GClass1701)] = new OperationFactoryDelegate(Weapon1);
			operationFactoryDelegates[typeof(GClass1702)] = new OperationFactoryDelegate(Weapon2);
			operationFactoryDelegates[typeof(GClass1714)] = new OperationFactoryDelegate(Weapon3);
			return operationFactoryDelegates;
		}

		public Player.BaseAnimationOperation Weapon1()
		{
			if (Item.ReloadMode == Weapon.EReloadMode.InternalMagazine && Item.Chambers.Length == 0)
			{
				return new FirearmClass2(this);
			}
			if (Item.MustBoltBeOpennedForInternalReload)
			{
				return new FirearmClass3(this);
			}
			return new FirearmClass2(this);
		}

		public Player.BaseAnimationOperation Weapon2()
		{
			return new FirearmClass1(this);
		}

		public Player.BaseAnimationOperation Weapon3()
		{
			if (Item.IsFlareGun)
			{
				return new GClass1717(this);
			}
			if (Item.IsOneOff)
			{
				return new GClass1719(this);
			}
			if (Item.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
			{
				return new FireOnlyBarrelFireOperation(this);
			}
			if (Item is GClass2943) // This is a revolver
			{
				return new GClass1716(this);
			}
			if (!Item.BoltAction)
			{
				return new GClass1714(this);
			}
			return new FirearmClass4(this);
		}

		public override bool ToggleBipod()
		{
			bool success = base.ToggleBipod();
			if (success)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					ToggleBipod = true
				});
			}
			return success;
		}

		public override bool CheckChamber()
		{
			bool flag = base.CheckChamber();
			if (flag)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					CheckChamber = true
				});
			}
			return flag;
		}

		public override bool CheckAmmo()
		{
			bool flag = base.CheckAmmo();
			if (flag)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					CheckAmmo = true
				});
			}
			return flag;
		}

		public override bool ChangeFireMode(Weapon.EFireMode fireMode)
		{
			bool flag = base.ChangeFireMode(fireMode);
			if (flag)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					ChangeFireMode = true,
					FireMode = fireMode
				});
			}
			return flag;
		}

		public override void ChangeAimingMode()
		{
			base.ChangeAimingMode();
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				ToggleAim = true,
				AimingIndex = IsAiming ? Item.AimIndex.Value : -1
			});
		}

		public override void SetAim(bool value)
		{
			bool isAiming = IsAiming;
			bool aimingInterruptedByOverlap = AimingInterruptedByOverlap;
			base.SetAim(value);
			if (IsAiming != isAiming || aimingInterruptedByOverlap)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					ToggleAim = true,
					AimingIndex = IsAiming ? Item.AimIndex.Value : -1
				});
			}
		}

		public override void AimingChanged(bool newValue)
		{
			base.AimingChanged(newValue);
			if (!IsAiming)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					ToggleAim = true,
					AimingIndex = -1
				});
			}
		}

		public override bool CheckFireMode()
		{
			bool flag = base.CheckFireMode();
			if (flag)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					CheckFireMode = true
				});
			}
			return flag;
		}

		public override void DryShot(int chamberIndex = 0, bool underbarrelShot = false)
		{
			base.DryShot(chamberIndex, underbarrelShot);
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasShotInfo = true,
				ShotInfoPacket = new()
				{
					ShotType = EShotType.DryFire,
					ChamberIndex = chamberIndex,
					UnderbarrelShot = underbarrelShot
				}
			});
		}

		public override bool ExamineWeapon()
		{
			bool flag = base.ExamineWeapon();
			if (flag)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					ExamineWeapon = true
				});
			}
			return flag;
		}

		public override void InitiateShot(IWeapon weapon, BulletClass ammo, Vector3 shotPosition, Vector3 shotDirection, Vector3 fireportPosition, int chamberIndex, float overheat)
		{
			EShotType shotType = default;

			switch (weapon.MalfState.State)
			{
				case Weapon.EMalfunctionState.None:
					shotType = EShotType.RegularShot;
					break;
				case Weapon.EMalfunctionState.Misfire:
					shotType = EShotType.Misfire;
					break;
				case Weapon.EMalfunctionState.Jam:
					shotType = EShotType.JamedShot;
					break;
				case Weapon.EMalfunctionState.HardSlide:
					shotType = EShotType.HardSlidedShot;
					break;
				case Weapon.EMalfunctionState.SoftSlide:
					shotType = EShotType.SoftSlidedShot;
					break;
				case Weapon.EMalfunctionState.Feed:
					shotType = EShotType.Feed;
					break;
			}

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasShotInfo = true,
				ShotInfoPacket = new()
				{
					ShotType = shotType,
					ShotPosition = shotPosition,
					ShotDirection = shotDirection,
					ChamberIndex = chamberIndex,
					Overheat = overheat,
					UnderbarrelShot = Weapon.IsUnderBarrelDeviceActive,
					AmmoTemplate = ammo.AmmoTemplate._id,
					LastShotOverheat = weapon.MalfState.LastShotOverheat,
					LastShotTime = weapon.MalfState.LastShotTime,
					SlideOnOverheatReached = weapon.MalfState.SlideOnOverheatReached
				}
			});

			player.StatisticsManager.OnShot(Weapon, ammo);

			base.InitiateShot(weapon, ammo, shotPosition, shotDirection, fireportPosition, chamberIndex, overheat);
		}

		public override void QuickReloadMag(MagazineClass magazine, Callback callback)
		{
			if (!CanStartReload())
			{
				return;
			}

			base.QuickReloadMag(magazine, callback);

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasQuickReloadMagPacket = true,
				QuickReloadMagPacket = new()
				{
					Reload = true,
					MagId = magazine.Id
				}
			});
		}

		public override void ReloadBarrels(AmmoPackReloadingClass ammoPack, ItemAddress placeToPutContainedAmmoMagazine, Callback callback)
		{
			if (!CanStartReload() && ammoPack.AmmoCount < 1)
			{
				return;
			}

			ReloadBarrelsHandler handler = new(player, placeToPutContainedAmmoMagazine, ammoPack);
			CurrentOperation.ReloadBarrels(ammoPack, placeToPutContainedAmmoMagazine, callback, new Callback(handler.Process));
		}

		public override void ReloadCylinderMagazine(AmmoPackReloadingClass ammoPack, Callback callback, bool quickReload = false)
		{
			if (Blindfire)
			{
				return;
			}
			if (Item.GetCurrentMagazine() == null)
			{
				return;
			}
			if (!CanStartReload())
			{
				return;
			}

			ReloadCylinderMagazineHandler handler = new(player, this, quickReload, ammoPack.GetReloadingAmmoIds(), [], (CylinderMagazineClass)Item.GetCurrentMagazine());
			Weapon.GetShellsIndexes(handler.shellsIndexes);
			CurrentOperation.ReloadCylinderMagazine(ammoPack, callback, new Callback(handler.Process), handler.quickReload);
		}

		public override void ReloadGrenadeLauncher(AmmoPackReloadingClass ammoPack, Callback callback)
		{
			if (!CanStartReload())
			{
				return;
			}

			string[] reloadingAmmoIds = ammoPack.GetReloadingAmmoIds();
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasReloadLauncherPacket = true,
				ReloadLauncher = new()
				{
					Reload = true,
					AmmoIds = reloadingAmmoIds
				}
			});

			CurrentOperation.ReloadGrenadeLauncher(ammoPack, callback);
		}

		public override void ReloadMag(MagazineClass magazine, ItemAddress gridItemAddress, Callback callback)
		{
			if (!CanStartReload() || Blindfire)
			{
				return;
			}

			ReloadMagHandler handler = new(player, gridItemAddress, magazine);
			CurrentOperation.ReloadMag(magazine, gridItemAddress, callback, new Callback(handler.Process));
		}

		public override void ReloadWithAmmo(AmmoPackReloadingClass ammoPack, Callback callback)
		{
			if (Item.GetCurrentMagazine() == null)
			{
				return;
			}
			if (!CanStartReload())
			{
				return;
			}

			ReloadWithAmmoHandler handler = new(player, ammoPack.GetReloadingAmmoIds());
			CurrentOperation.ReloadWithAmmo(ammoPack, callback, new Callback(handler.Process));
		}

		public override bool SetLightsState(FirearmLightStateStruct[] lightsStates, bool force = false, bool animated = true)
		{
			if (force || CurrentOperation.CanChangeLightState(lightsStates))
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					ToggleTacticalCombo = true,
					LightStatesPacket = new()
					{
						Amount = lightsStates.Length,
						LightStates = lightsStates
					}
				});
			}

			return base.SetLightsState(lightsStates, force);
		}

		public override void SetScopeMode(FirearmScopeStateStruct[] scopeStates)
		{
			SendScopeStates(scopeStates);
			base.SetScopeMode(scopeStates);
		}
		public override void OpticCalibrationSwitchUp(FirearmScopeStateStruct[] scopeStates)
		{
			SendScopeStates(scopeStates);
			base.OpticCalibrationSwitchUp(scopeStates);
		}

		public override void OpticCalibrationSwitchDown(FirearmScopeStateStruct[] scopeStates)
		{
			SendScopeStates(scopeStates);
			base.OpticCalibrationSwitchDown(scopeStates);
		}

		private void SendScopeStates(FirearmScopeStateStruct[] scopeStates)
		{
			if (!CurrentOperation.CanChangeScopeStates(scopeStates))
			{
				return;
			}

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				ChangeSightMode = true,
				ScopeStatesPacket = new()
				{
					Amount = scopeStates.Length,
					FirearmScopeStateStruct = scopeStates
				}
			});
		}

		public override void ShotMisfired(BulletClass ammo, Weapon.EMalfunctionState malfunctionState, float overheat)
		{
			EShotType shotType = new();

			switch (malfunctionState)
			{
				case Weapon.EMalfunctionState.Misfire:
					shotType = EShotType.Misfire;
					break;
				case Weapon.EMalfunctionState.Jam:
					shotType = EShotType.JamedShot;
					break;
				case Weapon.EMalfunctionState.HardSlide:
					shotType = EShotType.HardSlidedShot;
					break;
				case Weapon.EMalfunctionState.SoftSlide:
					shotType = EShotType.SoftSlidedShot;
					break;
				case Weapon.EMalfunctionState.Feed:
					shotType = EShotType.Feed;
					break;
			}

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasShotInfo = true,
				ShotInfoPacket = new()
				{
					ShotType = shotType,
					Overheat = overheat,
					AmmoTemplate = ammo.TemplateId
				}
			});

			base.ShotMisfired(ammo, malfunctionState, overheat);
		}

		public override bool ToggleLauncher()
		{
			bool flag = base.ToggleLauncher();
			if (flag)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					ToggleLauncher = true
				});
			}
			return flag;
		}

		public override void Loot(bool p)
		{
			base.Loot(p);
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Loot = p
			});
		}

		public override void SetInventoryOpened(bool opened)
		{
			base.SetInventoryOpened(opened);
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				EnableInventory = true,
				InventoryStatus = opened
			});
		}

		public override void ChangeLeftStance()
		{
			base.ChangeLeftStance();
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasStanceChange = true,
				LeftStanceState = player.MovementContext.LeftStanceEnabled
			});
		}

		public override void SendStartOneShotFire()
		{
			base.SendStartOneShotFire();
		}

		public override void CreateFlareShot(BulletClass flareItem, Vector3 shotPosition, Vector3 forward)
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasFlareShot = true,
				FlareShotPacket = new()
				{
					ShotPosition = shotPosition,
					ShotForward = forward,
					AmmoTemplateId = flareItem.TemplateId
				}
			});
			base.CreateFlareShot(flareItem, shotPosition, forward);
		}

		private void SendAbortReloadPacket(int amount)
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasReloadWithAmmoPacket = true,
				ReloadWithAmmo = new()
				{
					Reload = true,
					Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.AbortReload,
					AmmoLoadedToMag = amount
				}
			});
		}

		public override void RollCylinder(bool rollToZeroCamora)
		{
			if (Blindfire || IsAiming)
			{
				return;
			}

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasRollCylinder = true,
				RollToZeroCamora = rollToZeroCamora
			});

			CurrentOperation.RollCylinder(null, rollToZeroCamora);
		}

		private void SendEndReloadPacket(int amount)
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				HasReloadWithAmmoPacket = true,
				ReloadWithAmmo = new()
				{
					Reload = true,
					Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.EndReload,
					AmmoLoadedToMag = amount
				}
			});
		}

		private void SendBoltActionReloadPacket()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				ReloadBoltAction = true
			});
		}

		private class FirearmClass1(Player.FirearmController controller) : GClass1702(controller)
		{
			public override void SetTriggerPressed(bool pressed)
			{
				bool bool_ = bool_1;
				base.SetTriggerPressed(pressed);
				if (bool_1 && !bool_)
				{
					coopClientFirearmController.SendAbortReloadPacket(int_0);
				}
			}

			public override void SwitchToIdle()
			{
				coopClientFirearmController.SendEndReloadPacket(int_0);
				method_13();
				base.SwitchToIdle();
			}

			private CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
		}

		private class FirearmClass2(Player.FirearmController controller) : GClass1703(controller)
		{
			public override void SetTriggerPressed(bool pressed)
			{
				bool bool_ = bool_1;
				base.SetTriggerPressed(pressed);
				if (bool_1 && !bool_)
				{
					coopClientFirearmController.SendAbortReloadPacket(int_0);
				}
			}

			public override void SwitchToIdle()
			{
				coopClientFirearmController.SendEndReloadPacket(int_0);
				base.SwitchToIdle();
			}

			private readonly CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
		}

		private class FirearmClass3(Player.FirearmController controller) : GClass1704(controller)
		{
			public override void SetTriggerPressed(bool pressed)
			{
				bool bool_ = bool_1;
				base.SetTriggerPressed(pressed);
				if (bool_1 && !bool_)
				{
					coopClientFirearmController.SendAbortReloadPacket(int_0);
				}
			}

			public override void SwitchToIdle()
			{
				coopClientFirearmController.SendEndReloadPacket(int_0);
				base.SwitchToIdle();
			}

			private readonly CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
		}

		// Check for GClass increments
		private class FirearmClass4(Player.FirearmController controller) : GClass1715(controller)
		{
			public override void Start()
			{
				base.Start();
				SendBoltActionReloadPacket(!firearmController_0.IsTriggerPressed);
			}

			public override void SetTriggerPressed(bool pressed)
			{
				base.SetTriggerPressed(pressed);
				SendBoltActionReloadPacket(!firearmController_0.IsTriggerPressed);
			}

			public override void SetInventoryOpened(bool opened)
			{
				base.SetInventoryOpened(opened);
				SendBoltActionReloadPacket(true);
			}

			public override void ReloadMag(MagazineClass magazine, ItemAddress gridItemAddress, Callback finishCallback, Callback startCallback)
			{
				base.ReloadMag(magazine, gridItemAddress, finishCallback, startCallback);
				SendBoltActionReloadPacket(true);
			}

			public override void QuickReloadMag(MagazineClass magazine, Callback finishCallback, Callback startCallback)
			{
				base.QuickReloadMag(magazine, finishCallback, startCallback);
				SendBoltActionReloadPacket(true);
			}

			public override void ReloadWithAmmo(AmmoPackReloadingClass ammoPack, Callback finishCallback, Callback startCallback)
			{
				base.ReloadWithAmmo(ammoPack, finishCallback, startCallback);
				SendBoltActionReloadPacket(true);
			}

			private void SendBoltActionReloadPacket(bool value)
			{
				if (!hasSent && value)
				{
					hasSent = true;
					coopClientFirearmController.SendBoltActionReloadPacket();
				}
			}

			public override void Reset()
			{
				base.Reset();
				hasSent = false;
			}

			private CoopClientFirearmController coopClientFirearmController = (CoopClientFirearmController)controller;
			private bool hasSent;
		}

		private class ReloadMagHandler(CoopPlayer coopPlayer, ItemAddress gridItemAddress, MagazineClass magazine)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly ItemAddress gridItemAddress = gridItemAddress;
			private readonly MagazineClass magazine = magazine;

			public void Process(IResult error)
			{
				ItemAddress itemAddress = gridItemAddress;
				GClass1636 descriptor = itemAddress?.ToDescriptor();
				GClass1164 writer = new();

				byte[] locationDescription;
				if (descriptor != null)
				{
					writer.WritePolymorph(descriptor);
					locationDescription = writer.ToArray();
				}
				else
				{
					locationDescription = Array.Empty<byte>();
				}

				if (error.Succeed)
				{
					coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
					{
						HasReloadMagPacket = true,
						ReloadMagPacket = new()
						{
							Reload = true,
							MagId = magazine.Id,
							LocationDescription = locationDescription,
						}
					});
				}
			}
		}

		private class ReloadCylinderMagazineHandler(CoopPlayer coopPlayer, CoopClientFirearmController coopClientFirearmController, bool quickReload, string[] ammoIds, List<int> shellsIndexes, CylinderMagazineClass cylinderMagazine)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly CoopClientFirearmController coopClientFirearmController = coopClientFirearmController;
			public readonly bool quickReload = quickReload;
			private readonly string[] ammoIds = ammoIds;
			public readonly List<int> shellsIndexes = shellsIndexes;
			private readonly CylinderMagazineClass cylinderMagazine = cylinderMagazine;

			public void Process(IResult error)
			{
				coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
				{
					HasReloadWithAmmoPacket = true,
					ReloadWithAmmo = new()
					{
						Reload = true,
						Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
						AmmoIds = ammoIds
					},
					HasCylinderMagPacket = true,
					CylinderMag = new()
					{
						Changed = true,
						CamoraIndex = cylinderMagazine.CurrentCamoraIndex,
						HammerClosed = coopClientFirearmController.Item.CylinderHammerClosed
					}
				});
			}
		}

		private class ReloadBarrelsHandler(CoopPlayer coopPlayer, ItemAddress placeToPutContainedAmmoMagazine, AmmoPackReloadingClass ammoPack)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly ItemAddress placeToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
			private readonly AmmoPackReloadingClass ammoPack = ammoPack;

			public void Process(IResult error)
			{
				ItemAddress itemAddress = placeToPutContainedAmmoMagazine;
				GClass1636 descriptor = itemAddress?.ToDescriptor();
				GClass1164 writer = new();
				string[] ammoIds = ammoPack.GetReloadingAmmoIds();

				byte[] locationDescription;
				if (descriptor != null)
				{
					writer.WritePolymorph(descriptor);
					locationDescription = writer.ToArray();
				}
				else
				{
					locationDescription = Array.Empty<byte>();
				}

				if (coopPlayer.HealthController.IsAlive)
				{
					coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
					{
						HasReloadBarrelsPacket = true,
						ReloadBarrels = new()
						{
							Reload = true,
							AmmoIds = ammoIds,
							LocationDescription = locationDescription,
						}
					});
				}
			}
		}

		private class ReloadWithAmmoHandler(CoopPlayer coopPlayer, string[] ammoIds)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly string[] ammoIds = ammoIds;

			public void Process(IResult error)
			{
				if (error.Succeed)
				{
					coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
					{
						HasReloadWithAmmoPacket = true,
						ReloadWithAmmo = new()
						{
							Reload = true,
							Status = FikaSerialization.ReloadWithAmmoPacket.EReloadWithAmmoStatus.StartReload,
							AmmoIds = ammoIds
						}
					});
				}
			}
		}
	}
}
