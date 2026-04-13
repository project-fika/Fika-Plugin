using System;

namespace Fika.Core.Networking.Pooling;

internal sealed class InventoryOperationHandlerPool(int size, Func<InventoryOperationHandler> constructor)
    : PacketPool<InventoryOperationHandler>(size, constructor)
{
    public void ReturnHandler(InventoryOperationHandler handler)
    {
        handler.Dispose();
        Return(handler);
    }
}
