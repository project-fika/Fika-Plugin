using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Pooling;

/// <summary>
/// Provides methods to rent and return pooled <see cref="ArraySegment{Byte}"/> instances
/// backed by arrays from the shared <see cref="ArrayPool{Byte}"/>.
/// </summary>
internal static class ArraySegmentPooling
{
    /// <summary>
    /// Rents a byte array from the shared pool with a specified minimum length
    /// and returns it wrapped in an <see cref="ArraySegment{Byte}"/> with zero length.
    /// </summary>
    /// <param name="bufferCount">The minimum length of the array to rent.</param>
    /// <returns>
    /// An <see cref="ArraySegment{Byte}"/> wrapping the rented array with length zero.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArraySegment<byte> Get(int bufferCount)
    {
        return new(ArrayPool<byte>.Shared.Rent(bufferCount), 0, 0);
    }

    /// <summary>
    /// Copies the specified buffer into a rented array and returns it as an <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    /// <param name="buffer">The source buffer to copy from.</param>
    /// <returns>
    /// An <see cref="ArraySegment{Byte}"/> wrapping the rented array containing the copied data.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArraySegment<byte> Get(byte[] buffer)
    {
        return Get(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// Copies the contents of the specified <see cref="ArraySegment{Byte}"/> into a rented array
    /// and returns it wrapped as a new <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    /// <param name="buffer">The source array segment to copy.</param>
    /// <returns>
    /// An <see cref="ArraySegment{Byte}"/> wrapping the rented array containing the copied data.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArraySegment<byte> Get(ArraySegment<byte> buffer)
    {
        return Get(buffer.Array, buffer.Offset, buffer.Count);
    }

    /// <summary>
    /// Copies the contents of the specified <see cref="ReadOnlySpan{Byte}"/> into a rented array
    /// and returns it wrapped as a new <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    /// <param name="source">The source span to copy.</param>
    /// <returns>
    /// An <see cref="ArraySegment{Byte}"/> wrapping the rented array containing the copied data.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArraySegment<byte> Get(ReadOnlySpan<byte> source)
    {
        byte[] array = ArrayPool<byte>.Shared.Rent(source.Length);
        source.CopyTo(array);
        return new(array, 0, source.Length);
    }

    /// <summary>
    /// Copies a portion of the specified buffer into a rented array and returns it as an <see cref="ArraySegment{Byte}"/>.
    /// </summary>
    /// <param name="buffer">The source buffer to copy from.</param>
    /// <param name="bufferOffset">The zero-based byte offset in the source buffer.</param>
    /// <param name="bufferCount">The number of bytes to copy.</param>
    /// <returns>
    /// An <see cref="ArraySegment{Byte}"/> wrapping the rented array containing the copied data.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArraySegment<byte> Get(byte[] buffer, int bufferOffset, int bufferCount)
    {
        byte[] array = ArrayPool<byte>.Shared.Rent(bufferCount);
        Buffer.BlockCopy(buffer, bufferOffset, array, 0, bufferCount);
        return new(array, 0, bufferCount);
    }

    /// <summary>
    /// Returns a rented array back to the shared pool.
    /// </summary>
    /// <param name="buffer">The <see cref="ArraySegment{Byte}"/> whose underlying array should be returned.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(ArraySegment<byte> buffer)
    {
        ArrayPool<byte>.Shared.Return(buffer.Array, false);
    }
}