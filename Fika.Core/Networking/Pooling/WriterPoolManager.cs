using Mirror;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Pooling;

/// <summary>
/// Provides a static pool manager for <see cref="NetworkWriter"/> instances to optimize object reuse and reduce allocations.
/// </summary>
[Obsolete("Use EFTSerializationExtensions instead", true)]
public static class WriterPoolManager
{
    /// <summary>
    /// Initializes the <see cref="WriterPoolManager"/> class and pre-populates the pool with a single <see cref="NetworkWriter"/> instance.
    /// </summary>
    static WriterPoolManager()
    {
        _writers.Push(new());
    }

    /// <summary>
    /// The stack-based pool of <see cref="NetworkWriter"/> objects.
    /// </summary>
    private static readonly Stack<NetworkWriter> _writers = new(1);

    /// <summary>
    /// Returns an <see cref="NetworkWriter"/> instance to the pool for reuse.
    /// </summary>
    /// <param name="writer">The <see cref="NetworkWriter"/> instance to return to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use EFTSerializationExtensions instead", true)]
    public static void ReturnWriter(NetworkWriter writer)
    {
        _writers.Push(writer);
    }

    /// <summary>
    /// Retrieves an <see cref="NetworkWriter"/> instance from the pool. If the pool is empty, a new instance is created.
    /// The returned writer is reset before being provided.
    /// </summary>
    /// <returns>An <see cref="NetworkWriter"/> instance ready for use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use EFTSerializationExtensions instead", true)]
    public static NetworkWriter GetWriter()
    {
        if (_writers.Count == 0)
        {
            return new();
        }

        var writer = _writers.Pop();
        writer.Reset();
        return writer;
    }
}
