using System;
using System.Collections.Generic;

namespace Fika.Core.Networking.Pools
{
    public abstract class BasePacketPoolManager<TEnum, TType>
        where TEnum : Enum
        where TType : class, IDisposable
    {
        public void CreatePool()
        {
            if (_pool == null)
            {
                _pool = [];
                foreach ((TEnum key, Func<TType> value) in _subPacketFactories)
                {
                    _pool[key] = new(4, value);
                }
            }

            _poolExists = true;
        }

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

        public T GetPacket<T>(TEnum type) where T : TType
        {
            if (!_poolExists)
            {
                CreatePool();
            }

            return (T)WithdrawPacket(type).Get();
        }

        public void ReturnPacket(TEnum type, TType packet)
        {
            if (!_poolExists)
            {
                return;
            }

            packet.Dispose();
            WithdrawPacket(type).Return(packet);
        }

        private PacketPool<TType> WithdrawPacket(TEnum type)
        {
            if (!_pool.TryGetValue(type, out PacketPool<TType> packet))
            {
                throw new ArgumentException("Could not find given type in the packet pool manager!", nameof(type));
            }

            return packet;
        }

        private bool _poolExists;
        private Dictionary<TEnum, PacketPool<TType>> _pool;

        protected Dictionary<TEnum, Func<TType>> _subPacketFactories;
    }
}