// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Coop.Players;
using HarmonyLib;
using System.Collections.Generic;

namespace Fika.Core.Coop.ObservedClasses
{
	public class ObservedInventoryController : Player.PlayerInventoryController, Interface15
	{
		private readonly IPlayerSearchController searchController;
		private readonly CoopPlayer coopPlayer;
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
				return searchController;
			}
		}

		public ObservedInventoryController(Player player, Profile profile, bool examined, MongoID firstId, ushort firstOperationId) : base(player, profile, examined)
		{
			mongoID_0 = firstId;
			ushort_0 = firstOperationId;
			searchController = new GClass1898();
			coopPlayer = (CoopPlayer)player;
		}

		public override void AddDiscardLimits(Item rootItem, IEnumerable<GStruct384> destroyedItems)
		{
			// Do nothing
		}

		public override IEnumerable<GStruct384> GetItemsOverDiscardLimit(Item item)
		{
			return [];
		}

		public override bool HasDiscardLimit(Item item, out int limit)
		{
			limit = 0;
			return false;
		}

		public override GStruct428<bool> TryThrowItem(Item item, Callback callback = null, bool silent = false)
		{
			ThrowItem(item, false, callback);
			return true;
		}

		public override void StrictCheckMagazine(MagazineClass magazine, bool status, int skill = 0, bool notify = false, bool useOperation = true)
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

		public override bool vmethod_0(GClass3119 operation)
		{
			return true;
		}

		public override SearchContentOperation vmethod_2(SearchableItemClass item)
		{
			return null;
		}

		public override void InProcess(TraderControllerClass executor, Item item, ItemAddress to, bool succeed, GInterface388 operation, Callback callback)
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
			coopPlayer.StatisticsManager.OnGrabLoot(item);
		}

		private void HandleInProcess(Item item, ItemAddress to, GInterface388 operation, Callback callback)
		{
			Class1058 class1 = new()
			{
				callback = callback
			};
			Player.Class1184 handler = new()
			{
				player_0 = coopPlayer,
				callback = callback
			};
			if (!coopPlayer.HealthController.IsAlive)
			{
				handler.callback.Succeed();
				return;
			}
			// Check for GClass increments, fold operation
			if ((item.Parent != to || operation is GClass3135) && handler.player_0.HandsController.CanExecute(operation))
			{
				Traverse.Create(handler.player_0).Field<Callback>("_setInHandsCallback").Value = handler.callback;
				RaiseInOutProcessEvents(new(handler.player_0.HandsController.Item, CommandStatus.Begin, this));
				handler.player_0.HandsController.Execute(operation, new Callback(handler.method_1));
				return;
			}
			if (operation is GClass3135 && !handler.player_0.HandsController.CanExecute(operation))
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

		public void SetNewID(MongoID newId, ushort nextId)
		{
			mongoID_0 = newId;
			ushort_0 = nextId;
		}

		public GStruct423 CreateOperationFromDescriptor(GClass1670 descriptor)
		{
			method_13(descriptor);
			return descriptor.ToInventoryOperation(coopPlayer);
		}
	}
}
