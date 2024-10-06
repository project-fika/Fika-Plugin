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
			searchController = new GClass1902(profile);
		}

#if DEBUG
		public override void RollBack()
		{
			FikaPlugin.Instance.FikaLogger.LogWarning("Rolling back on bot: " + coopBot.NetId);
			base.RollBack();
		} 
#endif

		public override IPlayerSearchController PlayerSearchController
		{
			get
			{
				return searchController;
			}
		}

		public override void RollBack()
		{
			base.RollBack();
			ResyncInventoryIdPacket packet = new(coopBot.NetId)
			{
				MongoId = mongoID_0
			};
			coopBot.PacketSender.SendPacket(ref packet, false);
		}

		public override void CallMalfunctionRepaired(Weapon weapon)
		{
			// Do nothing
		}

		public override void vmethod_1(GClass3119 operation, [CanBeNull] Callback callback)
		{
			HandleOperation(operation, callback).HandleExceptions();
		}

		private async Task HandleOperation(GClass3119 operation, Callback callback)
		{
			if (coopBot.HealthController.IsAlive)
			{
				await Task.Yield();
			}
			RunBotOperation(operation, callback);
		}

		private void RunBotOperation(GClass3119 operation, Callback callback)
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

		public override SearchContentOperation vmethod_2(SearchableItemClass item)
		{
			return new GClass3158(method_12(), this, PlayerSearchController, Profile, item);
		}

		private class BotInventoryOperationHandler(CoopBotInventoryController controller, GClass3119 operation, Callback callback)
		{
			private readonly CoopBotInventoryController controller = controller;
			public readonly GClass3119 Operation = operation;
			public readonly Callback Callback = callback;

			public void HandleResult(IResult result)
			{
				if (!result.Succeed)
				{
					FikaPlugin.Instance.FikaLogger.LogWarning($"BotInventoryOperationHandler: Operation has failed! Controller: {controller.Name}, Operation ID: {Operation.Id}, Operation: {Operation}, Error: {result.Error}");
					Callback?.Invoke(result);
					return;
				}

				// Check for GClass increments
				// Tripwire kit is always null on AI so we cannot use ToDescriptor as it throws a nullref
				if (Operation is not GClass3137)
				{
#if DEBUG
					FikaPlugin.Instance.FikaLogger.LogInfo($"Sending bot operation {Operation.GetType()} from {controller.coopBot.Profile.Nickname}");
#endif
					InventoryPacket packet = new()
					{
						HasItemControllerExecutePacket = true
					};

					GClass1175 writer = new();
					writer.WritePolymorph(Operation.ToDescriptor());
					packet.ItemControllerExecutePacket = new()
					{
						CallbackId = Operation.Id,
						OperationBytes = writer.ToArray()
					};

					controller.coopBot.PacketSender.InventoryPackets.Enqueue(packet);
				}

				Callback?.Invoke(result);
			}
		}
	}
}
