// © 2026 Lacyway All Rights Reserved

using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Main.BaseClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using Fika.Core.Networking.Pooling;
using JetBrains.Annotations;
using Diz.LanguageExtensions;
using static EFT.Player;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.BotClasses;

public sealed class BotInventoryController : BaseInventoryController
{
    public BotInventoryController(IntPtr pointer) : base(pointer)
    {
    }

    public override bool HasDiscardLimits
    {
        get
        {
            return false;
        }
    }
    private readonly FikaBot _fikaBot;
    private readonly BotInventoryOperationHandlerPool _botInventoryOperationHandlerPool;

    public BotInventoryController(Player player, Profile profile, bool examined, MongoID currentId, ushort nextOperationId) : base(Il2CppInjection.Allocate<BotInventoryController>(), player, profile, examined, false)
    {
        _fikaBot = (FikaBot)player;
        IdSource = currentId;
        _nextOperationId = nextOperationId;
        PlayerSearchController = new BotSearchController(profile);
        _botInventoryOperationHandlerPool = BotInventoryOperationHandlerPool.Instance;
    }

    public override IPlayerSearchController PlayerSearchController { get; }

    public override void CallMalfunctionRepaired(Weapon weapon)
    {
        // Do nothing
    }

    /// <summary>
    /// Gets an inventory handler
    /// </summary>
    /// <returns>A pooled handler</returns>
    public BotInventoryOperationHandler GetHandler()
    {
        return _botInventoryOperationHandlerPool.Get();
    }

    /// <summary>
    /// Returns a handler
    /// </summary>
    /// <param name="handler">The handler to return</param>
    public void ReturnHandler(BotInventoryOperationHandler handler)
    {
        _botInventoryOperationHandlerPool.ReturnHandler(handler);
    }

    public override void Execute(EFT.InventoryLogic.Operations.AbstractOperation operation, Callback callback)
    {
#if DEBUG
        FikaGlobals.LogInfo($"Sending bot operation {operation.GetType()} from {_fikaBot.Profile.Nickname}");
#endif
        _fikaBot.PacketSender.NetworkManager.SendGenericPacket(EGenericSubPacketType.InventoryOperation,
            InventoryPacket.FromValue(_fikaBot.NetId, operation), true);
        HandleOperation(operation, callback).Forget();
    }

    /// <summary>
    /// Override to not replicate the tripwire
    /// </summary>
    public override void PlantTripwire(ThrowWeap grenade, PlantingKit plantingKit, Vector3 fromPosition, Vector3 toPosition, Callback callback = null)
    {
        var gstruct = ItemManipulator.SimulatePlantTripwire(this, grenade, plantingKit);
        if (!gstruct.Failed)
        {
            RunBotOperation(new PlantTripwireOperation(GetAndIncrementNextOperationId(), this, gstruct.Value, fromPosition, toPosition, _fikaBot), callback);
            return;
        }
        callback?.Invoke(gstruct.ToResult());
    }

    private async Task HandleOperation(EFT.InventoryLogic.Operations.AbstractOperation operation, Callback callback)
    {
        if (_fikaBot.HealthController.IsAlive)
        {
            await Task.Yield();
        }
        RunBotOperation(operation, callback);
    }

    private void RunBotOperation(EFT.InventoryLogic.Operations.AbstractOperation operation, Callback callback)
    {
        var handler = _botInventoryOperationHandlerPool.Get();
        handler.Set(this, operation, callback);
        try
        {
            if (CanExecute(operation))
            {
                handler.Operation.Execute(new System.Action<Comfort.Common.IResult>(handler.HandleResult));
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

    public override SearchContentOperation CreateSearchOperation(SearchableItem item)
    {
        return new SinglePlayerSearchContentOperation(GetAndIncrementNextOperationId(), this, PlayerSearchController, Profile, item);
    }
}
