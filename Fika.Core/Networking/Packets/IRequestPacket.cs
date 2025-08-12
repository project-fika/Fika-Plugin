using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets;

/// <summary>
/// Represents a request packet with handling for requests, responses, and serialization.
/// </summary>
public interface IRequestPacket
{
    /// <summary>
    /// Handles an incoming request from a network peer within the server context.
    /// </summary>
    /// <param name="peer">
    /// The network peer that sent the request.
    /// </param>
    /// <param name="server">
    /// The server instance processing the request.
    /// </param>
    public void HandleRequest(NetPeer peer, FikaServer server);

    /// <summary>
    /// Handles the response to a request.
    /// </summary>
    public void HandleResponse();

    /// <summary>
    /// Serializes the request packet data into the provided <see cref="NetDataWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The writer used to serialize data.
    /// </param>
    public void Serialize(NetDataWriter writer);
}
