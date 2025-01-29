using EFT;
using Fika.Core.Coop.Components;
using LiteNetLib.Utils;
using System;

namespace Fika.Core.Networking
{
    public interface IFikaNetworkManager
    {
        public CoopHandler CoopHandler { get; set; }
        public EPlayerSide RaidSide { get; set; }
        public int SendRate { get; }
        public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new();
        public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new();
        public void RegisterReusablePacket<T>(Action<T> handle) where T : class, IReusable, new();
        public void RegisterReusablePacket<T, TUserData>(Action<T, TUserData> handle) where T : class, IReusable, new();
        public void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate);
        internal void PrintStatistics();
    }
}
