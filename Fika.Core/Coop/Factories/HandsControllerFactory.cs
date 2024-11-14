// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Coop.ObservedClasses;
using Fika.Core.Coop.Players;

namespace Fika.Core.Coop.Factories
{
	/// <summary>
	/// Used to create custom <see cref="Player.ItemHandsController"/>s for the client that are used for networking
	/// </summary>
	/// <param name="player">The <see cref="CoopPlayer"/> to initiate the controller on.</param>
	/// <param name="item">The <see cref="Item"/> to add to the controller.</param>
	internal class HandsControllerFactory(CoopPlayer player, Item item = null, KnifeComponent knifeComponent = null)
	{
		public CoopPlayer player = player;
		public Item item = item;
		public KnifeComponent knifeComponent = knifeComponent;
		public MedsItemClass meds;
		public FoodDrinkItemClass food;
		public EBodyPart bodyPart;
		public float amount;
		public int animationVariant;

		/// <summary>
		/// Creates a <see cref="CoopObservedFirearmController"/>
		/// </summary>
		/// <returns>A new <see cref="CoopObservedFirearmController"/> or null if the action failed.</returns>
		public Player.FirearmController CreateObservedFirearmController()
		{
			if (item is Weapon weapon)
			{
				return CoopObservedFirearmController.Create(player, weapon);
			}
			else
			{
				FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedFirearmController: item was not of type Weapon, was: {item.GetType()}");
				return null;
			}
		}

		/// <summary>
		/// Creates a <see cref="CoopObservedGrenadeController"/>
		/// </summary>
		/// <returns>A new <see cref="CoopObservedGrenadeController"/> or null if the action failed.</returns>
		public Player.GrenadeHandsController CreateObservedGrenadeController()
		{
			if (item is ThrowWeapItemClass grenade)
			{
				return CoopObservedGrenadeController.Create(player, grenade);
			}
			else
			{
				FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CoopObservedGrenadeController: item was not of type GrenadeClass, was: {item.GetType()}");
				return null;
			}
		}

		/// <summary>
		/// Creates a <see cref="CoopObservedQuickGrenadeController"/>
		/// </summary>
		/// <returns>A new <see cref="CoopObservedQuickGrenadeController"/> or null if the action failed.</returns>
		public Player.QuickGrenadeThrowHandsController CreateObservedQuickGrenadeController()
		{
			if (item is ThrowWeapItemClass grenade)
			{
				return CoopObservedQuickGrenadeController.Create(player, grenade);
			}
			else
			{
				FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedQuickGrenadeController: item was not of type GrenadeClass, was: {item.GetType()}");
				return null;
			}
		}

		/// <summary>
		/// Creates a <see cref="CoopObservedKnifeController"/>
		/// </summary>
		/// <returns>A new <see cref="CoopObservedKnifeController"/> or null if the action failed.</returns>
		public Player.KnifeController CreateObservedKnifeController()
		{
			if (knifeComponent != null)
			{
				return CoopObservedKnifeController.Create(player, knifeComponent);
			}
			else
			{
				FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CoopObservedKnifeController: knifeComponent was null!");
				return null;
			}
		}

		/// <summary>
		/// Creates a <see cref="CoopObservedMedsController"/>
		/// </summary>
		/// <returns>A new <see cref="CoopObservedMedsController"/> or null if the action failed.</returns>
		public Player.MedsController CreateObservedMedsController()
		{
			if (food != null)
			{
				return CoopObservedMedsController.Create(player, food, EBodyPart.Head, amount, animationVariant);
			}
			if (meds != null)
			{
				return CoopObservedMedsController.Create(player, meds, bodyPart, 1f, animationVariant);
			}
			else
			{
				FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedMedsController: meds or food was null!");
				return null;
			}
		}
	}
}
