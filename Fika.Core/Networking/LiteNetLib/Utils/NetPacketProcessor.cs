using System;
using System.Collections.Generic;
using Fika.Core.Networking.Packets;

namespace Fika.Core.Networking.LiteNetLib.Utils;

public class NetPacketProcessor
{
    private static class HashCache<T>
    {
        public static readonly ulong Id;

        //FNV-1 64 bit hash
        static HashCache()
        {
            var hash = 14695981039346656037UL; //offset
            var typeName = typeof(T).ToString();
            for (var i = 0; i < typeName.Length; i++)
            {
                hash ^= typeName[i];
                hash *= 1099511628211UL; //prime
            }
            Id = hash;
        }
    }

    private static class ShortHashCache<T>
    {
        public static readonly ushort Id;

        // CRC-16-CCITT
        static ShortHashCache()
        {
            var typeName = typeof(T).ToString();
            const ushort poly = 0x1021;
            ushort crc = 0xFFFF;

            foreach (var c in typeName)
            {
                crc ^= (ushort)(c << 8);
                for (var i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)(crc << 1 ^ poly);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }

            Id = crc;
        }
    }

    protected delegate void SubscribeDelegate(NetDataReader reader, object userData);

    private readonly NetSerializer _netSerializer;
    private readonly Dictionary<ushort, SubscribeDelegate> _callbacks = [];

    public NetPacketProcessor()
    {
        _netSerializer = new NetSerializer();
    }

    public NetPacketProcessor(int maxStringLength)
    {
        _netSerializer = new NetSerializer(maxStringLength);
    }

    protected virtual ulong GetHash<T>()
    {
        return HashCache<T>.Id;
    }

    protected private ushort GetShortHash<T>()
    {
        return ShortHashCache<T>.Id;
    }

    protected virtual SubscribeDelegate GetCallbackFromData(NetDataReader reader)
    {
        var hash = reader.GetUShort();
        if (!_callbacks.TryGetValue(hash, out var action))
        {
            throw new ParseException($"Undefined packet in NetDataReader: {hash}");
        }
        return action;
    }

    protected virtual void WriteHash<T>(NetDataWriter writer)
    {
        writer.Put(GetHash<T>());
    }

    protected virtual void WriteShortHash<T>(NetDataWriter writer)
    {
        writer.Put(GetShortHash<T>());
    }

    /// <summary>
    /// Register nested property type
    /// </summary>
    /// <typeparam name="T">INetSerializable structure</typeparam>
    public void RegisterNestedType<T>() where T : struct, INetSerializable
    {
        _netSerializer.RegisterNestedType<T>();
    }

    /// <summary>
    /// Register nested property type
    /// </summary>
    /// <param name="writeDelegate"></param>
    /// <param name="readDelegate"></param>
    public void RegisterNestedType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate)
    {
        _netSerializer.RegisterNestedType<T>(writeDelegate, readDelegate);
    }

    /// <summary>
    /// Register nested property type
    /// </summary>
    /// <typeparam name="T">INetSerializable class</typeparam>
    public void RegisterNestedType<T>(Func<T> constructor) where T : class, INetSerializable
    {
        _netSerializer.RegisterNestedType(constructor);
    }

    /// <summary>
    /// Reads all available data from NetDataReader and calls OnReceive delegates
    /// </summary>
    /// <param name="reader">NetDataReader with packets data</param>
    public void ReadAllPackets(NetDataReader reader)
    {
        while (reader.AvailableBytes > 0)
        {
            ReadPacket(reader);
        }
    }

    /// <summary>
    /// Reads all available data from NetDataReader and calls OnReceive delegates
    /// </summary>
    /// <param name="reader">NetDataReader with packets data</param>
    /// <param name="userData">Argument that passed to OnReceivedEvent</param>
    /// <exception cref="ParseException">Malformed packet</exception>
    public void ReadAllPackets(NetDataReader reader, object userData)
    {
        while (reader.AvailableBytes > 0)
        {
            ReadPacket(reader, userData);
        }
    }

    /// <summary>
    /// Reads one packet from NetDataReader and calls OnReceive delegate
    /// </summary>
    /// <param name="reader">NetDataReader with packet</param>
    /// <exception cref="ParseException">Malformed packet</exception>
    public void ReadPacket(NetDataReader reader)
    {
        ReadPacket(reader, null);
    }

    public void Write<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
    T>(NetDataWriter writer, T packet) where T : class, new()
    {
        WriteShortHash<T>(writer);
        _netSerializer.Serialize(writer, packet);
    }

    public void WriteNetReusable<T>(NetDataWriter writer, ref T packet) where T : INetReusable
    {
        WriteShortHash<T>(writer);
        packet.Serialize(writer);
    }

    public void WriteNetSerializable<T>(NetDataWriter writer, ref T packet) where T : INetSerializable
    {
        WriteShortHash<T>(writer);
        packet.Serialize(writer);
    }

    /// <summary>
    /// Reads one packet from NetDataReader and calls OnReceive delegate
    /// </summary>
    /// <param name="reader">NetDataReader with packet</param>
    /// <param name="userData">Argument that passed to OnReceivedEvent</param>
    /// <exception cref="ParseException">Malformed packet</exception>
    public void ReadPacket(NetDataReader reader, object userData)
    {
        GetCallbackFromData(reader)(reader, userData);
    }

    /// <summary>
    /// Register and subscribe to packet receive event
    /// </summary>
    /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
    /// <param name="packetConstructor">Method that constructs packet instead of slow Activator.CreateInstance</param>
    /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
    public void Subscribe<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
    T>(Action<T> onReceive, Func<T> packetConstructor) where T : class, new()
    {
        _netSerializer.Register<T>();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            var reference = packetConstructor();
            _netSerializer.Deserialize(reader, reference);
            onReceive(reference);
        };
    }

    /// <summary>
    /// Register and subscribe to packet receive event (with userData)
    /// </summary>
    /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
    /// <param name="packetConstructor">Method that constructs packet instead of slow Activator.CreateInstance</param>
    /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
    public void Subscribe<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
    T, TUserData>(Action<T, TUserData> onReceive, Func<T> packetConstructor) where T : class, new()
    {
        _netSerializer.Register<T>();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            var reference = packetConstructor();
            _netSerializer.Deserialize(reader, reference);
            onReceive(reference, (TUserData)userData);
        };
    }

    /// <summary>
    /// Register and subscribe to packet receive event
    /// This method will overwrite last received packet class on receive (less garbage)
    /// </summary>
    /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
    /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
    public void SubscribeReusable<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
    T>(Action<T> onReceive) where T : class, new()
    {
        _netSerializer.Register<T>();
        var reference = new T();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            _netSerializer.Deserialize(reader, reference);
            onReceive(reference);
        };
    }

    /// <summary>
    /// Register and subscribe to packet receive event
    /// This method will overwrite last received packet class on receive (less garbage)
    /// </summary>
    /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
    /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
    public void SubscribeReusable<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(Trimming.SerializerMemberTypes)]
#endif
    T, TUserData>(Action<T, TUserData> onReceive) where T : class, new()
    {
        _netSerializer.Register<T>();
        var reference = new T();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            _netSerializer.Deserialize(reader, reference);
            onReceive(reference, (TUserData)userData);
        };
    }

    /// <summary>
    /// Register and subscribe to packet receive event
    /// This method will overwrite last received packet class on receive (less garbage)
    /// </summary>
    /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
    /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
    public void SubscribeNetReusable<T>(Action<T> onReceive)
        where T : class, INetReusable, new()
    {
        _netSerializer.Register<T>();
        T reference = new();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            reference.Deserialize(reader);
            onReceive(reference);
            reference.Flush();
        };
    }

    /// <summary>
    /// Register and subscribe to packet receive event
    /// This method will overwrite last received packet class on receive (less garbage)
    /// </summary>
    /// <param name="onReceive">event that will be called when packet deserialized with ReadPacket method</param>
    /// <exception cref="InvalidTypeException"><typeparamref name="T"/>'s fields are not supported, or it has no fields</exception>
    public void SubscribeNetReusable<T, TUserData>(Action<T, TUserData> onReceive)
        where T : class, INetReusable, new()
    {
        _netSerializer.Register<T>();
        T reference = new();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            reference.Deserialize(reader);
            onReceive(reference, (TUserData)userData);
            reference.Flush();
        };
    }

    /// <summary>
    /// Registers a callback for a packet type that implements <see cref="INetSerializable"/>, using a custom constructor and supporting user data.
    /// </summary>
    /// <typeparam name="T">The type of the packet. Must implement <see cref="INetSerializable"/>.</typeparam>
    /// <typeparam name="TUserData">The type of the user data (typically <see cref="NetPeer"/>).</typeparam>
    /// <param name="onReceive">The delegate to be executed when the packet is received.</param>
    /// <param name="packetConstructor">A function that returns a new instance of <typeparamref name="T"/>.</param>
    public void SubscribeNetSerializable<T, TUserData>(
        Action<T, TUserData> onReceive,
        Func<T> packetConstructor) where T : INetSerializable
    {
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            var pkt = packetConstructor();
            pkt.Deserialize(reader);
            onReceive(pkt, (TUserData)userData);
        };
    }

    /// <summary>
    /// Registers a callback for a packet type that implements <see cref="INetSerializable"/>, using a custom constructor.
    /// </summary>
    /// <typeparam name="T">The type of the packet. Must implement <see cref="INetSerializable"/>.</typeparam>
    /// <param name="onReceive">The delegate to be executed when the packet is received.</param>
    /// <param name="packetConstructor">A function that returns a new instance of <typeparamref name="T"/>.</param>
    public void SubscribeNetSerializable<T>(
        Action<T> onReceive,
        Func<T> packetConstructor) where T : INetSerializable
    {
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            var pkt = packetConstructor();
            pkt.Deserialize(reader);
            onReceive(pkt);
        };
    }

    /// <summary>
    /// Registers a callback for a packet type that implements <see cref="INetSerializable"/> and has a parameterless constructor, supporting user data.
    /// </summary>
    /// <remarks>
    /// To reduce allocations, this method uses a single internal reference to <typeparamref name="T"/> for deserialization.
    /// </remarks>
    /// <typeparam name="T">The type of the packet. Must implement <see cref="INetSerializable"/> and have a <see langword="new"/>() constraint.</typeparam>
    /// <typeparam name="TUserData">The type of the user data (typically <see cref="NetPeer"/>).</typeparam>
    /// <param name="onReceive">The delegate to be executed when the packet is received.</param>
    public void SubscribeNetSerializable<T, TUserData>(
        Action<T, TUserData> onReceive) where T : INetSerializable, new()
    {
        var reference = new T();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            reference.Deserialize(reader);
            onReceive(reference, (TUserData)userData);
        };
    }

    /// <summary>
    /// Registers a callback for a packet type that implements <see cref="INetSerializable"/> and has a parameterless constructor.
    /// </summary>
    /// <remarks>
    /// To reduce allocations, this method uses a single internal reference to <typeparamref name="T"/> for deserialization.
    /// </remarks>
    /// <typeparam name="T">The type of the packet. Must implement <see cref="INetSerializable"/> and have a <see langword="new"/>() constraint.</typeparam>
    /// <param name="onReceive">The delegate to be executed when the packet is received.</param>
    public void SubscribeNetSerializable<T>(
        Action<T> onReceive) where T : INetSerializable, new()
    {
        var reference = new T();
        _callbacks[GetShortHash<T>()] = (reader, userData) =>
        {
            reference.Deserialize(reader);
            onReceive(reference);
        };
    }

    /// <summary>
    /// Remove any subscriptions by type
    /// </summary>
    /// <typeparam name="T">Packet type</typeparam>
    /// <returns>true if remove is success</returns>
    public bool RemoveSubscription<T>()
    {
        return _callbacks.Remove(GetShortHash<T>());
    }
}
