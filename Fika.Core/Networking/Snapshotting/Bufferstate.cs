namespace Fika.Core.Networking.Snapshotting;

public enum EBufferState : byte
{
    /// <summary>Valid 'from' and 'to' boundaries found.</summary>
    Interpolating,
    /// <summary>Render time exceeds the newest packet. Projection required.</summary>
    Extrapolating,
    /// <summary>Buffer is empty or data is too old to project.</summary>
    Stale
}