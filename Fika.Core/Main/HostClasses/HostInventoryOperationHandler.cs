using System;
using Comfort.Common;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.HostClasses;

public sealed class HostInventoryOperationHandler : IDisposable
{
    public HostInventoryController InventoryController;
    public BaseInventoryOperationClass Operation;
    public Callback Callback;

    public Callback HandleResultDelegate;

    public static HostInventoryOperationHandler CreateInstance()
    {
        return new HostInventoryOperationHandler();
    }

    private HostInventoryOperationHandler()
    {
        HandleResultDelegate = HandleResult;
    }

    public void Set(HostInventoryController inventoryController, BaseInventoryOperationClass operation, Callback callback)
    {
        InventoryController = inventoryController;
        Operation = operation;
        Callback = callback;
    }

    public void HandleResult(IResult result)
    {
        if (!result.Succeed)
        {
            FikaGlobals.LogError($"[{Time.frameCount}][{InventoryController.Name}] {InventoryController.ID} - Local operation failed: {Operation.Id} - {Operation}\r\nError: {result.Error}");
        }
        Callback?.Invoke(result);
    }

    public void Dispose()
    {
        InventoryController = null;
        Operation = null;
        Callback = null;
    }
}