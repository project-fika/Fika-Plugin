using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Pooling;

/// <summary>
/// Provides a static pool manager for <see cref="EFTWriterClass"/> instances to optimize object reuse and reduce allocations.
/// </summary>
public static class WriterPoolManager
{
    /// <summary>
    /// Initializes the <see cref="WriterPoolManager"/> class and pre-populates the pool with a single <see cref="EFTWriterClass"/> instance.
    /// </summary>
    static WriterPoolManager()
    {
        _writers.Push(new());
    }

    /// <summary>
    /// The stack-based pool of <see cref="EFTWriterClass"/> objects.
    /// </summary>
    private static readonly Stack<EFTWriterClass> _writers = new(1);

    /// <summary>
    /// Returns an <see cref="EFTWriterClass"/> instance to the pool for reuse.
    /// </summary>
    /// <param name="writer">The <see cref="EFTWriterClass"/> instance to return to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnWriter(EFTWriterClass writer)
    {
        _writers.Push(writer);
    }

    /// <summary>
    /// Retrieves an <see cref="EFTWriterClass"/> instance from the pool. If the pool is empty, a new instance is created.
    /// The returned writer is reset before being provided.
    /// </summary>
    /// <returns>An <see cref="EFTWriterClass"/> instance ready for use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EFTWriterClass GetWriter()
    {
        if (_writers.Count == 0)
        {
            return new();
        }

        EFTWriterClass writer = _writers.Pop();
        writer.Reset();
        return writer;
    }
}
