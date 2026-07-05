#if DEBUG
using Fika.Core.Main.Utils;
#endif
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Pooling;

/// <summary>
/// A pool for reusable packets of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The packet type, must be a reference type.</typeparam>
public class PacketPool<T> : IDisposable
    where T : class
{
    /// <summary>
    /// Internal stack storing available packet instances.
    /// </summary>
    private readonly Stack<T> _pool;

    /// <summary>
    /// Function to construct new instances when pool is empty.
    /// </summary>
    private readonly Func<T> _constructor;

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketPool{T}"/> class.
    /// Pre-allocates <paramref name="size"/> instances.
    /// </summary>
    /// <param name="size">Initial pool size.</param>
    /// <param name="constructor">Factory function to create new instances.</param>
    public PacketPool(int size, Func<T> constructor)
    {
        _pool = new Stack<T>(size);
        _constructor = constructor;
        for (var i = 0; i < size; i++)
        {
            _pool.Push(_constructor());
        }
    }

    /// <summary>
    /// Gets an instance from the pool or creates a new one if empty.
    /// </summary>
    /// <returns>A reusable instance of <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get()
    {
        if (_pool.TryPop(out var item))
        {
            return item;
        }
#if DEBUG
        var concreteType = _constructor.Method.ReturnType;
        FikaGlobals.LogError($"[{concreteType.Name}] Pool empty. Allocating new instance.");
#endif
        return _constructor();
    }

    /// <summary>
    /// Returns an instance back to the pool.
    /// </summary>
    /// <param name="item">The instance to return.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T item)
    {
        _pool.Push(item);
    }

    /// <summary>
    /// Clears the pool and releases references.
    /// </summary>
    public void Dispose()
    {
        _pool.Clear();
    }
}
