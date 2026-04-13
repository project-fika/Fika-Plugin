using System;
using Fika.Core.Main.ClientClasses;

namespace Fika.Core.Networking.Pooling;

internal sealed class ClientInventoryOperationHandlerPool(int size, Func<ClientInventoryOperationHandler> constructor)
    : PacketPool<ClientInventoryOperationHandler>(size, constructor)
{
    public void ReturnHandler(ClientInventoryOperationHandler handler)
    {
        handler.Dispose();
        Return(handler);
    }
}
