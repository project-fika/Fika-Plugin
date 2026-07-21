// © 2026 Lacyway All Rights Reserved

using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using HarmonyLib;
using JetBrains.Annotations;

namespace Fika.Core.Main.ObservedClasses;

public sealed class ObservedInventoryController : Player.PlayerInventoryController, Interface18
{
    private readonly IPlayerSearchController _searchController;
    private readonly FikaPlayer _fikaPlayer;
    public override bool HasDiscardLimits
    {
        get
        {
            return false;
        }
    }

    public override IPlayerSearchController PlayerSearchController
    {
        get
        {
            return _searchController;
        }
    }

    public ObservedInventoryController(Player player, Profile profile, bool examined, MongoID firstId, ushort firstOperationId, bool aiControl) : base(player, profile, examined)
    {
        MongoID_0 = firstId;
        Ushort_0 = firstOperationId;
        _searchController = new AISearchControllerClass();
        _fikaPlayer = (FikaPlayer)player;
    }

    public override void AddDiscardLimits(Item rootItem, IEnumerable<DestroyedItemsStruct> destroyedItems)
    {
        // Do nothing
    }

    public override IEnumerable<DestroyedItemsStruct> GetItemsOverDiscardLimit(Item item)
    {
        return [];
    }

    public override bool HasDiscardLimit(Item item, out int limit)
    {
        limit = 0;
        return false;
    }

    public override GStruct156<bool> TryThrowItem(Item item, Callback callback = null, bool silent = false)
    {
        ThrowItem(item, false, callback);
        return true;
    }

    public override GStruct155 CheckItemAction(Item item, ItemAddress location)
    {
        if (item.CurrentAddress == null)
        {
            return default;
        }
        if (!item.CheckForLockable(out var lockableComponent))
        {
            return new InteractionsHandlerClass.GClass1593(lockableComponent);
        }
        if (location != null && !location.Container.ParentItem.CheckForLockable(out lockableComponent))
        {
            return new InteractionsHandlerClass.GClass1593(lockableComponent);
        }
        var flag = false;
        if (item is CompoundItem compoundItem)
        {
            foreach (var item2 in compoundItem.GetAllItems())
            {
                foreach (var geventArgs in List_0)
                {
                    // this block is redundant during this check and isn't really meant to be used as it triggers a deny if player drags currently equipped item out of a slot
                    /*if (item2 == geventArgs.Item)
                    {
                        FikaGlobals.LogError($"{item2.LocalizedShortName()} was same as queued process: {geventArgs.Item.LocalizedShortName()}");
                        flag = true;
                    }*/
                    if (geventArgs.Location != null && geventArgs.Location.Container.ParentItem == item2)
                    {
#if DEBUG
                        FikaGlobals.LogError($"{item2.LocalizedShortName()} was parentItem of: {geventArgs.Item.LocalizedShortName()}");
#endif
                        flag = true;
                    }
                    if (location != null && geventArgs.Location != null && smethod_0(item2, location, geventArgs.Item, geventArgs.Location))
                    {
#if DEBUG
                        FikaGlobals.LogError($"{item2.LocalizedShortName()} failed to process on smethod_0");
#endif
                        flag = true;
                    }
                    if (flag)
                    {
#if DEBUG
                        FikaGlobals.LogError($"Flag hit, gevent was {geventArgs.GetType().Name}");
#endif
                        return new GClass1561(item, ParentItem.GetRootItem());
                    }
                }
            }
        }
        foreach (var geventArgs2 in List_0)
        {
            var lambda = new Class2447();
            if (geventArgs2 is GEventArgs7 geventArgs3 && (geventArgs3.TargetItem == item || geventArgs3.Item == item))
            {
#if DEBUG
                FikaGlobals.LogError($"{item.LocalizedShortName()} was in a GEventArgs7");
#endif
                flag = true;
            }
            if (geventArgs2 is GEventArgs8 geventArgs4 && (geventArgs4.FromItem == item || geventArgs4.Item == item || geventArgs4.TargetItem == item))
            {
#if DEBUG
                FikaGlobals.LogError($"{item.LocalizedShortName()} was in a GEventArgs8");
#endif
                flag = true;
            }
            if (geventArgs2 is GEventArgs2 geventArgs5 && item == geventArgs5.To.Container.ParentItem)
            {
#if DEBUG
                FikaGlobals.LogError($"{item.LocalizedShortName()} was in a GEventArgs2");
#endif
                flag = true;
            }
            if (geventArgs2 is GEventArgs3 geventArgs6)
            {
                if (item.Parent.Container.ParentItem == geventArgs6.Item)
                {
#if DEBUG
                    FikaGlobals.LogError($"{item.LocalizedShortName()} was in a GEventArgs3 and ParentItem was event Item");
#endif
                    flag = true;
                }
                if (Equals(geventArgs6.From, location))
                {
#if DEBUG
                    FikaGlobals.LogError($"{item.LocalizedShortName()} was in a GEventArgs6 and From was same as Location");
#endif
                    flag = true;
                }
            }
            lambda.inOutHandsProcess = geventArgs2 as GEventArgs17;
            if (lambda.inOutHandsProcess != null)
            {
                if (item.GetAllParentItemsAndSelf(false).Any(lambda.method_1))
                {
#if DEBUG
                    FikaGlobals.LogError($"{item.LocalizedShortName()} failed to pass GetAllParentItemsAndSelf");
#endif
                    flag = true;
                }
                if (location?.Container.ParentItem.GetAllParentItemsAndSelf(false).Any(lambda.method_1) == true)
                {
#if DEBUG
                    FikaGlobals.LogError($"{item.LocalizedShortName()} location failed to pass GetAllParentItemsAndSelf");
#endif
                    flag = true;
                }
            }
            if (item == geventArgs2.Item)
            {
#if DEBUG
                FikaGlobals.LogError($"{item.LocalizedShortName()} item was same as GEventArgs2.Item");
#endif
                flag = true;
            }
            if (location != null && geventArgs2.Location != null && smethod_0(item, location, geventArgs2.Item, geventArgs2.Location))
            {
#if DEBUG
                FikaGlobals.LogError($"{item.LocalizedShortName()} failed to process smethod_0 v2");
#endif
                flag = true;
            }
            if (flag)
            {
#if DEBUG
                FikaGlobals.LogError($"Flag hit, gevent was {geventArgs2.GetType().Name}");
#endif
                return new GClass1561(item, ParentItem.GetRootItem());
            }
        }
        if (!method_16(item, location))
        {
            return default;
        }
        return new GClass1568(item, location);
    }

    public override bool CheckOverLimit(IEnumerable<Item> items, [CanBeNull] ItemAddress to, bool useItemCountInEquipment, out InteractionsHandlerClass.GClass1609 error)
    {
        error = null;
        return true;
    }

    public override bool IsLimitedAtAddress(Item item, [CanBeNull] ItemAddress address, out int limit)
    {
        return IsLimitedAtAddress(item.TemplateId, address, out limit);
    }

    public override bool IsLimitedAtAddress(string templateId, ItemAddress address, out int limit)
    {
        limit = -1;
        return false;
    }

    public override void StrictCheckMagazine(MagazineItemClass magazine, bool status, int skill = 0, bool notify = false, bool useOperation = true)
    {
        // Do nothing
    }

    public override void OnAmmoLoadedCall(int count)
    {
        // Do nothing
    }

    public override void OnAmmoUnloadedCall(int count)
    {
        // Do nothing
    }

    public override void OnMagazineCheckCall()
    {
        // Do nothing
    }

    public override bool IsInventoryBlocked()
    {
        return false;
    }

    public override bool vmethod_0(BaseInventoryOperationClass operation)
    {
        return true;
    }

    public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
    {
        return null;
    }

    public override void InProcess(TraderControllerClass executor, Item item, ItemAddress to, bool succeed, GInterface438 operation, Callback callback)
    {
        if (!succeed)
        {
            callback.Succeed();
            return;
        }
        if (!executor.CheckTransferOwners(item, to, out var error))
        {
            callback.Fail(error.ToString());
            return;
        }
        HandleInProcess(item, to, operation, callback);
        _fikaPlayer.StatisticsManager.OnGrabLoot(item);
    }

    private void HandleInProcess(Item item, ItemAddress to, GInterface438 operation, Callback callback)
    {
        Player.Class1350 handler = new()
        {
            player_0 = _fikaPlayer,
            callback = callback
        };

        if (!_fikaPlayer.HealthController.IsAlive)
        {
            handler.callback.Succeed();
            return;
        }

        if (item is not Weapon && (item.Parent != to || operation is FoldOperationClass) && handler.player_0.HandsController.CanExecute(operation))
        {
            Traverse.Create(handler.player_0).Field<Callback>("_setInHandsCallback").Value = handler.callback;
            RaiseInOutProcessEvents(new(handler.player_0.HandsController.Item, CommandStatus.Begin, this));
            handler.player_0.HandsController.Execute(operation, new Callback(handler.method_1));
            return;
        }

        if (operation is FoldOperationClass && !handler.player_0.HandsController.CanExecute(operation))
        {
            handler.callback.Fail("Can't perform operation");
            return;
        }

        handler.callback.Succeed();
    }

    public override void GetTraderServicesDataFromServer(string traderId)
    {
        // Do nothing
    }

    public void SetNewID(MongoID newId)
    {
        MongoID_0 = newId;
    }

    OperationDataStruct Interface18.CreateOperationFromDescriptor(BaseDescriptorClass descriptor)
    {
        method_13(descriptor);
        return descriptor.ToInventoryOperation(_fikaPlayer);
    }
}
