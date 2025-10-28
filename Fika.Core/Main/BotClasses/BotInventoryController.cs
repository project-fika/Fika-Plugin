﻿// © 2025 Lacyway All Rights Reserved

using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.InventoryLogic.Operations;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using JetBrains.Annotations;
using System;
using System.Threading.Tasks;
using static EFT.Player;

namespace Fika.Core.Main.BotClasses;

public class BotInventoryController : PlayerInventoryController
{
    public override bool HasDiscardLimits
    {
        get
        {
            return false;
        }
    }
    private readonly FikaBot _fikaBot;
    private readonly IPlayerSearchController _searchController;

    public BotInventoryController(Player player, Profile profile, bool examined, MongoID currentId, ushort nextOperationId) : base(player, profile, examined)
    {
        _fikaBot = (FikaBot)player;
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
#if DEBUG
        FikaPlugin.Instance.FikaLogger.LogInfo($"Sending bot operation {operation.GetType()} from {_fikaBot.Profile.Nickname}");
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

    private class BotInventoryOperationHandler(BotInventoryController controller, BaseInventoryOperationClass operation, Callback callback)
    {
        private readonly BotInventoryController controller = controller;
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
