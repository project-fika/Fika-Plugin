using System;
using Fika.Core.Main.HostClasses;

namespace Fika.Core.Networking.Pooling;

internal sealed class HostInventoryOperationHandlerPool(int size, Func<HostInventoryOperationHandler> constructor)
    : PacketPool<HostInventoryOperationHandler>(size, constructor)
{
    public void ReturnHandler(HostInventoryOperationHandler handler)
    {
        handler.Dispose();
        Return(handler);
    }
}
