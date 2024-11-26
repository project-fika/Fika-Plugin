// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Fika.Core.Networking.FirearmSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;

namespace Fika.Core.Coop.ClientClasses
{
	public class CoopClientFirearmController : Player.FirearmController
	{
		protected CoopPlayer player;
		private bool isClient;

		public static CoopClientFirearmController Create(CoopPlayer player, Weapon weapon)
		{
			CoopClientFirearmController controller = smethod_6<CoopClientFirearmController>(player, weapon);
			controller.player = player;
			controller.isClient = FikaBackendUtils.IsClient;
			return controller;
		}

		public override void SetWeaponOverlapValue(float overlap)
		{
			base.SetWeaponOverlapValue(overlap);
			player.ObservedOverlap = overlap;
		}

		public override void WeaponOverlapping()
		{
			base.WeaponOverlapping();
			player.LeftStanceDisabled = DisableLeftStanceByOverlap;
		}

		public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
		{
			// Check for GClass increments..
			Dictionary<Type, OperationFactoryDelegate> operationFactoryDelegates = base.GetOperationFactoryDelegates();
			operationFactoryDelegates[typeof(GClass1752)] = new OperationFactoryDelegate(Weapon1);
			operationFactoryDelegates[typeof(GClass1753)] = new OperationFactoryDelegate(Weapon2);
			operationFactoryDelegates[typeof(GenericFireOperationClass)] = new OperationFactoryDelegate(Weapon3);
			return operationFactoryDelegates;
		}

		public override void OnPlayerDead()
		{
			if (IsAiming)
			{
				SetAim(false);
			}
			base.OnPlayerDead();
		}

		public override bool CanStartReload()
		{
			if (isClient)
			{
				return !player.WaitingForCallback && base.CanStartReload();
			}

			return base.CanStartReload();
		}

		public override bool CanPressTrigger()
		{
			if (isClient)
			{
				return !player.WaitingForCallback && base.CanPressTrigger();
			}

			return base.CanPressTrigger();
		}

		public Player.BaseAnimationOperation Weapon1()
		{
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
				return new FlareGunFireOperationClass(this);
			}
			if (Item.IsOneOff)
			{
				return new IsOneOffFireOperationClass(this);
			}
			if (Item.ReloadMode == Weapon.EReloadMode.OnlyBarrel)
			{
				return new FireOnlyBarrelFireOperation(this);
			}
			if (Item is RevolverItemClass) // This is a revolver
			{
				return new RevolverFireOperationClass(this);
			}
			if (!Item.BoltAction)
			{
				return new GenericFireOperationClass(this);
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
					Type = EFirearmSubPacketType.ToggleBipod
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
					Type = EFirearmSubPacketType.CheckChamber
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
					Type = EFirearmSubPacketType.CheckAmmo
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
					Type = EFirearmSubPacketType.ChangeFireMode,
					SubPacket = new ChangeFireModePacket()
					{
						FireMode = fireMode
					}
				});
			}
			return flag;
		}

		public override void ChangeAimingMode()
		{
			base.ChangeAimingMode();
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.ToggleAim,
				SubPacket = new ToggleAimPacket()
				{
					AimingIndex = IsAiming ? Item.AimIndex.Value : -1
				}
			});
		}

		public override void SetAim(bool value)
		{
			bool isAiming = IsAiming;
			bool aimingInterruptedByOverlap = AimingInterruptedByOverlap;
			base.SetAim(value);
			if (IsAiming != isAiming || aimingInterruptedByOverlap && player.HealthController.IsAlive)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.ToggleAim,
					SubPacket = new ToggleAimPacket()
					{
						AimingIndex = IsAiming ? Item.AimIndex.Value : -1
					}
				});
			}
		}

		public override void AimingChanged(bool newValue)
		{
			base.AimingChanged(newValue);
			if (!IsAiming && player.HealthController.IsAlive)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.ToggleAim,
					SubPacket = new ToggleAimPacket()
					{
						AimingIndex = -1
					}
				});
			}
		}

		public override bool CheckFireMode()
		{
			bool flag = base.CheckFireMode();
			if (flag && player.HealthController.IsAlive)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.CheckFireMode
				});
			}
			return flag;
		}

		public override void DryShot(int chamberIndex = 0, bool underbarrelShot = false)
		{
			base.DryShot(chamberIndex, underbarrelShot);
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.ShotInfo,
				SubPacket = new ShotInfoPacket()
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
			if (flag && player.HealthController.IsAlive)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.ExamineWeapon
				});
			}
			return flag;
		}

		public override void InitiateShot(IWeapon weapon, AmmoItemClass ammo, Vector3 shotPosition, Vector3 shotDirection, Vector3 fireportPosition, int chamberIndex, float overheat)
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
				Type = EFirearmSubPacketType.ShotInfo,
				SubPacket = new ShotInfoPacket()
				{
					ShotType = shotType,
					ShotPosition = shotPosition,
					ShotDirection = shotDirection,
					ChamberIndex = chamberIndex,
					Overheat = overheat,
					UnderbarrelShot = Weapon.IsUnderBarrelDeviceActive || Weapon.IsGrenadeLauncher,
					AmmoTemplate = ammo.AmmoTemplate._id,
					LastShotOverheat = weapon.MalfState.LastShotOverheat,
					LastShotTime = weapon.MalfState.LastShotTime,
					SlideOnOverheatReached = weapon.MalfState.SlideOnOverheatReached
				}
			});

			player.StatisticsManager.OnShot(Weapon, ammo);

			base.InitiateShot(weapon, ammo, shotPosition, shotDirection, fireportPosition, chamberIndex, overheat);
		}

		public override void QuickReloadMag(MagazineItemClass magazine, Callback callback)
		{
			if (!CanStartReload())
			{
				return;
			}

			base.QuickReloadMag(magazine, callback);

			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.QuickReloadMag,
				SubPacket = new QuickReloadMagPacket()
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
			CurrentOperation.ReloadBarrels(ammoPack, placeToPutContainedAmmoMagazine, callback, handler.Process);
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

			ReloadCylinderMagazineHandler handler = new(player, this, quickReload, ammoPack.GetReloadingAmmoIds(), [], (CylinderMagazineItemClass)Item.GetCurrentMagazine());
			Weapon.GetShellsIndexes(handler.shellsIndexes);
			CurrentOperation.ReloadCylinderMagazine(ammoPack, callback, handler.Process, handler.quickReload);
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
				Type = EFirearmSubPacketType.ReloadLauncher,
				SubPacket = new ReloadLauncherPacket()
				{
					Reload = true,
					AmmoIds = reloadingAmmoIds
				}
			});

			CurrentOperation.ReloadGrenadeLauncher(ammoPack, callback);
		}

		public override void ReloadMag(MagazineItemClass magazine, ItemAddress itemAddress, Callback callback)
		{
			if (!CanStartReload() || Blindfire)
			{
				return;
			}

			ReloadMagHandler handler = new(player, itemAddress, magazine);
			CurrentOperation.ReloadMag(magazine, itemAddress, callback, handler.Process);
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
			CurrentOperation.ReloadWithAmmo(ammoPack, callback, handler.Process);
		}

		public override bool SetLightsState(FirearmLightStateStruct[] lightsStates, bool force = false, bool animated = true)
		{
			if (force || CurrentOperation.CanChangeLightState(lightsStates))
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.ToggleLightStates,
					SubPacket = new LightStatesPacket()
					{
						Amount = lightsStates.Length,
						States = lightsStates
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
				Type = EFirearmSubPacketType.ToggleScopeStates,
				SubPacket = new ScopeStatesPacket()
				{
					Amount = scopeStates.Length,
					States = scopeStates
				}
			});
		}

		public override void ShotMisfired(AmmoItemClass ammo, Weapon.EMalfunctionState malfunctionState, float overheat)
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
				Type = EFirearmSubPacketType.ShotInfo,
				SubPacket = new ShotInfoPacket()
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
					Type = EFirearmSubPacketType.ToggleLauncher
				});
			}
			return flag;
		}

		public override void Loot(bool p)
		{
			base.Loot(p);
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.Loot
			});
		}

		public override void SetInventoryOpened(bool opened)
		{
			base.SetInventoryOpened(opened);
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.ToggleInventory,
				SubPacket = new ToggleInventoryPacket()
				{
					Open = opened
				}
			});
		}

		public override void ChangeLeftStance()
		{
			base.ChangeLeftStance();
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.LeftStanceChange,
				SubPacket = new LeftStanceChangePacket()
				{
					LeftStance = player.MovementContext.LeftStanceEnabled
				}
			});
		}

		public override void SendStartOneShotFire()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.FlareShot,
				SubPacket = new FlareShotPacket()
				{
					StartOneShotFire = true
				}
			});
		}

		public override void CreateFlareShot(AmmoItemClass flareItem, Vector3 shotPosition, Vector3 forward)
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.FlareShot,
				SubPacket = new FlareShotPacket()
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
				Type = EFirearmSubPacketType.ReloadWithAmmo,
				SubPacket = new ReloadWithAmmoPacket()
				{
					Reload = true,
					Status = EReloadWithAmmoStatus.AbortReload,
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
				Type = EFirearmSubPacketType.RollCylinder,
				SubPacket = new RollCylinderPacket()
				{
					RollToZeroCamora = rollToZeroCamora
				}
			});

			CurrentOperation.RollCylinder(null, rollToZeroCamora);
		}

		private void SendEndReloadPacket(int amount)
		{
			if (player.HealthController.IsAlive)
			{
				player.PacketSender.FirearmPackets.Enqueue(new()
				{
					Type = EFirearmSubPacketType.ReloadWithAmmo,
					SubPacket = new ReloadWithAmmoPacket()
					{
						Reload = true,
						Status = EReloadWithAmmoStatus.EndReload,
						AmmoLoadedToMag = amount
					}
				});
			}
		}

		private void SendBoltActionReloadPacket()
		{
			player.PacketSender.FirearmPackets.Enqueue(new()
			{
				Type = EFirearmSubPacketType.ReloadBoltAction
			});
		}

		private class FirearmClass1(Player.FirearmController controller) : GClass1753(controller)
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

		private class FirearmClass2(Player.FirearmController controller) : GClass1754(controller)
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

		private class FirearmClass3(Player.FirearmController controller) : GClass1755(controller)
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
		private class FirearmClass4(Player.FirearmController controller) : GClass1766(controller)
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

			public override void ReloadMag(MagazineItemClass magazine, ItemAddress gridItemAddress, Callback finishCallback, Callback startCallback)
			{
				base.ReloadMag(magazine, gridItemAddress, finishCallback, startCallback);
				SendBoltActionReloadPacket(true);
			}

			public override void QuickReloadMag(MagazineItemClass magazine, Callback finishCallback, Callback startCallback)
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

		private class ReloadMagHandler(CoopPlayer coopPlayer, ItemAddress gridItemAddress, MagazineItemClass magazine)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly ItemAddress gridItemAddress = gridItemAddress;
			private readonly MagazineItemClass magazine = magazine;

			public void Process(IResult result)
			{
				ItemAddress itemAddress = gridItemAddress;
				GClass1687 descriptor = itemAddress?.ToDescriptor();
				GClass1198 writer = new();

				byte[] locationDescription;
				if (descriptor != null)
				{
					writer.WritePolymorph(descriptor);
					locationDescription = writer.ToArray();
				}
				else
				{
					locationDescription = [];
				}

				if (coopPlayer.HealthController.IsAlive)
				{
					coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
					{
						Type = EFirearmSubPacketType.ReloadMag,
						SubPacket = new ReloadMagPacket()
						{
							Reload = true,
							MagId = magazine.Id,
							LocationDescription = locationDescription,
						}
					});
				}
			}
		}

		private class ReloadCylinderMagazineHandler(CoopPlayer coopPlayer, CoopClientFirearmController coopClientFirearmController, bool quickReload, string[] ammoIds, List<int> shellsIndexes, CylinderMagazineItemClass cylinderMagazine)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly CoopClientFirearmController coopClientFirearmController = coopClientFirearmController;
			public readonly bool quickReload = quickReload;
			private readonly string[] ammoIds = ammoIds;
			public readonly List<int> shellsIndexes = shellsIndexes;
			private readonly CylinderMagazineItemClass cylinderMagazine = cylinderMagazine;

			public void Process(IResult result)
			{
				if (coopPlayer.HealthController.IsAlive)
				{
					coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
					{
						Type = EFirearmSubPacketType.CylinderMag,
						SubPacket = new CylinderMagPacket()
						{
							Changed = true,
							CamoraIndex = cylinderMagazine.CurrentCamoraIndex,
							HammerClosed = coopClientFirearmController.Item.CylinderHammerClosed,
							Reload = true,
							Status = EReloadWithAmmoStatus.StartReload,
							AmmoIds = ammoIds
						}
					});
				}
			}
		}

		private class ReloadBarrelsHandler(CoopPlayer coopPlayer, ItemAddress placeToPutContainedAmmoMagazine, AmmoPackReloadingClass ammoPack)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly ItemAddress placeToPutContainedAmmoMagazine = placeToPutContainedAmmoMagazine;
			private readonly AmmoPackReloadingClass ammoPack = ammoPack;

			public void Process(IResult result)
			{
				ItemAddress itemAddress = placeToPutContainedAmmoMagazine;
				GClass1687 descriptor = itemAddress?.ToDescriptor();
				GClass1198 writer = new();
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
						Type = EFirearmSubPacketType.ReloadBarrels,
						SubPacket = new ReloadBarrelsPacket()
						{
							Reload = true,
							AmmoIds = ammoIds,
							LocationDescription = locationDescription
						}
					});
				}
			}
		}

		private class ReloadWithAmmoHandler(CoopPlayer coopPlayer, string[] ammoIds)
		{
			private readonly CoopPlayer coopPlayer = coopPlayer;
			private readonly string[] ammoIds = ammoIds;

			public void Process(IResult result)
			{
				if (coopPlayer.HealthController.IsAlive)
				{
					coopPlayer.PacketSender.FirearmPackets.Enqueue(new()
					{
						Type = EFirearmSubPacketType.ReloadWithAmmo,
						SubPacket = new ReloadWithAmmoPacket()
						{
							Reload = true,
							Status = EReloadWithAmmoStatus.StartReload,
							AmmoIds = ammoIds
						}
					});
				}
			}
		}
	}
}
