// © 2026 Lacyway All Rights Reserved

using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.GameTriggers;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Pooling;
using JetBrains.Annotations;
using static EFT.Player;

namespace Fika.Core.Main.BotClasses;

public sealed class BotInventoryController : PlayerInventoryController
{
    public override bool HasDiscardLimits
    {
        get
        {
            return false;
        }
    }
    private readonly FikaBot _fikaBot;
    private readonly BotInventoryOperationHandlerPool _botInventoryOperationHandlerPool;

    public BotInventoryController(Player player, Profile profile, bool examined, MongoID currentId, ushort nextOperationId) : base(player, profile, examined)
    {
        _fikaBot = (FikaBot)player;
        MongoID_0 = currentId;
        Ushort_0 = nextOperationId;
        PlayerSearchController = new BotSearchControllerClass(profile);
        _botInventoryOperationHandlerPool = BotInventoryOperationHandlerPool.Instance;
    }

    public override IPlayerSearchController PlayerSearchController { get; }

    public override void CallMalfunctionRepaired(Weapon weapon)
    {
        // Do nothing
    }

    public override void vmethod_1(BaseInventoryOperationClass operation, [CanBeNull] Callback callback)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Sending bot operation {operation.GetType()} from {_fikaBot.Profile.Nickname}");
#endif
        _fikaBot.PacketSender.NetworkManager.SendGenericPacket(EGenericSubPacketType.InventoryOperation,
            InventoryPacket.FromValue(_fikaBot.NetId, operation), true);
        HandleOperation(operation, callback).HandleExceptions();
    }

    /// <summary>
    /// Override to not replicate the tripwire
    /// </summary>
    public override void PlantTripwire(ThrowWeapItemClass grenade, PlantingKitsItemClass plantingKit, Vector3 fromPosition, Vector3 toPosition, Callback callback = null)
    {
        var gstruct = InteractionsHandlerClass.SimulatePlantTripwire(this, grenade, plantingKit);
        if (!gstruct.Failed)
        {
            HandleOperation(new GClass3492(method_12(), this, gstruct.Value, fromPosition, toPosition, _fikaBot), callback)
                .HandleExceptions();
            return;
        }
        callback?.Invoke(gstruct.ToResult());
    }

    private async Task HandleOperation(BaseInventoryOperationClass operation, Callback callback)
    {
        if (_fikaBot.HealthController.IsAlive)
        {
            await Task.Yield();
        }
        RunBotOperation(operation, callback);
    }

    private void RunBotOperation(BaseInventoryOperationClass operation, Callback callback)
    {
        var handler = _botInventoryOperationHandlerPool.Get();
        handler.Set(this, operation, callback);
        try
        {
            if (vmethod_0(operation))
            {
                handler.Operation.method_1(handler.HandleResult);
                return;
            }
            handler.Operation.Dispose();
            handler.Callback?.Fail($"Can't execute {handler.Operation}", 1);
        }
        finally
        {
            _botInventoryOperationHandlerPool.ReturnHandler(handler);
        }
    }

    public override SearchContentOperation vmethod_2(SearchableItemItemClass item)
    {
        return new SearchContentOperationResultClass(method_12(), this, PlayerSearchController, Profile, item);
    }
}
