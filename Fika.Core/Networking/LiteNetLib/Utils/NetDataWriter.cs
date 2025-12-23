using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Fika.Core.Networking.LiteNetLib.Utils;

public unsafe class NetDataWriter
{
    protected byte[] _data;
    protected int _position;
    private const int _initialSize = 64;
    private readonly bool _autoResize;

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Length;
    }
    public byte[] Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data;
    }
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
    }

    /// <summary>
    /// Returns a new <see cref="ReadOnlySpan{T}"/>(<see cref="byte"/>) of the <see cref="Data"/>
    /// </summary>
    public ReadOnlySpan<byte> AsReadOnlySpan
    {
        get
        {
            return new(Data, 0, Length);
        }
    }

    [ThreadStatic]
    private static UTF8Encoding _utf8EncodingInternal;

    public static UTF8Encoding UTF8Encoding => _utf8EncodingInternal ??= new UTF8Encoding(false, true);

    public NetDataWriter() : this(true, _initialSize)
    {
    }

    public NetDataWriter(bool autoResize) : this(autoResize, _initialSize)
    {
    }

    public NetDataWriter(bool autoResize, int initialSize)
    {
        _data = new byte[initialSize];
        _autoResize = autoResize;
    }

    /// <summary>
    /// Creates NetDataWriter from existing ByteArray
    /// </summary>
    /// <param name="bytes">Source byte array</param>
    /// <param name="copy">Copy array to new location or use existing</param>
    public static NetDataWriter FromBytes(byte[] bytes, bool copy)
    {
        if (copy)
        {
            var netDataWriter = new NetDataWriter(true, bytes.Length);
            netDataWriter.Put(bytes);
            return netDataWriter;
        }
        return new NetDataWriter(true, 0) { _data = bytes, _position = bytes.Length };
    }

    /// <summary>
    /// Creates NetDataWriter from existing ByteArray (always copied data)
    /// </summary>
    /// <param name="bytes">Source byte array</param>
    /// <param name="offset">Offset of array</param>
    /// <param name="length">Length of array</param>
    public static NetDataWriter FromBytes(byte[] bytes, int offset, int length)
    {
        var netDataWriter = new NetDataWriter(true, bytes.Length);
        netDataWriter.Put(bytes, offset, length);
        return netDataWriter;
    }

    /// <summary>
    /// Creates NetDataWriter from the given <paramref name="bytes"/>.
    /// </summary>
    public static NetDataWriter FromBytes(Span<byte> bytes)
    {
        var netDataWriter = new NetDataWriter(true, bytes.Length);
        netDataWriter.Put(bytes);
        return netDataWriter;
    }

    public static NetDataWriter FromString(string value)
    {
        var netDataWriter = new NetDataWriter();
        netDataWriter.Put(value);
        return netDataWriter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResizeIfNeed(int newSize)
    {
        if (_data.Length < newSize)
        {
            Array.Resize(ref _data, Math.Max(newSize, _data.Length * 2));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureFit(int additionalSize)
    {
        if (_data.Length < _position + additionalSize)
        {
            Array.Resize(ref _data, Math.Max(_position + additionalSize, _data.Length * 2));
        }
    }

    public void Reset(int size)
    {
        ResizeIfNeed(size);
        _position = 0;
    }

    public void Reset()
    {
        _position = 0;
    }

    public byte[] CopyData()
    {
        var resultData = new byte[_position];
        Buffer.BlockCopy(_data, 0, resultData, 0, _position);
        return resultData;
    }

    /// <summary>
    /// Sets position of NetDataWriter to rewrite previous values
    /// </summary>
    /// <param name="position">new byte position</param>
    /// <returns>previous position of data writer</returns>
    public int SetPosition(int position)
    {
        var prevPosition = _position;
        _position = position;
        return prevPosition;
    }

    public void Put(float value)
    {
        PutUnmanaged(value);
    }

    public void Put(double value)
    {
        PutUnmanaged(value);
    }

    public void Put(long value)
    {
        PutUnmanaged(value);
    }

    public void Put(ulong value)
    {
        PutUnmanaged(value);
    }

    public void Put(int value)
    {
        PutUnmanaged(value);
    }

    public void Put(uint value)
    {
        PutUnmanaged(value);
    }

    public void Put(char value)
    {
        PutUnmanaged((ushort)value);
    }

    public void Put(ushort value)
    {
        PutUnmanaged(value);
    }

    public void Put(short value)
    {
        PutUnmanaged(value);
    }

    public void Put(sbyte value)
    {
        if (_autoResize)
        {
            ResizeIfNeed(_position + 1);
        }

        _data[_position] = (byte)value;
        _position++;
    }

    public void Put(byte value)
    {
        if (_autoResize)
        {
            ResizeIfNeed(_position + 1);
        }

        _data[_position] = value;
        _position++;
    }

    public void Put(Guid value)
    {
        if (_autoResize)
        {
            ResizeIfNeed(_position + 16);
        }

        value.TryWriteBytes(_data.AsSpan(_position));
        _position += 16;
    }

    public void Put(byte[] data, int offset, int length)
    {
        Put(data.AsSpan(offset, length));
    }

    public void Put(byte[] data)
    {
        Put(data.AsSpan());
    }

    public void Put(ReadOnlySpan<byte> data)
    {
        if (_autoResize)
        {
            ResizeIfNeed(_position + data.Length);
        }

        data.CopyTo(_data.AsSpan(_position));
        _position += data.Length;
    }

    public void PutSBytesWithLength(sbyte[] data, int offset, ushort length)
    {
        if (_autoResize)
        {
            ResizeIfNeed(_position + 2 + length);
        }

        FastBitConverter.GetBytes(_data, _position, length);
        Buffer.BlockCopy(data, offset, _data, _position + 2, length);
        _position += 2 + length;
    }

    public void PutSBytesWithLength(sbyte[] data)
    {
        PutArray(data, 1);
    }

    public void PutBytesWithLength(byte[] data, int offset, ushort length)
    {
        PutBytesWithLength(data.AsSpan(offset, length));
    }

    public void PutBytesWithLength(ReadOnlySpan<byte> data)
    {
        if (_autoResize)
        {
            ResizeIfNeed(_position + 2 + data.Length);
        }

        FastBitConverter.GetBytes(_data, _position, (ushort)data.Length);

        Span<byte> destSpan = new(_data, _position + 2, data.Length);
        data.CopyTo(destSpan);

        _position += 2 + data.Length;
    }

    public void PutBytesWithLength(byte[] data)
    {
        PutArray(data, 1);
    }

    public void Put(bool value)
    {
        Put((byte)(value ? 1 : 0));
    }

    public void PutArray(Array arr, int sz)
    {
        var length = arr == null ? (ushort)0 : (ushort)arr.Length;
        sz *= length;
        if (_autoResize)
        {
            ResizeIfNeed(_position + sz + 2);
        }

        FastBitConverter.GetBytes(_data, _position, length);
        if (arr != null)
        {
            Buffer.BlockCopy(arr, 0, _data, _position + 2, sz);
        }

        _position += sz + 2;
    }

    public void PutArray(float[] value)
    {
        PutArray(value, 4);
    }

    public void PutArray(double[] value)
    {
        PutArray(value, 8);
    }

    public void PutArray(long[] value)
    {
        PutArray(value, 8);
    }

    public void PutArray(ulong[] value)
    {
        PutArray(value, 8);
    }

    public void PutArray(int[] value)
    {
        PutArray(value, 4);
    }

    public void PutArray(uint[] value)
    {
        PutArray(value, 4);
    }

    public void PutArray(ushort[] value)
    {
        PutArray(value, 2);
    }

    public void PutArray(short[] value)
    {
        PutArray(value, 2);
    }

    public void PutArray(bool[] value)
    {
        PutArray(value, 1);
    }

    public void PutArray(string[] value)
    {
        var strArrayLength = value == null ? (ushort)0 : (ushort)value.Length;
        Put(strArrayLength);
        for (var i = 0; i < strArrayLength; i++)
        {
            Put(value[i]);
        }
    }

    public void PutArray(string[] value, int strMaxLength)
    {
        var strArrayLength = value == null ? (ushort)0 : (ushort)value.Length;
        Put(strArrayLength);
        for (var i = 0; i < strArrayLength; i++)
        {
            Put(value[i], strMaxLength);
        }
    }

    public void PutArray<T>(T[] value) where T : INetSerializable, new()
    {
        var strArrayLength = (ushort)(value?.Length ?? 0);
        Put(strArrayLength);
        for (var i = 0; i < strArrayLength; i++)
        {
            value[i].Serialize(this);
        }
    }

    public void Put(IPEndPoint endPoint)
    {
        if (endPoint.AddressFamily == AddressFamily.InterNetwork)
        {
            Put((byte)0);
        }
        else if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            Put((byte)1);
        }
        else
        {
            throw new ArgumentException("Unsupported address family: " + endPoint.AddressFamily);
        }

        Put(endPoint.Address.GetAddressBytes());
        Put((ushort)endPoint.Port);
    }

    public void PutLargeString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Put(0);
            return;
        }
        var size = UTF8Encoding.GetByteCount(value);
        if (size == 0)
        {
            Put(0);
            return;
        }
        Put(size);
        if (_autoResize)
        {
            ResizeIfNeed(_position + size);
        }

        UTF8Encoding.GetBytes(value, 0, size, _data, _position);
        _position += size;
    }

    public void Put(string value)
    {
        Put(value, 0);
    }

    /// <summary>
    /// Note that "maxLength" only limits the number of characters in a string, not its size in bytes.
    /// </summary>
    public void Put(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            Put((ushort)0);
            return;
        }

        var length = maxLength > 0 && value.Length > maxLength ? maxLength : value.Length;
        var maxSize = UTF8Encoding.GetMaxByteCount(length);
        if (_autoResize)
        {
            ResizeIfNeed(_position + maxSize + sizeof(ushort));
        }

        var size = UTF8Encoding.GetBytes(value, 0, length, _data, _position + sizeof(ushort));
        if (size == 0)
        {
            Put((ushort)0);
            return;
        }
        Put(checked((ushort)(size + 1)));
        _position += size;
    }

    public void Put<T>(T obj) where T : INetSerializable
    {
        obj.Serialize(this);
    }

    /// <summary>
    /// Writes a nullable value of type <typeparamref name="T"/> into the internal byte buffer at the current position,
    /// first writing a <see cref="bool"/> indicating whether the value is present, 
    /// and then writing the value itself if it exists. <br/> Advances the position by 1 byte for the presence flag plus
    /// the size of <typeparamref name="T"/> if the value is present.
    /// </summary>
    /// <typeparam name="T">An unmanaged value type to write into the buffer.</typeparam>
    /// <param name="value">The nullable value to write into the buffer. If <see langword="null"/>, only a <see langword="false"/> flag is written.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PutUnmanaged<T>(T value) where T : unmanaged
    {
        if (_autoResize)
        {
            ResizeIfNeed(_position + sizeof(T));
        }

        fixed (byte* ptr = &_data[_position])
        {
            *(T*)ptr = value;
        }

        _position += sizeof(T);
    }

    /// <summary>
    /// Writes a value of type <typeparamref name="T"/> into the internal byte buffer at the current position,
    /// advancing the position by the size of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">An unmanaged value type to write into the buffer.</typeparam>
    /// <param name="value">The value to write into the buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PutNullableUnmanaged<T>(T? value) where T : unmanaged
    {
        var hasValue = value.HasValue;
        Put(hasValue);
        if (!hasValue)
        {
            return;
        }

        PutUnmanaged(value.Value);
    }

    /// <summary>
    /// Writes an enum value of type <typeparamref name="T"/> to the internal data buffer at the current position. <br/>
    /// Automatically resizes the buffer if <see cref="_autoResize"/> is enabled.
    /// Advances the position by the size of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">An unmanaged enum type to write.</typeparam>
    /// <param name="value">The enum value to write.</param>
    public void PutEnum<T>(T value) where T : unmanaged, Enum
    {
        var size = Unsafe.SizeOf<T>();
        if (_autoResize)
        {
            ResizeIfNeed(_position + size);
        }

        fixed (byte* ptr = &_data[_position])
        {
            *(T*)ptr = value;
        }

        _position += size;
    }

    /// <summary>
    /// Serializes a <see cref="DateTime"/> to the <paramref name="writer"/>
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to serialize</param>
    public void PutDateTime(DateTime dateTime)
    {
        Put(dateTime.ToOADate());
    }
}