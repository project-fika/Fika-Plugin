using EFT;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Packets.Player;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fika.Core.Networking;

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
    /// <param name="packet">The packet instance to send, passed by reference</param>
    /// <param name="deliveryMethod">The delivery method (reliable, unreliable, etc.) to use for sending the packet.</param>
    /// <param name="multicast">If <see langword="true"/>, the packet will be sent to multiple recipients; otherwise, it will be sent to a single target (server is always multicast)</param>
    public void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, bool multicast = false) where T : INetSerializable;
    /// <summary>
    /// Sends a generic network packet to one or more peers.
    /// </summary>
    /// <param name="type">The generic sub-packet type identifier used to determine how the packet will be processed.</param>
    /// <param name="subpacket">The sub-packet payload to send. Must implement <see cref="IPoolSubPacket"/>.</param>
    /// <param name="multicast">If <see langword="true"/>, the packet will be sent to multiple recipients; otherwise, it will be sent to a single target (server is always multicast)</param>
    /// <param name="peerToIgnore">An optional peer to exclude from receiving the packet, typically the sender.</param>
    public void SendGenericPacket(EGenericSubPacketType type, IPoolSubPacket subpacket, bool multicast = false, NetPeer peerToIgnore = null);
    /// <summary>
    /// Sends a packet implementing <see cref="INetReusable"/> with manual serialization control.
    /// </summary>
    /// <typeparam name="T">The packet type, which must implement <see cref="INetReusable"/>.</typeparam>
    /// <param name="packet">A reference to the packet instance to send.</param>
    /// <param name="deliveryMethod">The delivery method (reliable, unreliable, etc.) to use for sending the packet.</param>
    /// <param name="multicast">If <see langword="true"/>, the packet will be sent to multiple recipients; otherwise, it will be sent to a single target (server is always multicast)</param>
    /// <param name="peerToIgnore">An optional peer to exclude from receiving the packet, typically the sender.</param>
    public void SendNetReusable<T>(ref T packet, DeliveryMethod deliveryMethod, bool multicast = false, NetPeer peerToIgnore = null) where T : INetReusable;
    /// <summary>
    /// Sends a packet of data directly to a specific peer
    /// </summary>
    /// <typeparam name="T">The type of packet to send, which must implement <see cref="INetSerializable"/></typeparam>
    /// <param name="peer">The target <see cref="NetPeer"/> that will receive the packet</param>
    /// <remarks>
    /// Should only be used as a <see cref="FikaServer"/>, since a <see cref="FikaClient"/> only has one <see cref="NetPeer"/>
    /// </remarks>
    public void SendDataToPeer<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peer) where T : INetSerializable;
    /// <summary>
    /// Sends a player state using fast serialization
    /// </summary>
    /// <param name="packet">The player state</param>
    public void SendPlayerState(ref PlayerStatePacket packet);
    /// <summary>
    /// Sends raw VOIP audio data to a specific peer or multiple recipients.
    /// </summary>
    /// <param name="data">The audio data to send, provided as an <see cref="ArraySegment{T}"/> of bytes.</param>
    /// <param name="deliveryMethod">The delivery method (reliable, unreliable, etc.) to use for sending the packet.</param>
    /// <param name="peer">The target peer to receive the audio data (only used by server)</param>
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
    /// <typeparam name="T">The type to register</typeparam>
    /// <param name="writeDelegate">The serialize method</param>
    /// <param name="readDelegate">The deserialize method</param>
    public void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate);
    public Task InitializeVOIP();
    internal void PrintStatistics();
    /// <summary>
    /// The SendRate of the <see cref="Networking.IFikaNetworkManager"/>
    /// </summary>
    public enum ESendRate : byte
    {
        Low,
        Medium,
        High
    }
}
