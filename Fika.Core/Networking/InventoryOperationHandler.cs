using System;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Packets.Communication;
#if DEBUG
#endif
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Generic.SubPackets;
#if DEBUG
#endif

namespace Fika.Core.Networking;

public sealed class InventoryOperationHandler : IDisposable
{
    public void Set(OperationDataStruct operationResult, ushort operationId, int netId, NetPeer peer, FikaServer server)
    {
        OperationResult = operationResult;
        _operationId = operationId;
        _netId = netId;
        _peer = peer;
        _server = server;
    }

    private InventoryOperationHandler()
    {
        HandleResultDelegate = HandleResult;
    }

    public static InventoryOperationHandler CreateInstance()
    {
        return new InventoryOperationHandler();
    }

    public Callback HandleResultDelegate;
    public OperationDataStruct OperationResult;

    private ushort _operationId;
    private int _netId;
    private NetPeer _peer;
    private FikaServer _server;

    public void HandleResult(IResult result)
    {
        if (!result.Succeed)
        {
            FikaGlobals.LogError($"Error in operation: {result.Error ?? "An unknown error has occured"}");
            _server.SendGenericPacketToPeer(EGenericSubPacketType.OperationCallback,
                        OperationCallbackPacket.FromValue(_netId, _operationId, EOperationStatus.Failed,
                        result.Error ?? "An unknown error has occured"), _peer);

            ResyncInventoryIdPacket resyncPacket = new(_netId);
            _server.SendDataToPeer(ref resyncPacket, DeliveryMethod.ReliableOrdered, _peer);

            return;
        }

        _server.SendGenericPacketToPeer(EGenericSubPacketType.OperationCallback,
                        OperationCallbackPacket.FromValue(_netId, _operationId, EOperationStatus.Succeeded), _peer);
    }

    public void Dispose()
    {
        OperationResult = default;
        _operationId = default;
        _netId = default;
        _peer = null;
        _server = null;
    }
}