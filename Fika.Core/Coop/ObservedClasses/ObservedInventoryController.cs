// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Coop.Players;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedInventoryController : Player.PlayerInventoryController, Interface16
    {
        private readonly IPlayerSearchController _searchController;
        private readonly CoopPlayer _coopPlayer;
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
            _coopPlayer = (CoopPlayer)player;
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

        public override GStruct442<bool> TryThrowItem(Item item, Callback callback = null, bool silent = false)
        {
            ThrowItem(item, false, callback);
            return true;
        }

        public override bool CheckOverLimit(IEnumerable<Item> items, [CanBeNull] ItemAddress to, bool useItemCountInEquipment, out InteractionsHandlerClass.GClass3910 error)
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

        public override void InProcess(TraderControllerClass executor, Item item, ItemAddress to, bool succeed, GInterface416 operation, Callback callback)
        {
            if (!succeed)
            {
                callback.Succeed();
                return;
            }
            if (!executor.CheckTransferOwners(item, to, out Error error))
            {
                callback.Fail(error.ToString());
                return;
            }
            HandleInProcess(item, to, operation, callback);
            _coopPlayer.StatisticsManager.OnGrabLoot(item);
        }

        private void HandleInProcess(Item item, ItemAddress to, GInterface416 operation, Callback callback)
        {
            Player.Class1240 handler = new()
            {
                player_0 = _coopPlayer,
                callback = callback
            };

            if (!_coopPlayer.HealthController.IsAlive)
            {
                handler.callback.Succeed();
                return;
            }

            if ((item.Parent != to || operation is FoldOperationClass) && handler.player_0.HandsController.CanExecute(operation))
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

        OperationDataStruct Interface16.CreateOperationFromDescriptor(BaseDescriptorClass descriptor)
        {
            method_13(descriptor);
            return descriptor.ToInventoryOperation(_coopPlayer);
        }
    }
}
