using System;
using Fika.Core.Main.BotClasses;

namespace Fika.Core.Networking.Pooling;

internal sealed class BotInventoryOperationHandlerPool(int size, Func<BotInventoryOperationHandler> constructor)
    : PacketPool<BotInventoryOperationHandler>(size, constructor)
{
    public static BotInventoryOperationHandlerPool Instance { get; private set; }

    /// <summary>
    /// Creates a new instance of the pool
    /// </summary>
    public static void Create()
    {
        Instance = new BotInventoryOperationHandlerPool(8, BotInventoryOperationHandler.CreateInstance);
    }

    /// <summary>
    /// Clears the pool
    /// </summary>
    public static void Clear()
    {
        Instance.Dispose();
        Instance = null;
    }

    public void ReturnHandler(BotInventoryOperationHandler handler)
    {
        handler.Dispose();
        Return(handler);
    }
}
