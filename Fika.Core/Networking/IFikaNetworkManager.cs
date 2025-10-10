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

/// <summary>
/// Defines the contract for the Fika network manager, responsible for handling network communication,
/// player synchronization, VOIP, and packet registration.
/// </summary>
public interface IFikaNetworkManager
{
    /// <summary>
    /// Gets or sets the network identifier for this network manager instance.
    /// </summary>
    int NetId { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="CoopHandler"/> responsible for managing cooperative gameplay logic.
    /// </summary>
    CoopHandler CoopHandler { get; set; }

    /// <summary>
    /// Gets or sets the raid side type for the current session.
    /// </summary>
    ESideType RaidSide { get; set; }

    /// <summary>
    /// Gets the rate at which network packets are sent.
    /// </summary>
    int SendRate { get; }

    /// <summary>
    /// Gets or sets a value indicating whether VOIP is allowed.
    /// </summary>
    bool AllowVOIP { get; set; }

    /// <summary>
    /// Gets or sets the list of observed cooperative players in the session.
    /// </summary>
    List<ObservedPlayer> ObservedCoopPlayers { get; set; }

    /// <summary>
    /// Gets or sets the total number of players in the session.
    /// </summary>
    int PlayerAmount { get; set; }

    /// <summary>
    /// Creates the Fika chat system for in-game communication.
    /// </summary>
    void CreateFikaChat();

    /// <summary>
    /// Sets up game variables for the specified <see cref="FikaPlayer"/>.
    /// </summary>
    /// <param name="fikaPlayer">The player for whom to set up game variables.</param>
    void SetupGameVariables(FikaPlayer fikaPlayer);

    /// <summary>
    /// Sends a packet.
    /// </summary>
    /// <typeparam name="T">The type of packet to send, which must implement <see cref="INetSerializable"/>.</typeparam>
    /// <param name="packet">The packet instance to send, passed by reference.</param>
    /// <param name="deliveryMethod">The delivery method (reliable, unreliable, etc.) to use for sending the packet.</param>
    /// <param name="broadcast">If <see langword="true"/>, the packet will be sent to multiple recipients; otherwise, it will be sent to a single target (server is always broadcast).</param>
    void SendData<T>(ref T packet, DeliveryMethod deliveryMethod, bool broadcast = false) where T : INetSerializable;

    /// <summary>
    /// Sends a generic network packet to one or more peers.
    /// </summary>
    /// <param name="type">The generic sub-packet type identifier used to determine how the packet will be processed.</param>
    /// <param name="subpacket">The sub-packet payload to send. Must implement <see cref="IPoolSubPacket"/>.</param>
    /// <param name="broadcast">If <see langword="true"/>, the packet will be sent to multiple recipients; otherwise, it will be sent to a single target (server is always broadcast).</param>
    /// <param name="peerToIgnore">An optional peer to exclude from receiving the packet, typically the sender.</param>
    void SendGenericPacket(EGenericSubPacketType type, IPoolSubPacket subpacket, bool broadcast = false, NetPeer peerToIgnore = null);

    /// <summary>
    /// Sends a packet implementing <see cref="INetReusable"/> with manual serialization control.
    /// </summary>
    /// <typeparam name="T">The packet type, which must implement <see cref="INetReusable"/>.</typeparam>
    /// <param name="packet">A reference to the packet instance to send.</param>
    /// <param name="deliveryMethod">The delivery method (reliable, unreliable, etc.) to use for sending the packet.</param>
    /// <param name="broadcast">If <see langword="true"/>, the packet will be sent to multiple recipients; otherwise, it will be sent to a single target (server is always broadcast).</param>
    /// <param name="peerToIgnore">An optional peer to exclude from receiving the packet, typically the sender.</param>
    void SendNetReusable<T>(ref T packet, DeliveryMethod deliveryMethod, bool broadcast = false, NetPeer peerToIgnore = null) where T : INetReusable;

    /// <summary>
    /// Sends a packet of data directly to a specific peer.
    /// </summary>
    /// <typeparam name="T">The type of packet to send, which must implement <see cref="INetSerializable"/>.</typeparam>
    /// <param name="packet">The packet instance to send, passed by reference.</param>
    /// <param name="deliveryMethod">The delivery method (reliable, unreliable, etc.) to use for sending the packet.</param>
    /// <param name="peer">The target <see cref="NetPeer"/> that will receive the packet.</param>
    /// <remarks>
    /// Should only be used as a <see cref="FikaServer"/>, since a <see cref="FikaClient"/> only has one <see cref="NetPeer"/>.
    /// </remarks>
    void SendDataToPeer<T>(ref T packet, DeliveryMethod deliveryMethod, NetPeer peer) where T : INetSerializable;

    /// <summary>
    /// Sends a player state using fast serialization.
    /// </summary>
    /// <param name="packet">The player state packet to send.</param>
    void SendPlayerState(ref PlayerStatePacket packet);

    /// <summary>
    /// Sends raw VOIP audio data to a specific peer or multiple recipients.
    /// </summary>
    /// <param name="data">The audio data to send, provided as an <see cref="ArraySegment{T}"/> of bytes.</param>
    /// <param name="deliveryMethod">The delivery method (reliable, unreliable, etc.) to use for sending the packet.</param>
    /// <param name="peer">The target peer to receive the audio data (only used by server).</param>
    void SendVOIPData(ArraySegment<byte> data, DeliveryMethod deliveryMethod, NetPeer peer = null);

    /// <summary>
    /// Registers a packet to the <see cref="NetPacketProcessor"/>.
    /// </summary>
    /// <typeparam name="T">The packet type.</typeparam>
    /// <param name="handle">The <see cref="Action"/> to run when receiving the packet.</param>
    void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new();

    /// <summary>
    /// Registers a packet to the <see cref="NetPacketProcessor"/> with user data.
    /// </summary>
    /// <typeparam name="T">The packet type.</typeparam>
    /// <typeparam name="TUserData">The user data type.</typeparam>
    /// <param name="handle">The <see cref="Action"/> to run when receiving the packet.</param>
    void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new();

    /// <summary>
    /// Registers a reusable packet to the <see cref="NetPacketProcessor"/>. Reusable uses the same instance throughout the lifetime of the <see cref="NetManager"/>.
    /// Custom types must be registered with <see cref="RegisterCustomType{T}(Action{NetDataWriter, T}, Func{NetDataReader, T})"/> first.
    /// </summary>
    /// <typeparam name="T">The packet type.</typeparam>
    /// <param name="handle">The <see cref="Action"/> to run when receiving the packet.</param>
    void RegisterReusable<T>(Action<T> handle) where T : class, IReusable, new();

    /// <summary>
    /// Registers a reusable packet to the <see cref="NetPacketProcessor"/> with user data. Reusable uses the same instance throughout the lifetime of the <see cref="NetManager"/>.
    /// Custom types must be registered with <see cref="RegisterCustomType{T}(Action{NetDataWriter, T}, Func{NetDataReader, T})"/> first.
    /// </summary>
    /// <typeparam name="T">The packet type.</typeparam>
    /// <typeparam name="TUserData">The user data type.</typeparam>
    /// <param name="handle">The <see cref="Action"/> to run when receiving the packet.</param>
    void RegisterReusable<T, TUserData>(Action<T, TUserData> handle) where T : class, IReusable, new();

    /// <summary>
    /// Registers a reusable network packet to the <see cref="NetPacketProcessor"/>.
    /// </summary>
    /// <typeparam name="T">The packet type.</typeparam>
    /// <param name="handle">The <see cref="Action"/> to run when receiving the packet.</param>
    void RegisterNetReusable<T>(Action<T> handle) where T : class, INetReusable, new();

    /// <summary>
    /// Registers a reusable network packet to the <see cref="NetPacketProcessor"/> with user data.
    /// </summary>
    /// <typeparam name="T">The packet type.</typeparam>
    /// <typeparam name="TUserData">The user data type.</typeparam>
    /// <param name="handle">The <see cref="Action"/> to run when receiving the packet.</param>
    void RegisterNetReusable<T, TUserData>(Action<T, TUserData> handle) where T : class, INetReusable, new();

    /// <summary>
    /// Registers a custom type to the <see cref="NetPacketProcessor"/> so that it can handle serializing the type.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    /// <param name="writeDelegate">The serialize method.</param>
    /// <param name="readDelegate">The deserialize method.</param>
    void RegisterCustomType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate);

    /// <summary>
    /// Initializes the VOIP system asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task InitializeVOIP();

    /// <summary>
    /// Prints network statistics to the output (internal use only).
    /// </summary>
    internal void PrintStatistics();

    /// <summary>
    /// Represents the send rate options for the <see cref="IFikaNetworkManager"/>.
    /// </summary>
    public enum ESendRate : byte
    {
        /// <summary>
        /// Low send rate (10/s).
        /// </summary>
        Low,
        /// <summary>
        /// Medium send rate (20/s).
        /// </summary>
        Medium,
        /// <summary>
        /// High send rate (30/s).
        /// </summary>
        High
    }
}
