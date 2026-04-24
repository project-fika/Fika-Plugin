namespace Fika.Core.Networking.Snapshotting;

/// <summary>
/// Defines the minimum data required for a synchronized network snapshot.
/// </summary>
public interface ISnapshot
{
    /// <summary> The server's timestamp when this state was captured. </summary>
    double RemoteTime { get; }
    /// <summary> The local system time when this packet was received. </summary>
    double LocalTime { get; }
}