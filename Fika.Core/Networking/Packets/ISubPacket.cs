using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets;

/// <summary>
/// Represents a sub-packet with execution and serialization capabilities.
/// </summary>
public interface ISubPacket
{
    /// <summary>
    /// Executes the sub-packet logic, optionally with a <see cref="FikaPlayer"/> context.
    /// </summary>
    /// <param name="player">
    /// The player involved in the execution. This parameter is optional and can be null.
    /// </param>
    public void Execute(FikaPlayer player = null);

    /// <summary>
    /// Serializes the sub-packet data into the provided <see cref="NetDataWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The writer used to serialize data.
    /// </param>
    public void Serialize(NetDataWriter writer);
}
