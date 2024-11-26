// © 2024 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using JetBrains.Annotations;
using System.Threading.Tasks;
using static EFT.Player;

namespace Fika.Core.Coop.BotClasses
{
	public class CoopBotInventoryController : PlayerInventoryController
	{
		public override bool HasDiscardLimits
		{
			get
			{
				return false;
			}
		}
		private readonly CoopBot coopBot;
		private readonly IPlayerSearchController searchController;

		public CoopBotInventoryController(Player player, Profile profile, bool examined, MongoID currentId, ushort nextOperationId) : base(player, profile, examined)
		{
			coopBot = (CoopBot)player;
			mongoID_0 = currentId;
			ushort_0 = nextOperationId;
			searchController = new GClass1973(profile);
		}

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		public override void CallMalfunctionRepaired(Weapon weapon)
		{
			// Do nothing
		}

		public override void vmethod_1(BaseInventoryOperationClass operation, [CanBeNull] Callback callback)
		{
			// Check for GClass increments
			// Tripwire kit is always null on AI so we cannot use ToDescriptor as it throws a nullref
			if (operation is not GClass3210)
			{
#if DEBUG
				FikaPlugin.Instance.FikaLogger.LogInfo($"Sending bot operation {operation.GetType()} from {coopBot.Profile.Nickname}");
#endif
				GClass1198 writer = new();
				writer.WritePolymorph(operation.ToDescriptor());
				InventoryPacket packet = new()
				{
					CallbackId = operation.Id,
					OperationBytes = writer.ToArray()
				};

				coopBot.PacketSender.InventoryPackets.Enqueue(packet);
			}
			HandleOperation(operation, callback).HandleExceptions();
		}

		private async Task HandleOperation(BaseInventoryOperationClass operation, Callback callback)
		{
			if (coopBot.HealthController.IsAlive)
			{
				await Task.Yield();
			}
			RunBotOperation(operation, callback);
		}

		private void RunBotOperation(BaseInventoryOperationClass operation, Callback callback)
		{
			BotInventoryOperationHandler handler = new(this, operation, callback);
			if (vmethod_0(operation))
			{
				handler.Operation.method_1(handler.HandleResult);
				return;
			}
			handler.Operation.Dispose();
			handler.Callback?.Fail($"Can't execute {handler.Operation}", 1);
		}

		public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
		{
			return new GClass3232(method_12(), this, PlayerSearchController, Profile, item);
		}

		private class BotInventoryOperationHandler(CoopBotInventoryController controller, BaseInventoryOperationClass operation, Callback callback)
		{
			private readonly CoopBotInventoryController controller = controller;
			public readonly BaseInventoryOperationClass Operation = operation;
			public readonly Callback Callback = callback;

			public void HandleResult(IResult result)
			{
				if (result.Failed)
				{
					FikaPlugin.Instance.FikaLogger.LogWarning($"BotInventoryOperationHandler: Operation has failed! Controller: {controller.Name}, Operation ID: {Operation.Id}, Operation: {Operation}, Error: {result.Error}");
				}

				Callback?.Invoke(result);
			}
		}
	}
}
