namespace Fika.Core.Networking.Packets;

/// <summary>
/// Represents types of grenade packet actions.
/// </summary>
public enum EGrenadePacketType : byte
{
    /// <summary>
    /// No grenade action.
    /// </summary>
    None,

    /// <summary>
    /// Examine the weapon while holding a grenade.
    /// </summary>
    ExamineWeapon,

    /// <summary>
    /// Perform a high grenade throw.
    /// </summary>
    HighThrow,

    /// <summary>
    /// Perform a low grenade throw.
    /// </summary>
    LowThrow,

    /// <summary>
    /// Pull the ring in preparation for a high throw.
    /// </summary>
    PullRingForHighThrow,

    /// <summary>
    /// Pull the ring in preparation for a low throw.
    /// </summary>
    PullRingForLowThrow
}
