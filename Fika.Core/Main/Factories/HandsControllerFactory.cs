// © 2025 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.Factories;

/// <summary>
/// Used to create custom <see cref="Player.ItemHandsController"/>s for the client that are used for networking
/// </summary>
/// <param name="player">The <see cref="FikaPlayer"/> to initiate the controller on.</param>
/// <param name="item">The <see cref="EFT.InventoryLogic.Item"/> to add to the controller.</param>
internal class HandsControllerFactory(ObservedPlayer player, Item item = null, KnifeComponent knifeComponent = null)
{
    public ObservedPlayer Player = player;
    public Item Item = item;
    public KnifeComponent KnifeComponent = knifeComponent;
    public MedsItemClass MedsItem;
    public FoodDrinkItemClass FoodItem;
    public GStruct382<EBodyPart> BodyParts;
    public float Amount;
    public int AnimationVariant;

    /// <summary>
    /// Creates a <see cref="ObservedFirearmController"/>
    /// </summary>
    /// <returns>A new <see cref="ObservedFirearmController"/> or null if the action failed.</returns>
    public Player.FirearmController CreateObservedFirearmController()
    {
        if (Item is Weapon weapon)
        {
            return ObservedFirearmController.Create(Player, weapon);
        }

        FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedFirearmController: item was not of type Weapon, was: {Item.GetType()}");
        return null;
    }

    /// <summary>
    /// Creates a <see cref="ObservedGrenadeController"/>
    /// </summary>
    /// <returns>A new <see cref="ObservedGrenadeController"/> or null if the action failed.</returns>
    public Player.GrenadeHandsController CreateObservedGrenadeController()
    {
        if (Item is ThrowWeapItemClass grenade)
        {
            return ObservedGrenadeController.Create(Player, grenade);
        }

        FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CoopObservedGrenadeController: item was not of type GrenadeClass, was: {Item.GetType()}");
        return null;
    }

    /// <summary>
    /// Creates a <see cref="ObservedQuickGrenadeController"/>
    /// </summary>
    /// <returns>A new <see cref="ObservedQuickGrenadeController"/> or null if the action failed.</returns>
    public Player.QuickGrenadeThrowHandsController CreateObservedQuickGrenadeController()
    {
        if (Item is ThrowWeapItemClass grenade)
        {
            return ObservedQuickGrenadeController.Create(Player, grenade);
        }

        FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedQuickGrenadeController: item was not of type GrenadeClass, was: {Item.GetType()}");
        return null;
    }

    /// <summary>
    /// Creates a <see cref="ObservedKnifeController"/>
    /// </summary>
    /// <returns>A new <see cref="ObservedKnifeController"/> or null if the action failed.</returns>
    public Player.KnifeController CreateObservedKnifeController()
    {
        if (KnifeComponent != null)
        {
            return ObservedKnifeController.Create(Player, KnifeComponent);
        }

        FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CoopObservedKnifeController: knifeComponent was null!");
        return null;
    }

    /// <summary>
    /// Creates a <see cref="ObservedMedsController"/>
    /// </summary>
    /// <returns>A new <see cref="ObservedMedsController"/> or null if the action failed.</returns>
    public Player.MedsController CreateObservedMedsController()
    {
        if (FoodItem != null)
        {
            return ObservedMedsController.Create(Player, FoodItem, new(EBodyPart.Head), Amount, AnimationVariant);
        }
        if (MedsItem != null)
        {
            return ObservedMedsController.Create(Player, MedsItem, BodyParts, 1f, AnimationVariant);
        }

        FikaPlugin.Instance.FikaLogger.LogError($"HandsControllerFactory::CreateObservedMedsController: meds or food was null!");
        return null;
    }
}
