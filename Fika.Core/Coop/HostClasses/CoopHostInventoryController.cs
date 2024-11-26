using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Fika.Core.Coop.ClientClasses.CoopClientInventoryController;

namespace Fika.Core.Coop.HostClasses
{
	public sealed class CoopHostInventoryController : Player.PlayerOwnerInventoryController
	{
		public override bool HasDiscardLimits
		{
			get
			{
				return false;
			}
		}
		private readonly ManualLogSource logger;
		private readonly Player player;
		private readonly CoopPlayer coopPlayer;
		private readonly IPlayerSearchController searchController;

		public CoopHostInventoryController(Player player, Profile profile, bool examined) : base(player, profile, examined)
		{
			this.player = player;
			coopPlayer = (CoopPlayer)player;
			searchController = new PlayerSearchControllerClass(profile, this);
			logger = BepInEx.Logging.Logger.CreateLogSource(nameof(CoopHostInventoryController));
		}

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		/*public override void RollBack()
		{
			base.RollBack();
			ResyncInventoryIdPacket packet = new(coopPlayer.NetId)
			{
				MongoId = mongoID_0
			};
			coopPlayer.PacketSender.SendPacket(ref packet, false);
		}*/

		public override void GetTraderServicesDataFromServer(string traderId)
		{
			if (FikaBackendUtils.IsClient)
			{
				TraderServicesPacket packet = new(coopPlayer.NetId)
				{
					IsRequest = true,
					TraderId = traderId
				};
				Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
				return;
			}

			coopPlayer.UpdateTradersServiceData(traderId).HandleExceptions();
		}

		public override void CallMalfunctionRepaired(Weapon weapon)
		{
			if (Singleton<SharedGameSettingsClass>.Instance.Game.Settings.MalfunctionVisability)
			{
				MonoBehaviourSingleton<PreloaderUI>.Instance.MalfunctionGlow.ShowGlow(BattleUIMalfunctionGlow.EGlowType.Repaired, true, method_41());
			}
		}

		public override void vmethod_1(BaseInventoryOperationClass operation, Callback callback)
		{
			HandleOperation(operation, callback).HandleExceptions();
		}

		private async Task HandleOperation(BaseInventoryOperationClass operation, Callback callback)
		{
			if (player.HealthController.IsAlive)
			{
				await Task.Yield();
			}
			RunHostOperation(operation, callback);
		}

		private void RunHostOperation(BaseInventoryOperationClass operation, Callback callback)
		{
			// Do not replicate picking up quest items, throws an error on the other clients            
			if (operation is GClass3195 moveOperation)
			{
				Item lootedItem = moveOperation.Item;
				if (lootedItem.QuestItem)
				{
					if (coopPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController && sharedQuestController.ContainsAcceptedType("PlaceBeacon"))
					{
						if (!sharedQuestController.CheckForTemplateId(lootedItem.TemplateId))
						{
							sharedQuestController.AddLootedTemplateId(lootedItem.TemplateId);

							// We use templateId because each client gets a unique itemId
							QuestItemPacket packet = new()
							{
								Nickname = coopPlayer.Profile.Info.MainProfileNickname,
								ItemId = lootedItem.TemplateId,
							};
							coopPlayer.PacketSender.SendPacket(ref packet);
						}
					}
					base.vmethod_1(operation, callback);
					return;
				}
			}

			// Do not replicate stashing quest items
			if (operation is RemoveOperationClass discardOperation)
			{
				if (discardOperation.Item.QuestItem)
				{
					base.vmethod_1(operation, callback);
					return;
				}
			}

			// Do not replicate quest operations / search operations
			// Check for GClass increments, ReadPolymorph
			if (operation is GClass3232 or GClass3236 or GClass3237 or GClass3238)
			{
				base.vmethod_1(operation, callback);
				return;
			}

#if DEBUG
			ConsoleScreen.Log($"InvOperation: {operation.GetType().Name}, Id: {operation.Id}");
#endif
			// Check for GClass increments, TraderServices
			if (operation is GClass3211)
			{
				base.vmethod_1(operation, callback);
				return;
			}

			HostInventoryOperationHandler handler = new(this, operation, callback);
			if (vmethod_0(handler.operation))
			{
				handler.operation.method_1(handler.HandleResult);

				GClass1198 writer = new();
				writer.WritePolymorph(operation.ToDescriptor());
				InventoryPacket packet = new()
				{
					CallbackId = operation.Id,
					OperationBytes = writer.ToArray()
				};

				coopPlayer.PacketSender.InventoryPackets.Enqueue(packet);
				return;
			}
			handler.operation.Dispose();
			handler.callback?.Fail($"Can't execute {handler.operation}", 1);
		}

		public override bool HasCultistAmulet(out CultistAmuletItemClass amulet)
		{
			amulet = null;
			using IEnumerator<Item> enumerator = Inventory.GetItemsInSlots([EquipmentSlot.Pockets]).GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is CultistAmuletItemClass cultistAmuletClass)
				{
					amulet = cultistAmuletClass;
					return true;
				}
			}
			return false;
		}

		private uint AddOperationCallback(BaseInventoryOperationClass operation, Action<ServerOperationStatus> callback)
		{
			ushort id = operation.Id;
			coopPlayer.OperationCallbacks.Add(id, callback);
			return id;
		}

		public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
		{
			return new GClass3232(method_12(), this, PlayerSearchController, Profile, item);
		}

		private class HostInventoryOperationHandler(CoopHostInventoryController inventoryController, BaseInventoryOperationClass operation, Callback callback)
		{
			public readonly CoopHostInventoryController inventoryController = inventoryController;
			public BaseInventoryOperationClass operation = operation;
			public readonly Callback callback = callback;

			public void HandleResult(IResult result)
			{
				if (!result.Succeed)
				{
					inventoryController.logger.LogError($"[{Time.frameCount}][{inventoryController.Name}] {inventoryController.ID} - Local operation failed: {operation.Id} - {operation}\r\nError: {result.Error}");
				}
				callback?.Invoke(result);
			}
		}
	}
}