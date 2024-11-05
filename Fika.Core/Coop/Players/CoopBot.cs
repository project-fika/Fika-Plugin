// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Coop.BotClasses;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.PacketHandlers;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using Fika.Core.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Networking.CommonSubPackets;
using static Fika.Core.Networking.Packets.SubPacket;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.Coop.Players
{
	/// <summary>
	/// Used to simulate bots for the host.
	/// </summary>
	public class CoopBot : CoopPlayer
	{
		public CoopPlayer MainPlayer
		{
			get
			{
				return (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
			}
		}
		/// <summary>
		/// The amount of players that have loaded this bot
		/// </summary>
		public int loadedPlayers = 0;
		private bool firstEnabled;

		public override bool IsVisible
		{
			get
			{
				return FikaBackendUtils.IsDedicated ? true : OnScreen;
			}
			set
			{

			}
		}

		public static async Task<CoopBot> CreateBot(GameWorld gameWorld, int playerId, Vector3 position, Quaternion rotation,
			string layerName, string prefix, EPointOfView pointOfView, Profile profile, bool aiControl,
			EUpdateQueue updateQueue, EUpdateMode armsUpdateMode, EUpdateMode bodyUpdateMode,
			CharacterControllerSpawner.Mode characterControllerMode, Func<float> getSensitivity,
			Func<float> getAimingSensitivity, IViewFilter filter, MongoID currentId, ushort nextOperationId)
		{
			bool useSimpleAnimator = profile.Info.Settings.UseSimpleAnimator;
			ResourceKey resourceKey = useSimpleAnimator ? ResourceKeyManagerAbstractClass.ZOMBIE_BUNDLE_NAME : ResourceKeyManagerAbstractClass.PLAYER_BUNDLE_NAME;
			CoopBot player = Create<CoopBot>(gameWorld, resourceKey, playerId, position, updateQueue, armsUpdateMode,
				bodyUpdateMode, characterControllerMode, getSensitivity, getAimingSensitivity, prefix, aiControl, useSimpleAnimator);

			player.IsYourPlayer = false;

			CoopBotInventoryController inventoryController = new(player, profile, true, currentId, nextOperationId);

			player.PacketSender = player.gameObject.AddComponent<BotPacketSender>();
			player.PacketReceiver = player.gameObject.AddComponent<PacketReceiver>();

			await player.Init(rotation, layerName, pointOfView, profile, inventoryController,
				new CoopBotHealthController(profile.Health, player, inventoryController, profile.Skills, aiControl),
				new ObservedStatisticsManager(), null, null, filter,
				EVoipState.NotAvailable, aiControl, false);

			player._handsController = EmptyHandsController.smethod_6<EmptyHandsController>(player);
			player._handsController.Spawn(1f, delegate { });

			player.AIData = new GClass551(null, player)
			{
				IsAI = true
			};

			Traverse botTraverse = Traverse.Create(player);
			botTraverse.Field<GClass886>("gclass886_0").Value = new();
			botTraverse.Field<GClass886>("gclass886_0").Value.Initialize(player, player.PlayerBones);

			if (FikaBackendUtils.IsDedicated)
			{
				botTraverse.Field<GClass886>("gclass886_0").Value.SetMode(GClass885.EMode.Disabled);
			}

			player.AggressorFound = false;
			player._animators[0].enabled = true;

			return player;
		}

		public override void OnVaulting()
		{
			// Do nothing
		}

		public override void OnSkillLevelChanged(AbstractSkillClass skill)
		{
			// Do nothing
		}

		public override void OnWeaponMastered(MasterSkillClass masterSkill)
		{
			// Do nothing
		}

		public override void ConnectSkillManager()
		{
			// Do nothing
		}

		public override void CreateMovementContext()
		{
			LayerMask movement_MASK = EFTHardSettings.Instance.MOVEMENT_MASK;
			MovementContext = BotMovementContext.Create(this, GetBodyAnimatorCommon, GetCharacterControllerCommon, movement_MASK);
		}

		public override void OnBeenKilledByAggressor(IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
		{
			base.OnBeenKilledByAggressor(aggressor, damageInfo, bodyPart, lethalDamageType);

			if (FikaPlugin.Instance.SharedQuestProgression && FikaPlugin.EasyKillConditions.Value)
			{
				if (aggressor.Profile.Info.GroupId == "Fika" && !aggressor.IsYourPlayer)
				{
					CoopPlayer mainPlayer = (CoopPlayer)Singleton<GameWorld>.Instance.MainPlayer;
					if (mainPlayer != null)
					{
						float distance = Vector3.Distance(aggressor.Position, Position);
						mainPlayer.HandleTeammateKill(ref damageInfo, bodyPart, Side, Profile.Info.Settings.Role, ProfileId,
							distance, Inventory.EquippedInSlotsTemplateIds, HealthController.BodyPartEffects, TriggerZones,
							(CoopPlayer)aggressor, Profile.Info.Settings.Experience);
					}
				}
			}
		}

		public override ShotInfoClass ApplyShot(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
		{
			ActiveHealthController activeHealthController = ActiveHealthController;
			if (activeHealthController != null && !activeHealthController.IsAlive)
			{
				return null;
			}
			bool flag = !string.IsNullOrEmpty(damageInfo.DeflectedBy);
			float damage = damageInfo.Damage;
			List<ArmorComponent> list = ProceedDamageThroughArmor(ref damageInfo, colliderType, armorPlateCollider, true);
			MaterialType materialType = flag ? MaterialType.HelmetRicochet : ((list == null || list.Count < 1) ? MaterialType.Body : list[0].Material);
			ShotInfoClass hitInfo = new()
			{
				PoV = PointOfView,
				Penetrated = string.IsNullOrEmpty(damageInfo.BlockedBy) || string.IsNullOrEmpty(damageInfo.DeflectedBy),
				Material = materialType
			};
			float num = damage - damageInfo.Damage;
			if (num > 0)
			{
				damageInfo.DidArmorDamage = num;
			}
			ApplyDamageInfo(damageInfo, bodyPartType, colliderType, 0f);
			ShotReactions(damageInfo, bodyPartType);
			ReceiveDamage(damageInfo.Damage, bodyPartType, damageInfo.DamageType, num, hitInfo.Material);

			if (list != null)
			{
				QueueArmorDamagePackets([.. list]);
			}

			if (damageInfo.Weapon != null)
			{
				lastWeaponId = damageInfo.Weapon.Id;
			}

			return hitInfo;
		}

		public override void ApplyDamageInfo(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType, float absorbed)
		{
			if (damageInfo.Weapon != null)
			{
				lastWeaponId = damageInfo.Weapon.Id;
			}
			base.ApplyDamageInfo(damageInfo, bodyPartType, colliderType, absorbed);
		}

		public override void ApplyExplosionDamageToArmor(Dictionary<GStruct209, float> armorDamage, DamageInfoStruct damageInfo)
		{
			_preAllocatedArmorComponents.Clear();
			Inventory.GetPutOnArmorsNonAlloc(_preAllocatedArmorComponents);
			List<ArmorComponent> armorComponents = [];
			foreach (ArmorComponent armorComponent in _preAllocatedArmorComponents)
			{
				float num = 0f;
				foreach (KeyValuePair<GStruct209, float> keyValuePair in armorDamage)
				{
					if (armorComponent.ShotMatches(keyValuePair.Key.BodyPartColliderType, keyValuePair.Key.ArmorPlateCollider))
					{
						num += keyValuePair.Value;
						armorComponents.Add(armorComponent);
					}
				}
				if (num > 0f)
				{
					num = armorComponent.ApplyExplosionDurabilityDamage(num, damageInfo, _preAllocatedArmorComponents);
					method_99(num, armorComponent);
				}
			}

			if (armorComponents.Count > 0)
			{
				QueueArmorDamagePackets([.. armorComponents]);
			}
		}

		public override void Proceed(Weapon weapon, Callback<IFirearmHandsController> callback, bool scheduled = true)
		{
			BotFirearmControllerHandler handler = new(this, weapon);

			bool flag = false;
			FirearmController firearmController;
			if ((firearmController = _handsController as FirearmController) != null)
			{
				flag = firearmController.CheckForFastWeaponSwitch(handler.weapon);
			}
			Func<FirearmController> func = new(handler.ReturnController);
			handler.Process = new Process<FirearmController, IFirearmHandsController>(this, func, handler.weapon, flag);
			handler.ConfirmCallback = new(handler.SendPacket);
			handler.Process.method_0(new(handler.HandleResult), callback, scheduled);
		}

		public override void OnDead(EDamageType damageType)
		{
			PacketSender.FirearmPackets.Clear();

			float num = EFTHardSettings.Instance.HIT_FORCE;
			num *= 0.3f + 0.7f * Mathf.InverseLerp(50f, 20f, LastDamageInfo.PenetrationPower);
			_corpseAppliedForce = num;

			if (FikaPlugin.ShowNotifications.Value)
			{
				if (LocaleUtils.IsBoss(Profile.Info.Settings.Role, out string name) && LastAggressor != null)
				{
					if (LastAggressor is CoopPlayer aggressor)
					{
						if (aggressor.gameObject.name.StartsWith("Player_") || aggressor.IsYourPlayer)
							NotificationManagerClass.DisplayMessageNotification(string.Format(LocaleUtils.KILLED_BOSS.Localized(),
								[ColorizeText(EColor.GREEN, LastAggressor.Profile.Info.MainProfileNickname), ColorizeText(EColor.BROWN, name)]),
								iconType: EFT.Communications.ENotificationIconType.Friend);
					}
				}
			}

			CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
			coopGame.Bots.Remove(ProfileId);

			base.OnDead(damageType);
		}

		public override void ShowHelloNotification(string sender)
		{
			// Do nothing
		}

		protected void OnEnable()
		{
			if (!firstEnabled)
			{
				firstEnabled = true;
				return;
			}

			CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
			if (coopGame != null && coopGame.Status == GameStatus.Started)
			{
				BotStatePacket packet = new()
				{
					NetId = NetId,
					Type = BotStatePacket.EStateType.EnableBot
				};
				if (PacketSender != null)
				{
					PacketSender.SendPacket(ref packet, true);
				}
			}
		}

		protected void OnDisable()
		{
			CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
			if (coopGame != null && coopGame.Status == GameStatus.Started)
			{
				BotStatePacket packet = new()
				{
					NetId = NetId,
					Type = BotStatePacket.EStateType.DisableBot
				};
				if (PacketSender != null)
				{
					PacketSender.SendPacket(ref packet, true);
				}
			}
		}

		public override void OnDestroy()
		{
#if DEBUG
			FikaPlugin.Instance.FikaLogger.LogInfo("Destroying " + ProfileId);
#endif
			if (Singleton<FikaServer>.Instantiated)
			{
				CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
				if (coopGame != null && coopGame.Status == GameStatus.Started)
				{
					FikaServer server = Singleton<FikaServer>.Instance;
					BotStatePacket packet = new()
					{
						NetId = NetId,
						Type = BotStatePacket.EStateType.DisposeBot
					};

					server.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
					coopGame.Bots.Remove(ProfileId);
				}
			}
			if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
			{
				coopHandler.Players.Remove(NetId);
			}
			base.OnDestroy();
		}

		public override void SendHandsInteractionStateChanged(bool value, int animationId)
		{
			if (value)
			{
				MovementContext.SetBlindFire(0);
			}
		}

		private class BotFirearmControllerHandler(CoopBot coopBot, Weapon weapon)
		{
			private readonly CoopBot coopBot = coopBot;
			public readonly Weapon weapon = weapon;
			public Process<FirearmController, IFirearmHandsController> Process;
			public Action ConfirmCallback;

			internal BotFirearmController ReturnController()
			{
				return BotFirearmController.Create(coopBot, weapon);
			}

			internal void SendPacket()
			{
				coopBot.PacketSender.CommonPlayerPackets.Enqueue(new()
				{
					Type = ECommonSubPacketType.Proceed,
					SubPacket = new ProceedPacket()
					{
						ProceedType = weapon.IsStationaryWeapon ? EProceedType.Stationary : EProceedType.Weapon,
						ItemId = weapon.Id
					}
				});
			}

			internal void HandleResult(IResult result)
			{
				if (result.Succeed)
				{
					ConfirmCallback();
				}
			}
		}
	}
}
