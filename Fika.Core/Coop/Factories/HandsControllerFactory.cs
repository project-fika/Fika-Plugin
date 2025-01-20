// © 2025 Lacyway All Rights Reserved

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
    /// <param name="item">The <see cref="EFT.InventoryLogic.Item"/> to add to the controller.</param>
    internal class HandsControllerFactory(ObservedCoopPlayer player, Item item = null, KnifeComponent knifeComponent = null)
    {
        public ObservedCoopPlayer Player = player;
        public Item Item = item;
        public KnifeComponent KnifeComponent = knifeComponent;
        public MedsItemClass MedsItem;
        public FoodDrinkItemClass FoodItem;
        public GStruct350<EBodyPart> BodyParts;
        public float Amount;
        public int AnimationVariant;

        /// <summary>
        /// Creates a <see cref="CoopObservedFirearmController"/>
        /// </summary>
        /// <returns>A new <see cref="CoopObservedFirearmController"/> or null if the action failed.</returns>
        public Player.FirearmController CreateObservedFirearmController()
        {
            if (Item is Weapon weapon)
            {
                return CoopObservedFirearmController.Create(Player, weapon);
            }

            FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedFirearmController: item was not of type Weapon, was: {Item.GetType()}");
            return null;
        }

        /// <summary>
        /// Creates a <see cref="CoopObservedGrenadeController"/>
        /// </summary>
        /// <returns>A new <see cref="CoopObservedGrenadeController"/> or null if the action failed.</returns>
        public Player.GrenadeHandsController CreateObservedGrenadeController()
        {
            if (Item is ThrowWeapItemClass grenade)
            {
                return CoopObservedGrenadeController.Create(Player, grenade);
            }

            FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CoopObservedGrenadeController: item was not of type GrenadeClass, was: {Item.GetType()}");
            return null;
        }

        /// <summary>
        /// Creates a <see cref="CoopObservedQuickGrenadeController"/>
        /// </summary>
        /// <returns>A new <see cref="CoopObservedQuickGrenadeController"/> or null if the action failed.</returns>
        public Player.QuickGrenadeThrowHandsController CreateObservedQuickGrenadeController()
        {
            if (Item is ThrowWeapItemClass grenade)
            {
                return CoopObservedQuickGrenadeController.Create(Player, grenade);
            }

            FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedQuickGrenadeController: item was not of type GrenadeClass, was: {Item.GetType()}");
            return null;
        }

        /// <summary>
        /// Creates a <see cref="CoopObservedKnifeController"/>
        /// </summary>
        /// <returns>A new <see cref="CoopObservedKnifeController"/> or null if the action failed.</returns>
        public Player.KnifeController CreateObservedKnifeController()
        {
            if (KnifeComponent != null)
            {
                return CoopObservedKnifeController.Create(Player, KnifeComponent);
            }

            FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CoopObservedKnifeController: knifeComponent was null!");
            return null;
        }

        /// <summary>
        /// Creates a <see cref="CoopObservedMedsController"/>
        /// </summary>
        /// <returns>A new <see cref="CoopObservedMedsController"/> or null if the action failed.</returns>
        public Player.MedsController CreateObservedMedsController()
        {
            if (FoodItem != null)
            {
                return CoopObservedMedsController.Create(Player, FoodItem, new(EBodyPart.Head), Amount, AnimationVariant);
            }
            if (MedsItem != null)
            {
                return CoopObservedMedsController.Create(Player, MedsItem, BodyParts, 1f, AnimationVariant);
            }

            FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedMedsController: meds or food was null!");
            return null;
        }
    }
}
