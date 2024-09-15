using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ClientClasses
{
	public sealed class CoopClientInventoryController : Player.PlayerOwnerInventoryController
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
		private CoopPlayer CoopPlayer
		{
			get
			{
				return (CoopPlayer)player;
			}
		}
		private readonly IPlayerSearchController searchController;

		public CoopClientInventoryController(Player player, Profile profile, bool examined) : base(player, profile, examined)
		{
			this.player = player;
			IPlayerSearchController playerSearchController = new GClass1867(profile, this);
			searchController = playerSearchController;
			logger = BepInEx.Logging.Logger.CreateLogSource(nameof(CoopClientInventoryController));
		}

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		public override void GetTraderServicesDataFromServer(string traderId)
		{
			if (FikaBackendUtils.IsClient)
			{
				TraderServicesPacket packet = new(CoopPlayer.NetId)
				{
					IsRequest = true,
					TraderId = traderId
				};
				Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
				return;
			}

			CoopPlayer.UpdateTradersServiceData(traderId).HandleExceptions();
		}

		public override void CallMalfunctionRepaired(Weapon weapon)
		{
			if (Singleton<SharedGameSettingsClass>.Instance.Game.Settings.MalfunctionVisability)
			{
				MonoBehaviourSingleton<PreloaderUI>.Instance.MalfunctionGlow.ShowGlow(BattleUIMalfunctionGlow.EGlowType.Repaired, true, method_41());
			}
		}

		public override void vmethod_1(GClass3088 operation, Callback callback)
		{
			HandleOperation(operation, callback).HandleExceptions();
		}

		private async Task HandleOperation(GClass3088 operation, Callback callback)
		{
			if (player.HealthController.IsAlive)
			{
				await Task.Yield();
			}
			RunClientOperation(operation, callback);
		}

		private void RunClientOperation(GClass3088 operation, Callback callback)
		{
			// Do not replicate picking up quest items, throws an error on the other clients            
			if (operation is GClass3091 moveOperation)
			{
				Item lootedItem = moveOperation.Item;
				if (lootedItem.Template.QuestItem)
				{
					if (CoopPlayer.AbstractQuestControllerClass is CoopClientSharedQuestController sharedQuestController && sharedQuestController.ContainsAcceptedType("PlaceBeacon"))
					{
						if (!sharedQuestController.CheckForTemplateId(lootedItem.TemplateId))
						{
							sharedQuestController.AddLootedTemplateId(lootedItem.TemplateId);

							// We use templateId because each client gets a unique itemId
							QuestItemPacket questPacket = new(CoopPlayer.Profile.Info.MainProfileNickname, lootedItem.TemplateId);
							CoopPlayer.PacketSender.SendPacket(ref questPacket);
						}
					}
					base.vmethod_1(operation, callback);
					return;
				}
			}

			// Do not replicate quest operations / search operations
			// Check for GClass increments, ReadPolymorph
			if (operation is GClass3126 or GClass3131 or GClass3132 or GClass3133)
			{
				base.vmethod_1(operation, callback);
				return;
			}

			GClass1164 writer = new();

			InventoryPacket packet = new()
			{
				HasItemControllerExecutePacket = true
			};

			ClientInventoryOperationHandler handler = new()
			{
				operation = operation,
				callback = callback,
				inventoryController = this
			};

			handler.callback ??= new Callback(ClientPlayer.Control0.Class1488.class1488_0.method_0);
			uint operationNum = AddOperationCallback(operation, new Callback<EOperationStatus>(handler.HandleResult));
			writer.WritePolymorph(operation.ToDescriptor());
			packet.ItemControllerExecutePacket = new()
			{
				CallbackId = operationNum,
				OperationBytes = writer.ToArray()
			};

#if DEBUG
			ConsoleScreen.Log($"InvOperation: {operation.GetType().Name}, Id: {operation.Id}");
#endif

			CoopPlayer.PacketSender.InventoryPackets.Enqueue(packet);
		}

		public override bool HasCultistAmulet(out CultistAmuletClass amulet)
		{
			amulet = null;
			using IEnumerator<Item> enumerator = Inventory.GetItemsInSlots([EquipmentSlot.Pockets]).GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is CultistAmuletClass cultistAmuletClass)
				{
					amulet = cultistAmuletClass;
					return true;
				}
			}
			return false;
		}

		private uint AddOperationCallback(GClass3088 operation, Callback<EOperationStatus> callback)
		{
			ushort id = operation.Id;
			CoopPlayer.OperationCallbacks.Add(id, callback);
			return id;
		}

		public override SearchContentOperation vmethod_2(SearchableItemClass item)
		{
			return new GClass3126(method_12(), this, PlayerSearchController, Profile, item);
		}

		private class ClientInventoryOperationHandler
		{
			public EOperationStatus? serverOperationStatus;
			public EOperationStatus? localOperationStatus;
			public GClass3088 operation;
			public Callback callback;
			public CoopClientInventoryController inventoryController;

			public void HandleResult(Result<EOperationStatus> result)
			{
				ClientInventoryCallbackManager callbackManager = new()
				{
					clientOperationManager = this,
					result = result
				};

				if (callbackManager.result.Succeed)
				{
					EOperationStatus value = callbackManager.result.Value;
					if (value == EOperationStatus.Started)
					{
						localOperationStatus = EOperationStatus.Started;
						serverOperationStatus = EOperationStatus.Started;
						operation.method_0(new Callback(callbackManager.HandleResult));
						return;
					}
					if (value == EOperationStatus.Finished)
					{
						serverOperationStatus = EOperationStatus.Finished;
						if (localOperationStatus == serverOperationStatus)
						{
							operation.Dispose();
							callback.Succeed();
							return;
						}

						// Check for GClass increments
						if (operation is GInterface388 gInterface)
						{
							gInterface.Terminate();
						}
					}
				}
				else
				{
					FikaPlugin.Instance.FikaLogger.LogError($"{inventoryController.ID} - Client operation rejected by server: {operation.Id} - {operation}\r\nReason: {callbackManager.result.Error}");
					serverOperationStatus = EOperationStatus.Failed;
					localOperationStatus = EOperationStatus.Failed;
					operation.Dispose();
					callback.Invoke(callbackManager.result);

					// Check for GClass increments
					if (operation is GInterface388 gInterface)
					{
						gInterface.Terminate();
					}
				}
			}
		}

		private class ClientInventoryCallbackManager
		{
			public Result<EOperationStatus> result;
			public ClientInventoryOperationHandler clientOperationManager;

			public void HandleResult(IResult executeResult)
			{
				if (!executeResult.Succeed && (executeResult.Error is not "skipped skippable" or "skipped _completed"))
				{
					FikaPlugin.Instance.FikaLogger.LogError($"{clientOperationManager.inventoryController.ID} - Client operation critical failure: {clientOperationManager.inventoryController.ID} - {clientOperationManager.operation}\r\nError: {executeResult.Error}");
				}

				clientOperationManager.localOperationStatus = EOperationStatus.Finished;

				if (clientOperationManager.localOperationStatus == clientOperationManager.serverOperationStatus)
				{
					clientOperationManager.operation.Dispose();
					clientOperationManager.callback.Invoke(result);
					return;
				}

				if (clientOperationManager.serverOperationStatus != null)
				{
					if (clientOperationManager.serverOperationStatus == EOperationStatus.Failed)
					{
						clientOperationManager.operation.Dispose();
						clientOperationManager.callback.Invoke(result);
					}
				}
			}
		}
	}
}