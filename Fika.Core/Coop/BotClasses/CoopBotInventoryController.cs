// © 2025 Lacyway All Rights Reserved

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
        private readonly CoopBot _coopBot;
        private readonly IPlayerSearchController _searchController;

        public CoopBotInventoryController(Player player, Profile profile, bool examined, MongoID currentId, ushort nextOperationId) : base(player, profile, examined)
        {
            _coopBot = (CoopBot)player;
            MongoID_0 = currentId;
            Ushort_0 = nextOperationId;
            _searchController = new BotSearchControllerClass(profile);
        }

        public override IPlayerSearchController PlayerSearchController
        {
            get
            {
                return _searchController;
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
            if (operation is not GClass3359)
            {
#if DEBUG
                FikaPlugin.Instance.FikaLogger.LogInfo($"Sending bot operation {operation.GetType()} from {_coopBot.Profile.Nickname}");
#endif
                EFTWriterClass eftWriter = new();
                eftWriter.WritePolymorph(operation.ToDescriptor());
                InventoryPacket packet = new()
                {
                    NetId = _coopBot.NetId,
                    CallbackId = operation.Id,
                    OperationBytes = eftWriter.ToArray()
                };

                _coopBot.PacketSender.SendPacket(ref packet);
            }
            HandleOperation(operation, callback).HandleExceptions();
        }

        private async Task HandleOperation(BaseInventoryOperationClass operation, Callback callback)
        {
            if (_coopBot.HealthController.IsAlive)
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
            return new SearchContentOperationResultClass(method_12(), this, PlayerSearchController, Profile, item);
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
