using EFT.InventoryLogic.Operations;
using System;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using static Fika.Core.Main.ClientClasses.ClientInventoryController;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientInventoryOperationHandler : IDisposable
{
    public ClientInventoryController InventoryController;
    public EFT.InventoryLogic.Operations.AbstractOperation Operation;
    public Callback Callback;

    public IResult OperationResult;
    public ServerOperationStatus ServerStatus;

    public Callback ExecuteResultDelegate;
    public Callback HandleResultDelegate;
    public Action<ServerOperationStatus> ServerStatusDelegate;

    public void Set(ClientInventoryController inventoryController, EFT.InventoryLogic.Operations.AbstractOperation operation, Callback callback)
    {
        InventoryController = inventoryController;
        Operation = operation;
        Callback = callback;
    }

    public static ClientInventoryOperationHandler CreateInstance()
    {
        return new ClientInventoryOperationHandler();
    }

    private ClientInventoryOperationHandler()
    {
        ExecuteResultDelegate = ExecuteResult;
        HandleResultDelegate = HandleResult;
        ServerStatusDelegate = ReceiveStatusFromServer;
    }

    public void ReceiveStatusFromServer(ServerOperationStatus serverStatus)
    {
        ServerStatus = serverStatus;
        switch (serverStatus.Status)
        {
            case EOperationStatus.Started:
                Operation.ExecuteWithoutDispose(ExecuteResultDelegate);
                return;
            case EOperationStatus.Succeeded:
                HandleResultDelegate(SuccessfulResult.New);
                return;
            case EOperationStatus.Failed:
                FikaGlobals.LogError($"{InventoryController.ID} - Client operation rejected by server: {Operation.Id} - {Operation}\r\nReason: {serverStatus.Error}");
                HandleResultDelegate(new FailedResult(serverStatus.Error));
                break;
            default:
                FikaGlobals.LogError("ReceiveStatusFromServer: Status was missing?");
                break;
        }
    }

    private void ExecuteResult(IResult executeResult)
    {
        if (!executeResult.Succeed)
        {
            FikaGlobals.LogError($"{InventoryController.ID} - Client operation critical failure: {Operation.Id} server status:  - {Operation}\r\nError: {executeResult.Error}");
        }
        HandleResultDelegate(executeResult);
    }

    private void HandleResult(IResult result)
    {
        var result2 = OperationResult;
        if (result2?.Failed != true)
        {
            OperationResult = result;
        }
        var serverStatus = ServerStatus.Status;
        if (!serverStatus.Finished())
        {
            return;
        }
        var localStatus = Operation.Status;
        if (localStatus.InProgress())
        {
            if (Operation is IServerDependentOperation ginterface)
            {
                ginterface.Terminate();
            }
            return;
        }
        try
        {
            Operation.Dispose();
            if (serverStatus != localStatus && localStatus.Finished())
            {
                FikaGlobals.LogError($"{InventoryController.ID} - Operation critical failure - status mismatch: {Operation.Id} server status: {serverStatus} client status: {localStatus} - {Operation}");
            }
            Callback?.Invoke(OperationResult);
        }
        finally
        {
            InventoryController.ReturnHandler(this);
        }
    }

    public void Dispose()
    {
        Operation = null;
        Callback = null;
        InventoryController = null;
        OperationResult = null;
        ServerStatus = default;
    }
}