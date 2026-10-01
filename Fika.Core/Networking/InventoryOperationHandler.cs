using Diz.LanguageExtensions;
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
    public void Set(OperationCreationResult operationResult, ushort operationId, int netId, NetPeer peer, FikaServer server)
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
    public OperationCreationResult OperationResult;

    private ushort _operationId;
    private int _netId;
    private NetPeer _peer;
    private FikaServer _server;

    public void HandleResult(IResult result)
    {
        try
        {
            if (!result.Succeed)
            {
                FikaGlobals.LogError($"Error in operation: {result.Error ?? "An unknown error has occured"}");
                var packet = new OperationCallbackPacket(_netId, _operationId, EOperationStatus.Failed,
                            result.Error ?? "An unknown error has occured");
                _server.SendGenericPacketToPeer(in packet, DeliveryMethod.ReliableOrdered, _peer);

                ResyncInventoryIdPacket resyncPacket = new(_netId);
                _server.SendDataToPeer(ref resyncPacket, DeliveryMethod.ReliableOrdered, _peer);

                return;
            }

            var successPacket = new OperationCallbackPacket(_netId, _operationId, EOperationStatus.Succeeded);
            _server.SendGenericPacketToPeer(in successPacket, DeliveryMethod.ReliableOrdered, _peer);
        }
        finally
        {
            _server.ReturnHandler(this);
        }
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