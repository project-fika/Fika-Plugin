namespace Fika.Core.Networking.Packets;

/// <summary>
/// Represents packet types used for requesting environmental or server state data.
/// </summary>
public enum ERequestSubPacketType : byte
{
    /// <summary>
    /// Spawn point request.
    /// </summary>
    SpawnPoint,

    /// <summary>
    /// Weather update request.
    /// </summary>
    Weather,

    /// <summary>
    /// Exfiltration request.
    /// </summary>
    Exfiltration,

    /// <summary>
    /// Request trader services information.
    /// </summary>
    TraderServices,

    /// <summary>
    /// Request character synchronization.
    /// </summary>
    CharacterSync
}
