using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;

namespace Fika.Core.Coop.ObservedClasses
{
	/// <summary>
	/// Created by: Paulov
	/// Paulov: Uses stubs for all of Statistics Manager
	/// </summary>
	public sealed class ObservedStatisticsManager : IStatisticsManager
	{
		public TimeSpan CurrentSessionLength
		{
			get
			{
				return default;
			}
		}

#pragma warning disable CS0067
		public event Action OnUniqueLoot;
#pragma warning restore CS0067

		public void AddDoorExperience(bool breached)
		{
			// Do nothing
		}

		public void BeginStatisticsSession()
		{
			// Do nothing
		}

		public void EndStatisticsSession(ExitStatus exitStatus, float pastTime)
		{
			// Do nothing
		}

		public void Init(Player player)
		{

		}

		public void OnEnemyDamage(DamageInfoStruct damage, EBodyPart bodyPart, string playerProfileId, EPlayerSide playerSide, WildSpawnType role, string groupId, float fullHealth, bool isHeavyDamage, float distance, int hour, List<string> targetEquipment, HealthEffects enemyEffects, List<string> zoneIds)
		{
			// Do nothing
		}

		public void OnEnemyKill(DamageInfoStruct damage, EDamageType lethalDamageType, EBodyPart bodyPart, EPlayerSide playerSide, WildSpawnType role, string playerAccountId, string playerProfileId, string playerName, string groupId, int level, int killExp, float distance, int hour, List<string> targetEquipment, HealthEffects enemyEffects, List<string> zoneIds, bool isFriendly, bool isAI)
		{
			// Do nothing
		}

		public void OnGrabLoot(Item item)
		{
			// Do nothing
		}

		public void OnGroupMemberConnected(Inventory inventory)
		{
			// Do nothing
		}

		public void OnInteractWithLootContainer(Item item)
		{
			// Do nothing
		}

		public void OnShot(Weapon weapon, AmmoItemClass ammo)
		{
			// Do nothing
		}
	}
}
