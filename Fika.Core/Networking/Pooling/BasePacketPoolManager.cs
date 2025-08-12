using System;
using System.Collections.Generic;

namespace Fika.Core.Networking.Pooling;

/// <summary>
/// Manages a pool of packet objects of type <typeparamref name="TType"/> categorized by keys of type <typeparamref name="TEnum"/>.
/// Provides functionality to create, clear, retrieve, and return packets from the pool.
/// </summary>
/// <typeparam name="TEnum">The enum type used as keys to identify packet types.</typeparam>
/// <typeparam name="TType">The packet type that implements <see cref="IDisposable"/>.</typeparam>
public abstract class BasePacketPoolManager<TEnum, TType>
    where TEnum : Enum
    where TType : class, IDisposable
{
    /// <summary>
    /// A dictionary mapping each packet type to a factory function that creates new packet instances.
    /// </summary>
    protected Dictionary<TEnum, Func<TType>> _subPacketFactories;

    private bool _poolExists;

    private Dictionary<TEnum, PacketPool<TType>> _pool;

    /// <summary>
    /// Creates a pool of packets for each packet type based on the registered factory functions.
    /// Initializes pools with an initial capacity of 2.
    /// </summary>
    public void CreatePool()
    {
        if (_pool == null)
        {
            _pool = [];
            foreach ((TEnum key, Func<TType> value) in _subPacketFactories)
            {
                _pool[key] = new PacketPool<TType>(4, value);
            }
        }

        _poolExists = true;
    }

    /// <summary>
    /// Clears and disposes all packet pools, releasing any resources held.
    /// </summary>
    public void ClearPool()
    {
        if (_pool != null)
        {
            foreach ((TEnum _, PacketPool<TType> value) in _pool)
            {
                value.Dispose();
            }
        }

        _pool = null;
        _poolExists = false;
    }

    /// <summary>
    /// Retrieves a packet of the specified type from the pool.
    /// If the pool is not created yet, it will be initialized.
    /// </summary>
    /// <typeparam name="T">The concrete packet type to retrieve, which must derive from <typeparamref name="TType"/>.</typeparam>
    /// <param name="type">The key identifying the packet type in the pool.</param>
    /// <returns>A packet instance from the pool.</returns>
    /// <exception cref="ArgumentException">Thrown if the specified type does not exist in the pool.</exception>
    public T GetPacket<T>(TEnum type) where T : TType
    {
        if (!_poolExists)
        {
            CreatePool();
        }

        return (T)WithdrawPacket(type).Get();
    }

    /// <summary>
    /// Returns a packet to the pool and disposes it.
    /// </summary>
    /// <param name="type">The key identifying the packet type in the pool.</param>
    /// <param name="packet">The packet instance to return to the pool.</param>
    /// <remarks>Does nothing if the pool has not been created</remarks>
    public void ReturnPacket(TEnum type, TType packet)
    {
        if (!_poolExists)
        {
            return;
        }

        packet.Dispose();
        WithdrawPacket(type).Return(packet);
    }

    /// <summary>
    /// Retrieves the <see cref="PacketPool{TType}"/> associated with the specified type.
    /// </summary>
    /// <param name="type">The key identifying the packet type.</param>
    /// <returns>The corresponding <see cref="PacketPool{TType}"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the specified type does not exist in the pool.</exception>
    private PacketPool<TType> WithdrawPacket(TEnum type)
    {
#if DEBUG
        if (!_pool.TryGetValue(type, out PacketPool<TType> packet))
        {
            throw new ArgumentException("Could not find given type in the packet pool manager!", nameof(type));
        }

        return packet;
#else
        return _pool[type];
#endif

    }
}