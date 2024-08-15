// © 2024 Lacyway All Rights Reserved

using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
	internal class CoopObservedGrenadeController : EFT.Player.GrenadeController
	{
		public CoopPlayer coopPlayer;

		private void Awake()
		{
			coopPlayer = GetComponent<CoopPlayer>();
		}

		public static CoopObservedGrenadeController Create(CoopPlayer player, GrenadeClass item)
		{
			return smethod_8<CoopObservedGrenadeController>(player, item);
		}

		/*public override Dictionary<Type, OperationFactoryDelegate> GetOperationFactoryDelegates()
        {
            return new Dictionary<Type, OperationFactoryDelegate>
                {
                    {
                        typeof(Class1029),
                        new OperationFactoryDelegate(method_10)
                    },
                    {
                        typeof(Class1027),
                        new OperationFactoryDelegate(method_11)
                    },
                    {
                        typeof(Class1028),
                        new OperationFactoryDelegate(CreateGrenadeClass1)
                    },
                    {
                        typeof(Class1025),
                        new OperationFactoryDelegate(method_13)
                    },
                    {
                        typeof(Class1026),
                        new OperationFactoryDelegate(method_14)
                    },
                    {
                        typeof(Class1023),
                        new OperationFactoryDelegate(method_15)
                    }
                };
        }*/

		/*private void CreateGrenadeClass1()
        {

        }*/

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
			// Do nothing, we use our own method
		}

		/// <summary>
		/// Spawns a grenade, uses data from <see cref="FikaSerialization.GrenadePacket"/>
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

		/*private class GrenadeClass1 : Class1028
        {
            public GrenadeClass1(Player.GrenadeController controller) : base(controller)
            {

            }
        }*/
	}
}
