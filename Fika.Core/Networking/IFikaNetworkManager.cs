using EFT;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Communication;
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
        public List<ObservedPlayer> ObservedCoopPlayers { get; set; }
        public int PlayerAmount { get; set; }
        public void CreateFikaChat();
        public void SetupGameVariables(FikaPlayer fikaPlayer);
        /// <summary>
        /// Sends a packet
        /// </summary>
        /// <typeparam name="T">The type of packet to send, which must implement <see cref="INetSerializable"/></typeparam>
        /// <param name="data">The packet instance to send, passed by reference</param>
        /// <param name="multicast">If <see langword="true"/>, the packet will be sent to multiple recipients; otherwise, it will be sent to a single target (server is always multicast)</param>
        public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, bool multicast = false) where T : INetSerializable;
        public void SendNetReusable<T>(ref T packet, DeliveryMethod deliveryMethod, bool multicast = false) where T : INetReusable;
        /// <summary>
        /// Sends a packet of data directly to a specific peer
        /// </summary>
        /// <typeparam name="T">The type of packet to send, which must implement <see cref="INetSerializable"/></typeparam>
        /// <param name="data">The packet instance to send, passed by reference</param>
        /// <param name="peer">The target <see cref="NetPeer"/> that will receive the packet</param>
        /// <remarks>
        /// Should only be used as a <see cref="FikaServer"/>, since a <see cref="FikaClient"/> only has one <see cref="NetPeer"/>
        /// </remarks>
        public void SendDataToPeer<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peer) where T : INetSerializable;
        public void SendVOIPData(ArraySegment<byte> data, DeliveryMethod deliveryMethod, NetPeer peer = null);
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
        public void RegisterNetReusable<T>(Action<T> handle) where T : class, INetReusable, new();
        public void RegisterNetReusable<T, TUserData>(Action<T, TUserData> handle) where T : class, INetReusable, new();
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
