// © 2024 Lacyway All Rights Reserved

using Fika.Core.Coop.Players;
using Fika.Core.Networking.Packets;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
	/// <summary>
	/// This is only used by AI
	/// </summary>
	internal class CoopObservedQuickGrenadeController : EFT.Player.QuickGrenadeThrowHandsController
	{
		public static CoopObservedQuickGrenadeController Create(CoopPlayer player, ThrowWeapItemClass item)
		{
			return smethod_9<CoopObservedQuickGrenadeController>(player, item);
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
		public override void vmethod_2(float timeSinceSafetyLevelRemoved, Vector3 position, Quaternion rotation, Vector3 force, bool lowThrow)
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
			base.vmethod_2(timeSinceSafetyLevelRemoved, position, rotation, force, lowThrow);
		}
	}
}
