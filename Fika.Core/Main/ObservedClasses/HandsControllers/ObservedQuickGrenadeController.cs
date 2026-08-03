// © 2026 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

/// <summary>
/// This is only used by AI
/// </summary>
internal sealed class ObservedQuickGrenadeController : Player.QuickGrenadeThrowHandsController
{
    public static ObservedQuickGrenadeController Create(FikaPlayer player, ThrowWeap item)
    {
        return CreateController<ObservedQuickGrenadeController>(player, item);
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

    /// <summary>
    /// Original method to spawn a grenade, we use <see cref="SpawnGrenade(float, Vector3, Quaternion, Vector3, bool)"/> instead
    /// </summary>
    /// <param name="timeSinceSafetyLevelRemoved"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="force"></param>
    /// <param name="lowThrow"></param>
    public override void ThrowGrenade(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
    {
        // Do nothing
    }

    /// <summary>
    /// Spawns a grenade, uses data from <see cref="SubPackets.GrenadePacket"/>
    /// </summary>
    /// <param name="timeSinceSafetyLevelRemoved">The time since the safety was removed, use 0f</param>
    /// <param name="position">The <see cref="Vector3"/> position to start from</param>
    /// <param name="rotation">The <see cref="Quaternion"/> rotation of the grenade</param>
    /// <param name="force">The <see cref="Vector3"/> force of the grenade</param>
    /// <param name="lowThrow">If it's a low throw or not</param>
    public void SpawnGrenade(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
    {
        base.ThrowGrenade(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
    }
}
