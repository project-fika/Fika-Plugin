namespace Fika.Core.Main.ObservedClasses.Snapshotting;

/// <summary>
/// Settings used for the <see cref="Snapshotter{T}"/>
/// </summary>
public class SnapshotInterpolationSettings
{
    #region Buffering
    /// <summary>
    /// Local simulation is behind by sendInterval * multiplier seconds. <br/>
    /// This guarantees that we always have enough snapshots in the buffer to mitigate lags and jitter. <br/>
    /// Increase this if the simulation isn't smooth. By default, it should be around 2.
    /// </summary>
    public double bufferTimeMultiplier = 2d;

    /// <summary>
    /// If a client can't process snapshots fast enough, don't store too many.
    /// </summary>
    public int bufferLimit = 32;
    #endregion

    #region Catchup / Slowdown
    /// <summary>
    /// Slowdown begins when the local timeline is moving too fast towards remote time. Threshold is in frames worth of snapshots. <br/>
    /// This needs to be negative. <br/>
    /// Don't modify unless you know what you are doing.
    /// </summary>
    public float catchupNegativeThreshold = -1f;

    /// <summary>
    /// Catchup begins when the local timeline is moving too slow and getting too far away from remote time. Threshold is in frames worth of snapshots. <br/>
    /// This needs to be positive. <br/>
    /// Don't modify unless you know what you are doing.
    /// </summary>
    public float catchupPositiveThreshold = 1f;

    /// <summary>
    /// Local timeline acceleration in % while catching up.
    /// </summary>
    [Range(0, 1)]
    public double catchupSpeed = 0.05d;

    /// <summary>
    /// Local timeline slowdown in % while slowing down.
    /// </summary>
    [Range(0, 1)]
    public double slowdownSpeed = 0.04d;

    /// <summary>
    /// Catchup/Slowdown is adjusted over n-second exponential moving average.
    /// </summary>
    public int driftEmaDuration = 3;
    #endregion

    #region Dynamic Adjustment
    /// <summary>
    /// Automatically adjust bufferTimeMultiplier for smooth results. <br/>
    /// Sets a low multiplier on stable connections, and a high multiplier on jittery connections.
    /// </summary>
    public bool dynamicAdjustment = true;

    /// <summary>
    /// Safety buffer that is always added to the dynamic bufferTimeMultiplier adjustment.
    /// </summary>
    public float dynamicAdjustmentTolerance = 1f;

    /// <summary>
    /// Dynamic adjustment is computed over n-second exponential moving average standard deviation.
    /// </summary>
    public int deliveryTimeEmaDuration = 4;
    #endregion
}
