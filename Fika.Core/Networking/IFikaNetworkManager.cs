using EFT;
using Fika.Core.Coop.Components;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading.Tasks;

namespace Fika.Core.Networking
{
    public interface IFikaNetworkManager
    {
        public int NetId { get; set; }
        public CoopHandler CoopHandler { get; set; }
        public EPlayerSide RaidSide { get; set; }
        public int SendRate { get; }
        public bool MultiThreaded { get; }
        public bool AllowVOIP { get; set; }
        public void SendVOIPPacket(ref VOIPPacket packet, NetPeer peer = null);
        public void SendVOIPData(ArraySegment<byte> data, NetPeer peer = null);
        public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new();
        public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new();
        public void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate);
        internal Task InitializeVOIP();
        internal void PrintStatistics();
    }
}
