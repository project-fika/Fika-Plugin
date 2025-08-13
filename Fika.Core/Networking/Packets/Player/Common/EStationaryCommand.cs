namespace Fika.Core.Networking.Packets.Player.Common;

/// <summary>
/// Represents commands related to stationary equipment or positions.
/// </summary>
public enum EStationaryCommand : byte
{
    /// <summary>
    /// Occupy the stationary turret.
    /// </summary>
    Occupy,

    /// <summary>
    /// Leave the stationary turret.
    /// </summary>
    Leave,

    /// <summary>
    /// Access to the turret was denied.
    /// </summary>
    Denied
}
