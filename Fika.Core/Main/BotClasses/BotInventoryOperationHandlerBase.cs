// © 2026 Lacyway All Rights Reserved

using System;
using Comfort.Common;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.BotClasses;

public sealed class BotInventoryOperationHandler : IDisposable
{
    private BotInventoryController _inventoryController;
    public BaseInventoryOperationClass Operation;
    public Callback Callback;

    public Callback HandleResultDelegate;

    public static BotInventoryOperationHandler CreateInstance()
    {
        return new BotInventoryOperationHandler();
    }

    private BotInventoryOperationHandler()
    {
        HandleResultDelegate = HandleResult;
    }

    public void Set(BotInventoryController controller, BaseInventoryOperationClass operation, Callback callback)
    {
        _inventoryController = controller;
        Operation = operation;
        Callback = callback;
    }

    public void HandleResult(IResult result)
    {
        if (result.Failed)
        {
            FikaGlobals.LogWarning($"BotInventoryOperationHandler: Operation has failed! Controller: {_inventoryController.Name}, Operation ID: {Operation.Id}, Operation: {Operation}, Error: {result.Error}");
        }

        Callback?.Invoke(result);
    }

    public void Dispose()
    {
        _inventoryController = null;
        Operation = null;
        Callback = null;
    }
}