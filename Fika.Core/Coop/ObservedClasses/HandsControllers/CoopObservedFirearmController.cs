// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.Players;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using static Fika.Core.Networking.FirearmSubPackets;

namespace Fika.Core.Coop.ObservedClasses
{
	public class CoopObservedFirearmController : FirearmController
	{
		public WeaponPrefab WeaponPrefab
		{
			get
			{
				return weaponPrefab;
			}
			set
			{
				weaponPrefab = value;
			}
		}

		private CoopPlayer coopPlayer;
		private bool triggerPressed = false;
		private bool needsReset = false;
		private float lastFireTime = 0f;
		public override bool IsTriggerPressed
		{
			get
			{
				return triggerPressed;
			}
		}
		private float overlapCounter = 0f;
		private float aimMovementSpeed = 1f;
		private bool hasFired = false;
		private WeaponPrefab weaponPrefab;
		private GClass1723 underBarrelManager;
		public override bool IsAiming
		{
			get
			{
				return base.IsAiming;
			}
			set
			{
				if (!value)
				{
					_player.Physical.HoldBreath(false);
				}
				if (_isAiming == value)
				{
					return;
				}
				_isAiming = value;
				_player.Skills.FastAimTimer.Target = value ? 0f : 2f;
				_player.MovementContext.SetAimingSlowdown(IsAiming, 0.33f + aimMovementSpeed);
				_player.Physical.Aim((!_isAiming || !(_player.MovementContext.StationaryWeapon == null)) ? 0f : ErgonomicWeight);
				coopPlayer.ProceduralWeaponAnimation.IsAiming = _isAiming;
			}
		}

		public override Vector3 WeaponDirection
		{
			get
			{
				return -CurrentFireport.up;
			}
		}

		public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
		{
			// Check for GClass increments..
			Dictionary<Type, OperationFactoryDelegate> operationFactoryDelegates = base.GetOperationFactoryDelegates();
			operationFactoryDelegates[typeof(GClass1748)] = new OperationFactoryDelegate(Flare1);
			return operationFactoryDelegates;
		}

		private BaseAnimationOperation Flare1()
		{
			return new ObservedFlareOperation(this);
		}

		protected void Start()
		{
			_objectInHandsAnimator.SetAiming(false);
			aimMovementSpeed = coopPlayer.Skills.GetWeaponInfo(Item).AimMovementSpeed;
			WeaponPrefab = ControllerGameObject.GetComponent<WeaponPrefab>();
			if (UnderbarrelWeapon != null)
			{
				underBarrelManager = Traverse.Create(this).Field<GClass1723>("GClass1723_0").Value;
			}
		}

		public static CoopObservedFirearmController Create(CoopPlayer player, Weapon weapon)
		{
			CoopObservedFirearmController controller = smethod_6<CoopObservedFirearmController>(player, weapon);
			controller.coopPlayer = player;
			return controller;
		}

		public override void ManualUpdate(float deltaTime)
		{
			base.ManualUpdate(deltaTime);
			if (hasFired)
			{
				lastFireTime += deltaTime;
				if (lastFireTime > 0.05f)
				{
					FirearmsAnimator.SetFire(false);
					hasFired = false;
					if (needsReset)
					{
						needsReset = false;
						WeaponSoundPlayer.OnBreakLoop();
					}
				} 
			}
		}

		public override void WeaponOverlapping()
		{
			SetWeaponOverlapValue(coopPlayer.ObservedOverlap);
			ObservedOverlapView();
			if (overlapCounter <= 1f)
			{
				overlapCounter += Time.deltaTime / 1f;
			}
			if (coopPlayer.LeftStanceDisabled && coopPlayer.MovementContext.LeftStanceEnabled && overlapCounter > 1f)
			{
				coopPlayer.MovementContext.LeftStanceController.DisableLeftStanceAnimFromHandsAction();
				overlapCounter = 0f;
			}
			if (!coopPlayer.MovementContext.LeftStanceController.LastAnimValue && !coopPlayer.LeftStanceDisabled && coopPlayer.MovementContext.LeftStanceEnabled && overlapCounter > 1f)
			{
				coopPlayer.MovementContext.LeftStanceController.SetAnimatorLeftStanceToCacheFromHandsAction();
				overlapCounter = 0f;
			}
		}

		private void ObservedOverlapView()
		{
			Vector3 vector = _player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Get();
			if (coopPlayer.ObservedOverlap < 0.02f)
			{
				_player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = coopPlayer.ObservedOverlap;
				_player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = true;
			}
			else
			{
				_player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire = false;
				_player.ProceduralWeaponAnimation.TurnAway.OriginZShift = vector.y;
				_player.ProceduralWeaponAnimation.TurnAway.OverlapDepth = coopPlayer.ObservedOverlap;
			}
		}

		public override void OnPlayerDead()
		{
			try
			{
				base.OnPlayerDead();
				triggerPressed = false;
				SetTriggerPressed(false);

				needsReset = false;
				WeaponSoundPlayer.OnBreakLoop();

				coopPlayer.HandsAnimator.Animator.Update(Time.fixedDeltaTime);
				ManualUpdate(Time.fixedDeltaTime);
				if (CurrentOperation.State != EOperationState.Finished)
				{
					CurrentOperation.FastForward();
				}

				StartCoroutine(BreakFiringLoop());
			}
			catch (Exception ex)
			{
				FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController: Exception was caught: " + ex.Message);
			}
		}

		public override void IEventsConsumerOnShellEject()
		{
			if (WeaponPrefab != null && WeaponPrefab.ObjectInHands is WeaponManagerClass weaponEffectsManager)
			{
				weaponEffectsManager.StartSpawnShell(coopPlayer.Velocity * 0.66f, 0);
			}
		}

		private IEnumerator BreakFiringLoop()
		{
			Traverse<bool> isFiring = Traverse.Create(WeaponSoundPlayer).Field<bool>("_isFiring");
			int attempts = 0;
			while (isFiring.Value && attempts < 10)
			{
				yield return new WaitForEndOfFrame();
				WeaponSoundPlayer.OnBreakLoop();
				attempts++;
			}
		}

		public override void SetScopeMode(FirearmScopeStateStruct[] scopeStates)
		{
			_player.ProceduralWeaponAnimation.ObservedCalibration();
			base.SetScopeMode(scopeStates);
		}

		public override void AdjustShotVectors(ref Vector3 position, ref Vector3 direction)
		{
			// Do nothing
		}

		public override bool CanChangeCompassState(bool newState)
		{
			return false;
		}

		public override bool CanRemove()
		{
			return true;
		}

		public override void OnCanUsePropChanged(bool canUse)
		{
			// Do nothing
		}

		public override void SetCompassState(bool active)
		{
			// Do nothing
		}

		public override bool ToggleBipod()
		{
			return HasBipod && CurrentOperation.ToggleBipod();
		}

		public void HandleShotInfoPacket(ref ShotInfoPacket packet, InventoryController inventoryController)
		{
			if (packet.ShotType >= EShotType.Misfire)
			{
				switch (packet.ShotType)
				{
					case EShotType.Misfire:
						Weapon.MalfState.State = Weapon.EMalfunctionState.Misfire;
						break;
					case EShotType.Feed:
						Weapon.MalfState.State = Weapon.EMalfunctionState.Feed;
						break;
					case EShotType.JamedShot:
						Weapon.MalfState.State = Weapon.EMalfunctionState.Jam;
						break;
					case EShotType.SoftSlidedShot:
						Weapon.MalfState.State = Weapon.EMalfunctionState.SoftSlide;
						break;
					case EShotType.HardSlidedShot:
						Weapon.MalfState.State = Weapon.EMalfunctionState.HardSlide;
						break;
				}

				if (string.IsNullOrEmpty(packet.AmmoTemplate))
				{
					FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleShotInfoPacket: AmmoTemplate was null or empty!");
					return;
				}

				BulletClass bullet = (BulletClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate, null);
				Weapon.MalfState.MalfunctionedAmmo = bullet;
				Weapon.MalfState.AmmoToFire = bullet;
				if (WeaponPrefab != null)
				{
					if (Weapon.HasChambers && Weapon.Chambers[0].ContainedItem is BulletClass)
					{
						Weapon.Chambers[0].RemoveItemWithoutRestrictions();
					}
					WeaponPrefab.InitMalfunctionState(Weapon, false, false, out _);
					if (Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
					{
						WeaponPrefab.RevertMalfunctionState(Weapon, true, true);
						coopPlayer.InventoryController.ExamineMalfunction(Weapon, true);
					}
				}
				else
				{
					FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleShotInfoPacket: WeaponPrefab was null!");
				}
				return;
			}

			if (packet.ShotType == EShotType.DryFire)
			{
				FirearmsAnimator.SetFire(true);
				DryShot();
				hasFired = true;
				lastFireTime = 0f;
				return;
			}

			if (packet.ShotType == EShotType.RegularShot)
			{
				HandleObservedShot(ref packet, inventoryController);
				return;
			}
		}

		private void HandleObservedShot(ref ShotInfoPacket packet, InventoryController inventoryController)
		{
			if (string.IsNullOrEmpty(packet.AmmoTemplate))
			{
				FikaPlugin.Instance.FikaLogger.LogError("CoopObservedFirearmController::HandleShotInfoPacket: AmmoTemplate was null or empty!");
				return;
			}

			BulletClass ammo = (BulletClass)Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(), packet.AmmoTemplate, null);
			InitiateShot(Item, ammo, packet.ShotPosition, packet.ShotDirection,
				CurrentFireport.position, packet.ChamberIndex, packet.Overheat);

			if (Weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
			{
				triggerPressed = true;
			}

			float pitchMult = method_60();
			WeaponSoundPlayer.FireBullet(ammo, packet.ShotPosition, packet.ShotDirection,
				pitchMult, Malfunction, false, IsBirstOf2Start);

			Weapon.MalfState.LastShotOverheat = packet.LastShotOverheat;
			Weapon.MalfState.LastShotTime = packet.LastShotTime;
			Weapon.MalfState.SlideOnOverheatReached = packet.SlideOnOverheatReached;

			triggerPressed = false;
			hasFired = true;
			lastFireTime = 0f;
			if (Weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
			{
				needsReset = true;
			}

			MagazineClass magazine = Weapon.GetCurrentMagazine();

			FirearmsAnimator.SetFire(true);

			if (packet.UnderbarrelShot)
			{
				if (UnderbarrelWeapon.Chamber.ContainedItem is BulletClass grenadeBullet && !grenadeBullet.IsUsed)
				{
					grenadeBullet.IsUsed = true;
					UnderbarrelWeapon.Chamber.RemoveItem();
					underBarrelManager?.DestroyPatronInWeapon();
				}
				FirearmsAnimator.SetFire(false);
				return;
			}

			if (Weapon.HasChambers)
			{
				if (Weapon.ReloadMode is Weapon.EReloadMode.OnlyBarrel)
				{
					for (int i = 0; i < Weapon.Chambers.Length; i++)
					{
						if (Weapon.Chambers[i].ContainedItem is BulletClass bClass && !bClass.IsUsed)
						{
							bClass.IsUsed = true;
							if (WeaponPrefab != null && WeaponPrefab.ObjectInHands is WeaponManagerClass weaponEffectsManager)
							{
								if (!bClass.AmmoTemplate.RemoveShellAfterFire)
								{
									weaponEffectsManager.MoveAmmoFromChamberToShellPort(bClass.IsUsed, i);
								}
								else
								{
									weaponEffectsManager.DestroyPatronInWeapon();
								}
							}
							if (!bClass.AmmoTemplate.RemoveShellAfterFire)
							{
								Weapon.ShellsInChambers[i] = bClass.AmmoTemplate;
							}
							break;
						}
					}
				}
				else
				{
					Weapon.Chambers[0].RemoveItem(false);
					if (WeaponPrefab != null && WeaponPrefab.ObjectInHands is WeaponManagerClass weaponEffectsManager)
					{
						HandleShellEvent(weaponEffectsManager, ref packet, ammo, magazine);
					}
				}
			}

			// Check for GClass increments
			if (Weapon is GClass2973)
			{
				Weapon.CylinderHammerClosed = Weapon.FireMode.FireMode == Weapon.EFireMode.doubleaction;

				if (magazine is CylinderMagazineClass cylinderMagazine)
				{
					int firstIndex = cylinderMagazine.GetCamoraFireOrLoadStartIndex(!Weapon.CylinderHammerClosed);
					BulletClass cylinderAmmo = cylinderMagazine.GetFirstAmmo(!Weapon.CylinderHammerClosed);
					if (cylinderAmmo != null)
					{
						cylinderAmmo.IsUsed = true;
						GStruct426<GInterface373> removeOperation = cylinderMagazine.RemoveAmmoInCamora(cylinderAmmo, inventoryController);
						if (removeOperation.Failed)
						{
							FikaPlugin.Instance.FikaLogger.LogError($"Error removing ammo from cylinderMagazine on netId {coopPlayer.NetId}");
						}
						coopPlayer.InventoryController.CheckChamber(Weapon, false);
						FirearmsAnimator.SetAmmoOnMag(cylinderMagazine.Count);
						Weapon.ShellsInChambers[firstIndex] = cylinderAmmo.AmmoTemplate;
					}
					if (Weapon.CylinderHammerClosed || Weapon.FireMode.FireMode != Weapon.EFireMode.doubleaction)
					{
						cylinderMagazine.IncrementCamoraIndex(false);
					}
					FirearmsAnimator.SetCamoraIndex(cylinderMagazine.CurrentCamoraIndex);
					FirearmsAnimator.SetDoubleAction(Convert.ToSingle(Weapon.CylinderHammerClosed));
					FirearmsAnimator.SetHammerArmed(!Weapon.CylinderHammerClosed);
				}
			}

			ammo.IsUsed = true;

			if (magazine != null && magazine is not CylinderMagazineClass && magazine.Count > 0 && !Weapon.BoltAction)
			{
				if (Item.HasChambers && magazine.IsAmmoCompatible(Item.Chambers) && Item.Chambers[0].ContainedItem == null)
				{
					magazine.Cartridges.PopTo(inventoryController, Item.Chambers[0].CreateItemAddress());
				}
				else
				{
					magazine.Cartridges.PopToNowhere(inventoryController);
				}
			}

			if (Weapon.IsBoltCatch && Weapon.ChamberAmmoCount == 1 && !Weapon.ManualBoltCatch && !Weapon.MustBoltBeOpennedForExternalReload && !Weapon.MustBoltBeOpennedForInternalReload)
			{
				FirearmsAnimator.SetBoltCatch(false);
			}

			if (ammo.AmmoTemplate.IsLightAndSoundShot)
			{
				method_61(packet.ShotPosition, packet.ShotDirection);
				LightAndSoundShot(packet.ShotPosition, packet.ShotDirection, ammo.AmmoTemplate);
			}
		}

		public void HandleObservedBoltAction()
		{
			StartCoroutine(ObservedBoltAction(FirearmsAnimator, this, coopPlayer.InventoryController));
		}

		public List<BulletClass> FindAmmoByIds(string[] ammoIds)
		{
			_preallocatedAmmoList.Clear();
			foreach (string id in ammoIds)
			{
				GStruct428<Item> gstruct = _player.FindItemById(id);
				if (gstruct.Succeeded && gstruct.Value is BulletClass bulletClass)
				{
					_preallocatedAmmoList.Add(bulletClass);
				}
			}
			return _preallocatedAmmoList;
		}

		private IEnumerator ObservedBoltAction(FirearmsAnimator animator, FirearmController controller, InventoryController inventoryController)
		{
			animator.SetBoltActionReload(true);
			animator.SetFire(true);

			yield return new WaitForSeconds(0.75f);

			/*if (WeaponPrefab != null && WeaponPrefab.ObjectInHands is WeaponManagerClass weaponEffectsManager)
			{
				weaponEffectsManager.StartSpawnShell(coopPlayer.Velocity * 0.66f, 0);
			}*/

			MagazineClass magazine = controller.Item.GetCurrentMagazine();
			Weapon weapon = controller.Weapon;

			if (magazine != null && magazine is not CylinderMagazineClass && weapon.HasChambers)
			{
				magazine.Cartridges.PopTo(inventoryController, controller.Item.Chambers[0].CreateItemAddress());
			}

			animator.SetBoltActionReload(false);
			animator.SetFire(false);
		}

		private void HandleShellEvent(WeaponManagerClass weaponEffectsManager, ref ShotInfoPacket packet, BulletClass ammo, MagazineClass magazine)
		{
			weaponEffectsManager.DestroyPatronInWeapon(packet.ChamberIndex);
			if (!ammo.AmmoTemplate.RemoveShellAfterFire)
			{
				weaponEffectsManager.CreatePatronInShellPort(ammo, packet.ChamberIndex);
				FirearmsAnimator.SetShellsInWeapon(1);
			}
			else
			{
				FirearmsAnimator.SetShellsInWeapon(0);
			}

			bool boltAction = Weapon.BoltAction;

			if (magazine != null && !boltAction)
			{
				weaponEffectsManager.SetRoundIntoWeapon(ammo, 0);
			}

			if (Weapon.IsBoltCatch && Weapon.ChamberAmmoCount == 0 && Weapon.GetCurrentMagazine() != null && Weapon.GetCurrentMagazineCount() == 0 && !Weapon.ManualBoltCatch && !boltAction)
			{
				FirearmsAnimator.SetBoltCatch(true);
			}

			//Check for GClass increments
			if (Weapon is GClass2973 || Weapon.ReloadMode == Weapon.EReloadMode.OnlyBarrel || boltAction)
			{
				return;
			}

			//weaponEffectsManager.StartSpawnShell(coopPlayer.Velocity * 0.66f, 0);
		}

		private class ObservedFlareOperation(FirearmController controller) : GClass1748(controller)
		{
			public override void ProcessRemoveOneOffWeapon()
			{
				// Do nothing
			}
		}
	}
}
