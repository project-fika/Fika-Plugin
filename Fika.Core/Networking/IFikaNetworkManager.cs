using EFT;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Networking
{
    public interface IFikaNetworkManager
    {
        public int NetId { get; set; }
        public CoopHandler CoopHandler { get; set; }
        public ESideType RaidSide { get; set; }
        public int SendRate { get; }
        public bool AllowVOIP { get; set; }
        public List<ObservedCoopPlayer> ObservedCoopPlayers { get; set; }
        public void CreateFikaChat();
        public void SetupGameVariables(CoopPlayer coopPlayer);
        public void SendVOIPPacket(ref VOIPPacket packet, NetPeer peer = null);
        public void SendVOIPData(ArraySegment<byte> data, NetPeer peer = null);
        /// <summary>
        /// Registers a packet to the <see cref="NetPacketProcessor"/>
        /// </summary>
        /// <typeparam name="T">The packet</typeparam>
        /// <param name="handle">The <see cref="Action"/> to run when receiving the packet</param>
        public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new();
        /// <summary>
        /// Registers a packet to the <see cref="NetPacketProcessor"/>
        /// </summary>
        /// <typeparam name="T">The packet</typeparam>
        /// <param name="handle">The <see cref="Action"/> to run when receiving the packet</param>
        public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new();
        /// <summary>
        /// Registers a reusable packet to the <see cref="NetPacketProcessor"/>, reusable uses the same instance throughout the lifetime of the <see cref="NetManager"/> <br/>
        /// Custom types must be registered with <see cref="RegisterCustomType{T}(Action{NetDataWriter, T}, Func{NetDataReader, T})"/> first
        /// </summary>
        /// <typeparam name="T">The packet</typeparam>
        /// <param name="handle">The <see cref="Action"/> to run when receiving the packet</param>
        public void RegisterReusable<T>(Action<T> handle) where T : class, IReusable, new();
        /// <summary>
        /// Registers a reusable packet to the <see cref="NetPacketProcessor"/>, reusable uses the same instance throughout the lifetime of the <see cref="NetManager"/> <br/>
        /// Custom types must be registered with <see cref="RegisterCustomType{T}(Action{NetDataWriter, T}, Func{NetDataReader, T})"/> first
        /// </summary>
        /// <typeparam name="T">The packet</typeparam>
        /// <param name="handle">The <see cref="Action"/> to run when receiving the packet</param>
        public void RegisterReusable<T, TUserData>(Action<T, TUserData> handle) where T : class, IReusable, new();
        /// <summary>
        /// Registers a custom type to the <see cref="NetPacketProcessor"/> so that it can handle serializing the type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writeDelegate">The serialize method</param>
        /// <param name="readDelegate">The deserialize method</param>
        public void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate);
        internal Task InitializeVOIP();
        internal void PrintStatistics();
    }
}
