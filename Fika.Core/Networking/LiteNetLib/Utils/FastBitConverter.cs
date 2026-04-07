using System;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.LiteNetLib.Utils;

public static class FastBitConverter
{
    /// <summary>
    /// Converts a value of type <typeparamref name="T"/> into a byte array starting at the specified index.
    /// </summary>
    /// <typeparam name="T">The type of the value to convert. Must be an unmanaged/blittable type.</typeparam>
    /// <param name="bytes">The destination byte array.</param>
    /// <param name="startIndex">The zero-based index in <paramref name="bytes"/> at which to begin writing.</param>
    /// <param name="value">The value to be converted and written.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the <paramref name="bytes"/> array is too small to contain the value at the given <paramref name="startIndex"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void GetBytes<T>(byte[] bytes, int startIndex, T value) where T : unmanaged
    {
        var size = sizeof(T);
        if (bytes.Length < startIndex + size)
        {
            throw new IndexOutOfRangeException();
        }

        Unsafe.WriteUnaligned(ref bytes[startIndex], value);
    }
}
