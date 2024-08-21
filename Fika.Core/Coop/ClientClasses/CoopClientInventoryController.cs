using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using EFT.UI;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.ClientClasses
{
	public sealed class CoopClientInventoryController(Player player, Profile profile, bool examined, IPlayerSearchController searchController) : Player.PlayerOwnerInventoryController(player, profile, examined)
	{
		public override bool HasDiscardLimits => false;
		ManualLogSource BepInLogger { get; set; } = BepInEx.Logging.Logger.CreateLogSource(nameof(CoopClientInventoryController));
		private readonly Player Player = player;
		private CoopPlayer CoopPlayer
		{
			get
			{
				return (CoopPlayer)Player;
			}
		}
		private IPlayerSearchController searchController = searchController;

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		public override void CallMalfunctionRepaired(Weapon weapon)
		{
			base.CallMalfunctionRepaired(weapon);
			if (!Player.IsAI && (bool)Singleton<SharedGameSettingsClass>.Instance.Game.Settings.MalfunctionVisability)
			{
				MonoBehaviourSingleton<PreloaderUI>.Instance.MalfunctionGlow.ShowGlow(BattleUIMalfunctionGlow.EGlowType.Repaired, true, method_42());
			}
		}

		public override void vmethod_1(GClass3086 operation, Callback callback)
		{
#if DEBUG
			ConsoleScreen.Log("InvOperation: " + operation.GetType().Name);
#endif

			// Do not replicate picking up quest items, throws an error on the other clients            
			if (operation is GClass3089 moveOperation)
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
							QuestItemPacket packet = new(CoopPlayer.Profile.Info.MainProfileNickname, lootedItem.TemplateId);
							CoopPlayer.PacketSender.SendPacket(ref packet);
						}
					}
					base.vmethod_1(operation, callback);
					return;
				}
			}

			// Do not replicate quest operations
			// Check for GClass increments, ReadPolymorph
			if (operation is GClass3129 or GClass3130 or GClass3131)
			{
				base.vmethod_1(operation, callback);
				return;
			}

			if (FikaBackendUtils.IsServer)
			{
				HostInventoryOperationManager operationManager = new(this, operation, callback);
				if (vmethod_0(operationManager.operation))
				{
					operationManager.operation.method_0(operationManager.HandleResult);

					InventoryPacket packet = new()
					{
						HasItemControllerExecutePacket = true
					};

					GClass1162 writer = new();
					writer.WritePolymorph(operation.ToDescriptor());
					packet.ItemControllerExecutePacket = new()
					{
						CallbackId = operation.Id,
						OperationBytes = writer.ToArray()
					};

					CoopPlayer.PacketSender.InventoryPackets.Enqueue(packet);

					return;
				}
				operationManager.operation.Dispose();
				operationManager.callback?.Fail($"Can't execute {operationManager.operation}", 1);
			}
			else if (FikaBackendUtils.IsClient)
			{
				InventoryPacket packet = new()
				{
					HasItemControllerExecutePacket = true
				};

				ClientInventoryOperationManager clientOperationManager = new()
				{
					operation = operation,
					callback = callback,
					inventoryController = this
				};

				clientOperationManager.callback ??= new Callback(ClientPlayer.Control0.Class1488.class1488_0.method_0);
				uint operationNum = AddOperationCallback(operation, new Callback<EOperationStatus>(clientOperationManager.HandleResult));

				GClass1162 writer = new();
				writer.WritePolymorph(operation.ToDescriptor());
				packet.ItemControllerExecutePacket = new()
				{
					CallbackId = operationNum,
					OperationBytes = writer.ToArray()
				};

				CoopPlayer.PacketSender.InventoryPackets.Enqueue(packet);
			}
		}

		private uint AddOperationCallback(GClass3086 operation, Callback<EOperationStatus> callback)
		{
			ushort id = operation.Id;
			CoopPlayer.OperationCallbacks.Add(id, callback);
			return id;
		}

		public override SearchContentOperation vmethod_2(SearchableItemClass item)
		{
			return new GClass3124(method_12(), this, PlayerSearchController, Profile, item);
		}

		private class HostInventoryOperationManager(CoopClientInventoryController inventoryController, GClass3086 operation, Callback callback)
		{
			public readonly CoopClientInventoryController inventoryController = inventoryController;
			public GClass3086 operation = operation;
			public readonly Callback callback = callback;

			public void HandleResult(IResult result)
			{
				if (!result.Succeed)
				{
					FikaPlugin.Instance.FikaLogger.LogError($"[{Time.frameCount}][{inventoryController.Name}] {inventoryController.ID} - Local operation failed: {operation.Id} - {operation}\r\nError: {result.Error}");
				}
				callback?.Invoke(result);
			}
		}

		private class ClientInventoryOperationManager
		{
			public EOperationStatus? serverOperationStatus;
			public EOperationStatus? localOperationStatus;
			public GClass3086 operation;
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
					}
				}
				else
				{
					FikaPlugin.Instance.FikaLogger.LogError($"{inventoryController.ID} - Client operation rejected by server: {operation.Id} - {operation}\r\nReason: {callbackManager.result.Error}");
					serverOperationStatus = EOperationStatus.Failed;
					localOperationStatus = EOperationStatus.Failed;
					operation.Dispose();
					callback.Invoke(callbackManager.result);
				}
			}
		}

		private class ClientInventoryCallbackManager
		{
			public Result<EOperationStatus> result;
			public ClientInventoryOperationManager clientOperationManager;

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