using EFT.Ballistics;
using System;
using System.Collections.Generic;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses;

/// <summary>
/// Created by: Paulov
/// Paulov: Uses stubs for all of Statistics Manager
/// </summary>
public sealed class ObservedStatisticsManager : Il2CppSystem.Object, IStatisticsManager
{
    public ObservedStatisticsManager(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedStatisticsManager() : base(Il2CppInjection.Allocate<ObservedStatisticsManager>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public Il2CppSystem.TimeSpan CurrentSessionLength
    {
        get
        {
            return new Il2CppSystem.TimeSpan();
        }
    }

    public void add_OnUniqueLoot(Il2CppSystem.Action value)
    {
    }

    public void remove_OnUniqueLoot(Il2CppSystem.Action value)
    {
    }

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

    public void OnEnemyDamage(DamageInfo damage, EBodyPart bodyPart, string playerProfileId, EPlayerSide playerSide, WildSpawnType role, string groupId, float fullHealth, bool isHeavyDamage, float distance, int hour, Il2CppSystem.Collections.Generic.List<string> targetEquipment, HealthEffects enemyEffects, Il2CppSystem.Collections.Generic.List<string> zoneIds)
    {
        // Do nothing
    }

    public void OnEnemyKill(DamageInfo damage, EDamageType lethalDamageType, EBodyPart bodyPart, EPlayerSide playerSide, WildSpawnType role, string playerAccountId, string playerProfileId, string playerName, string groupId, int level, int killExp, float distance, int hour, Il2CppSystem.Collections.Generic.List<string> targetEquipment, HealthEffects enemyEffects, Il2CppSystem.Collections.Generic.List<string> zoneIds, bool isFriendly, bool isAI)
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

    public void OnShot(Weapon weapon, Ammo ammo)
    {
        // Do nothing
    }
}
