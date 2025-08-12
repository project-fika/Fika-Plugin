namespace Fika.Core.Networking.Packets;

/// <summary>
/// Represents generic game-level packet events unrelated to specific systems.
/// </summary>
public enum EGenericSubPacketType : byte
{
    /// <summary>
    /// Client extraction event.
    /// </summary>
    ClientExtract,

    /// <summary>
    /// Client connected to the server.
    /// </summary>
    ClientConnected,

    /// <summary>
    /// Client disconnected from the server.
    /// </summary>
    ClientDisconnected,

    /// <summary>
    /// Exfiltration countdown event.
    /// </summary>
    ExfilCountdown,

    /// <summary>
    /// Clear all active effects.
    /// </summary>
    ClearEffects,

    /// <summary>
    /// Update backend data.
    /// </summary>
    UpdateBackendData,

    /// <summary>
    /// Secret exfiltration found event.
    /// </summary>
    SecretExfilFound,

    /// <summary>
    /// Border zone event.
    /// </summary>
    BorderZone,

    /// <summary>
    /// Mine triggered.
    /// </summary>
    Mine,

    /// <summary>
    /// Disarm a tripwire event.
    /// </summary>
    DisarmTripwire,

    /// <summary>
    /// Player muffled state changed.
    /// </summary>
    MuffledState,

    /// <summary>
    /// Spawns the BTR vehicle.
    /// </summary>
    SpawnBTR,

    /// <summary>
    /// Sync characters with server
    /// </summary>
    CharacterSync,

    /// <summary>
    /// Inventory operation
    /// </summary>
    InventoryOperation,

    /// <summary>
    /// Callback status for operation
    /// </summary>
    OperationCallback

    /// <summary>
    /// Train synchronization event (commented out).
    /// </summary>
    // TrainSync,

    /// <summary>
    /// Trader service notification event (commented out).
    /// </summary>
    // TraderServiceNotification,
}
