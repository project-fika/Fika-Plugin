using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fika.Core.Networking.LiteNetLib.Utils;

public unsafe class NetDataReader
{
    protected byte[] _data;
    protected int _position;
    protected int _dataSize;
    protected int _offset;

    public byte[] RawData
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data;
    }
    public int RawDataSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _dataSize;
    }
    public int UserDataOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _offset;
    }
    public int UserDataSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _dataSize - _offset;
    }
    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data == null;
    }
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
    }
    public bool EndOfData
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position == _dataSize;
    }
    public int AvailableBytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _dataSize - _position;
    }

    public void SkipBytes(int count)
    {
        _position += count;
    }

    public void SetPosition(int position)
    {
        _position = position;
    }

    public void SetSource(NetDataWriter dataWriter)
    {
        _data = dataWriter.Data;
        _position = 0;
        _offset = 0;
        _dataSize = dataWriter.Length;
    }

    public void SetSource(byte[] source)
    {
        _data = source;
        _position = 0;
        _offset = 0;
        _dataSize = source.Length;
    }

    public void SetSource(byte[] source, int offset, int maxSize)
    {
        _data = source;
        _position = offset;
        _offset = offset;
        _dataSize = maxSize;
    }

    public NetDataReader()
    {

    }

    public NetDataReader(NetDataWriter writer)
    {
        SetSource(writer);
    }

    public NetDataReader(byte[] source)
    {
        SetSource(source);
    }

    public NetDataReader(byte[] source, int offset, int maxSize)
    {
        SetSource(source, offset, maxSize);
    }

    #region GetMethods

    public void Get<T>(out T result) where T : struct, INetSerializable
    {
        result = default;
        result.Deserialize(this);
    }

    public void Get<T>(out T result, Func<T> constructor) where T : class, INetSerializable
    {
        result = constructor();
        result.Deserialize(this);
    }

    public void Get(out IPEndPoint result)
    {
        result = GetIPEndPoint();
    }

    public IPEndPoint GetIPEndPoint()
    {
        IPAddress address;
        //IPv4
        if (GetByte() == 0)
        {
            address = new IPAddress(new ReadOnlySpan<byte>(_data, _position, 4));
            _position += 4;
        }
        //IPv6
        else
        {
            address = new IPAddress(new ReadOnlySpan<byte>(_data, _position, 16));
            _position += 16;
        }
        return new IPEndPoint(address, GetUShort());
    }

    public void Get(out byte result)
    {
        result = GetByte();
    }

    public void Get(out sbyte result)
    {
        result = (sbyte)GetByte();
    }

    public void Get(out bool result)
    {
        result = GetBool();
    }

    public void Get(out char result)
    {
        result = GetChar();
    }

    public void Get(out ushort result)
    {
        result = GetUShort();
    }

    public void Get(out short result)
    {
        result = GetShort();
    }

    public void Get(out ulong result)
    {
        result = GetULong();
    }

    public void Get(out long result)
    {
        result = GetLong();
    }

    public void Get(out uint result)
    {
        result = GetUInt();
    }

    public void Get(out int result)
    {
        result = GetInt();
    }

    public void Get(out double result)
    {
        result = GetDouble();
    }

    public void Get(out float result)
    {
        result = GetFloat();
    }

    public void Get(out string result)
    {
        result = GetString();
    }

    public void Get(out string result, int maxLength)
    {
        result = GetString(maxLength);
    }

    public void Get(out Guid result)
    {
        result = GetGuid();
    }

    public byte GetByte()
    {
        var res = _data[_position];
        _position++;
        return res;
    }

    public sbyte GetSByte()
    {
        return (sbyte)GetByte();
    }

    public T[] GetArray<T>() where T : unmanaged
    {
        var length = GetUShort();
        var byteLength = length * Unsafe.SizeOf<T>();
        ReadOnlySpan<byte> slice = _data.AsSpan(_position, byteLength);
        var result = MemoryMarshal.Cast<byte, T>(slice)
            .ToArray();
        _position += byteLength;
        return result;
    }

    public T[] GetSerializableArray<T>() where T : INetSerializable, new()
    {
        var length = GetUShort();
        var result = new T[length];
        for (var i = 0; i < length; i++)
        {
            var item = new T();
            item.Deserialize(this);
            result[i] = item;
        }
        return result;
    }

    public T[] GetSerializableConstructorArray<T>(Func<T> constructor) where T : class, INetSerializable
    {
        var length = GetUShort();
        var result = new T[length];
        for (var i = 0; i < length; i++)
        {
            Get(out result[i], constructor);
        }

        return result;
    }

    public bool[] GetBoolArray()
    {
        return GetArray<bool>();
    }

    public ushort[] GetUShortArray()
    {
        return GetArray<ushort>();
    }

    public short[] GetShortArray()
    {
        return GetArray<short>();
    }

    public int[] GetIntArray()
    {
        return GetArray<int>();
    }

    public uint[] GetUIntArray()
    {
        return GetArray<uint>();
    }

    public float[] GetFloatArray()
    {
        return GetArray<float>();
    }

    public double[] GetDoubleArray()
    {
        return GetArray<double>();
    }

    public long[] GetLongArray()
    {
        return GetArray<long>();
    }

    public ulong[] GetULongArray()
    {
        return GetArray<ulong>();
    }

    public string[] GetStringArray()
    {
        var length = GetUShort();
        var arr = new string[length];
        for (var i = 0; i < length; i++)
        {
            arr[i] = GetString();
        }
        return arr;
    }

    /// <summary>
    /// Note that "maxStringLength" only limits the number of characters in a string, not its size in bytes.
    /// Strings that exceed this parameter are returned as empty
    /// </summary>
    public string[] GetStringArray(int maxStringLength)
    {
        var length = GetUShort();
        var arr = new string[length];
        for (var i = 0; i < length; i++)
        {
            arr[i] = GetString(maxStringLength);
        }
        return arr;
    }

    public bool GetBool()
    {
        return GetByte() == 1;
    }

    public char GetChar()
    {
        return (char)GetUShort();
    }

    public ushort GetUShort()
    {
        return GetUnmanaged<ushort>();
    }

    public short GetShort()
    {
        return GetUnmanaged<short>();
    }

    public long GetLong()
    {
        return GetUnmanaged<long>();
    }

    public ulong GetULong()
    {
        return GetUnmanaged<ulong>();
    }

    public int GetInt()
    {
        return GetUnmanaged<int>();
    }

    public uint GetUInt()
    {
        return GetUnmanaged<uint>();
    }

    public float GetFloat()
    {
        return GetUnmanaged<float>();
    }

    public double GetDouble()
    {
        return GetUnmanaged<double>();
    }


    /// <summary>
    /// Note that "maxLength" only limits the number of characters in a string, not its size in bytes.
    /// </summary>
    /// <returns>"string.Empty" if value > "maxLength"</returns>
    public string GetString(int maxLength)
    {
        var size = GetUShort();
        if (size == 0)
        {
            return string.Empty;
        }

        var actualSize = size - 1;
        ReadOnlySpan<byte> slice = _data.AsSpan(_position, actualSize);
        var result = maxLength > 0 && NetDataWriter.UTF8Encoding.GetCharCount(slice) > maxLength ?
            string.Empty :
            NetDataWriter.UTF8Encoding.GetString(slice);

        _position += actualSize;
        return result;
    }

    public string GetString()
    {
        var size = GetUShort();
        if (size == 0)
        {
            return string.Empty;
        }

        var actualSize = size - 1;
        ReadOnlySpan<byte> slice = _data.AsSpan(_position, actualSize);
        var result = NetDataWriter.UTF8Encoding.GetString(slice);

        _position += actualSize;
        return result;
    }

    public string GetLargeString()
    {
        var size = GetInt();
        if (size <= 0)
        {
            return string.Empty;
        }

        var result = NetDataWriter.UTF8Encoding.GetString(_data, _position, size);
        _position += size;
        return result;
    }

    public Guid GetGuid()
    {
        var result = new Guid(_data.AsSpan(_position, 16));
        _position += 16;
        return result;
    }

    public ArraySegment<byte> GetBytesSegment(int count)
    {
        ArraySegment<byte> segment = new(_data, _position, count);
        _position += count;
        return segment;
    }

    public ArraySegment<byte> GetRemainingBytesSegment()
    {
        ArraySegment<byte> segment = new(_data, _position, AvailableBytes);
        _position = _data.Length;
        return segment;
    }

    public T Get<T>() where T : struct, INetSerializable
    {
        var obj = default(T);
        obj.Deserialize(this);
        return obj;
    }

    public T Get<T>(Func<T> constructor) where T : class, INetSerializable
    {
        var obj = constructor();
        obj.Deserialize(this);
        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetRemainingBytesSpan()
    {
        return new ReadOnlySpan<byte>(_data, _position, _dataSize - _position);
    }

    public ReadOnlySpan<byte> GetSpan(int length)
    {
        ReadOnlySpan<byte> span = new(_data, _position, length);
        _position += length;
        return span;
    }

    public byte[] GetRemainingBytes()
    {
        var outgoingData = new byte[AvailableBytes];
        Buffer.BlockCopy(_data, _position, outgoingData, 0, AvailableBytes);
        _position = _data.Length;
        return outgoingData;
    }

    public void GetBytes(byte[] destination, int start, int count)
    {
        Buffer.BlockCopy(_data, _position, destination, start, count);
        _position += count;
    }

    public void GetBytes(byte[] destination, int count)
    {
        Buffer.BlockCopy(_data, _position, destination, 0, count);
        _position += count;
    }

    public sbyte[] GetSBytesWithLength()
    {
        return GetArray<sbyte>();
    }

    public byte[] GetBytesWithLength()
    {
        return GetArray<byte>();
    }

    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from the internal byte buffer at the current position,
    /// advancing the position by the size of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">An unmanaged value type to read from the buffer.</typeparam>
    /// <returns>The value of type <typeparamref name="T"/> read from the buffer.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown in DEBUG mode if there is not enough data remaining in the buffer to read a value of type <typeparamref name="T"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetUnmanaged<T>() where T : unmanaged
    {
        var size = sizeof(T);
#if DEBUG
        if (_position + size > _data.Length)
        {
            throw new IndexOutOfRangeException("Not enough data to read");
        }
#endif

        T value;
        fixed (byte* ptr = &_data[_position])
        {
            value = *(T*)ptr;
        }

        _position += size;
        return value;
    }

    /// <summary>
    /// Reads a nullable value of type <typeparamref name="T"/> from the internal byte buffer at the current position,
    /// first reading a <see cref="bool"/> indicating whether the value is present,
    /// and then reading the value itself if it exists. <br/>
    /// Advances the position by 1 byte for the presence flag plus the size of <typeparamref name="T"/> if the value is present.
    /// </summary>
    /// <typeparam name="T">An unmanaged value type to read from the buffer.</typeparam>
    /// <returns>
    /// The nullable value of type <typeparamref name="T"/> read from the buffer.
    /// Returns <see langword="null"/> if the presence flag indicates no value.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown in DEBUG mode if there is not enough data remaining in the buffer to read the value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetNullableUnmanaged<T>() where T : unmanaged
    {
        var hasValue = GetBool();

#if DEBUG
        var requiredSize = hasValue ? sizeof(T) : 0;
        if (_position + requiredSize > _data.Length)
        {
            throw new IndexOutOfRangeException("Not enough data to read");
        }
#endif

        if (!hasValue)
        {
            return null;
        }

        return GetUnmanaged<T>();
    }

    /// <summary>
    /// Reads an enum value of type <typeparamref name="T"/> from the internal data buffer at the current position. <br/>
    /// Advances the position by the size of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">An unmanaged enum type to read.</typeparam>
    /// <returns>The enum value read from the buffer.</returns>
    public T GetEnum<T>() where T : unmanaged, Enum
    {
        var span = GetSpan(Unsafe.SizeOf<T>());
        fixed (byte* ptr = span)
        {
            return *(T*)ptr;
        }
    }

    /// <summary>
    /// Deserializes a <see cref="DateTime"/> from the <paramref name="reader"/>
    /// </summary>
    /// <returns>The deserialized <see cref="DateTime"/></returns>
    public DateTime GetDateTime()
    {
        return DateTime.FromOADate(GetDouble());
    }
    #endregion

    #region PeekMethods

    public byte PeekByte()
    {
        return _data[_position];
    }

    public sbyte PeekSByte()
    {
        return (sbyte)_data[_position];
    }

    public bool PeekBool()
    {
        return _data[_position] == 1;
    }

    public char PeekChar()
    {
        return (char)PeekUShort();
    }

    /// <summary>
    /// Peeks a value of type <typeparamref name="T"/> from the internal byte buffer at the current position
    /// </summary>
    /// <typeparam name="T">An unmanaged value type to read from the buffer.</typeparam>
    /// <returns>The value of type <typeparamref name="T"/> read from the buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PeekUnmanaged<T>() where T : unmanaged
    {
        T value;
        fixed (byte* ptr = &_data[_position])
        {
            value = *(T*)ptr;
        }
        return value;
    }

    public ushort PeekUShort()
    {
        return PeekUnmanaged<ushort>();
        //return BitConverter.ToUInt16(_data, _position);
    }

    public short PeekShort()
    {
        return PeekUnmanaged<short>();
        //return BitConverter.ToInt16(_data, _position);
    }

    public long PeekLong()
    {
        return PeekUnmanaged<long>();
        //return BitConverter.ToInt64(_data, _position);
    }

    public ulong PeekULong()
    {
        return PeekUnmanaged<ulong>();
        //return BitConverter.ToUInt64(_data, _position);
    }

    public int PeekInt()
    {
        return PeekUnmanaged<int>();
        //return BitConverter.ToInt32(_data, _position);
    }

    public uint PeekUInt()
    {
        return PeekUnmanaged<uint>();
        //return BitConverter.ToUInt32(_data, _position);
    }

    public float PeekFloat()
    {
        return PeekUnmanaged<float>();
        //return BitConverter.ToSingle(_data, _position);
    }

    public double PeekDouble()
    {
        return PeekUnmanaged<double>();
        //return BitConverter.ToDouble(_data, _position);
    }

    /// <summary>
    /// Note that "maxLength" only limits the number of characters in a string, not its size in bytes.
    /// </summary>
    public string PeekString(int maxLength)
    {
        var size = PeekUShort();
        if (size == 0)
        {
            return string.Empty;
        }

        var actualSize = size - 1;
        return maxLength > 0 && NetDataWriter.UTF8Encoding.GetCharCount(_data, _position + 2, actualSize) > maxLength ?
            string.Empty :
            NetDataWriter.UTF8Encoding.GetString(_data, _position + 2, actualSize);
    }

    public string PeekString()
    {
        var size = PeekUShort();
        if (size == 0)
        {
            return string.Empty;
        }

        var actualSize = size - 1;
        return NetDataWriter.UTF8Encoding.GetString(_data, _position + 2, actualSize);
    }
    #endregion

    #region TryGetMethods
    public bool TryGetByte(out byte result)
    {
        if (AvailableBytes >= 1)
        {
            result = GetByte();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetSByte(out sbyte result)
    {
        if (AvailableBytes >= 1)
        {
            result = GetSByte();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetBool(out bool result)
    {
        if (AvailableBytes >= 1)
        {
            result = GetBool();
            return true;
        }
        result = false;
        return false;
    }

    public bool TryGetChar(out char result)
    {
        if (!TryGetUShort(out var uShortValue))
        {
            result = '\0';
            return false;
        }
        result = (char)uShortValue;
        return true;
    }

    public bool TryGetShort(out short result)
    {
        if (AvailableBytes >= 2)
        {
            result = GetShort();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetUShort(out ushort result)
    {
        if (AvailableBytes >= 2)
        {
            result = GetUShort();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetInt(out int result)
    {
        if (AvailableBytes >= 4)
        {
            result = GetInt();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetUInt(out uint result)
    {
        if (AvailableBytes >= 4)
        {
            result = GetUInt();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetLong(out long result)
    {
        if (AvailableBytes >= 8)
        {
            result = GetLong();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetULong(out ulong result)
    {
        if (AvailableBytes >= 8)
        {
            result = GetULong();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetFloat(out float result)
    {
        if (AvailableBytes >= 4)
        {
            result = GetFloat();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetDouble(out double result)
    {
        if (AvailableBytes >= 8)
        {
            result = GetDouble();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetString(out string result)
    {
        if (AvailableBytes >= 2)
        {
            var strSize = PeekUShort();
            if (AvailableBytes >= strSize + 1)
            {
                result = GetString();
                return true;
            }
        }
        result = null;
        return false;
    }

    public bool TryGetStringArray(out string[] result)
    {
        if (!TryGetUShort(out var strArrayLength))
        {
            result = null;
            return false;
        }

        result = new string[strArrayLength];
        for (var i = 0; i < strArrayLength; i++)
        {
            if (!TryGetString(out result[i]))
            {
                result = null;
                return false;
            }
        }

        return true;
    }

    public bool TryGetBytesWithLength(out byte[] result)
    {
        if (AvailableBytes >= 2)
        {
            var length = PeekUShort();
            if (AvailableBytes >= 2 + length)
            {
                result = GetBytesWithLength();
                return true;
            }
        }
        result = null;
        return false;
    }
    #endregion

    public void Clear()
    {
        _position = 0;
        _dataSize = 0;
        _data = null;
    }
}