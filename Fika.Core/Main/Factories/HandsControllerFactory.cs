// © 2026 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.ObservedClasses.HandsControllers;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;

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

        FikaGlobals.LogError($"item was not of type Weapon, was: {Item.GetType()}");
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

        FikaGlobals.LogError($"item was not of type GrenadeClass, was: {Item.GetType()}");
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

        FikaGlobals.LogError($"item was not of type GrenadeClass, was: {Item.GetType()}");
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

        FikaGlobals.LogError("knifeComponent was null!");
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

        FikaGlobals.LogError("meds or food was null!");
        return null;
    }
}
