namespace Fika.Core.Networking.Snapshotting;

/// <summary>
/// Manages the synchronization between server and local clocks using an Exponential Moving Average (EMA). <br/>
/// This smooths out network jitter and clock drift to provide a stable time offset for interpolation.
/// </summary>
public struct TimeSyncEMA
{
    /// <summary>
    /// The smoothing factor for the EMA calculation.
    /// </summary>
    /// <remarks>
    /// 0.1f is recommended for stability; higher values react faster to network changes but may introduce jitter.
    /// </remarks>
    private const float _alpha = 0.1f;

    /// <summary> The current smoothed difference between Server Time and Local Time. </summary>
    private double _emaOffset;

    /// <summary> Tracks whether the first sample has been taken to initialize the baseline offset. </summary>
    private bool _initialized;

    /// <summary>
    /// Gets the current smoothed offset.
    /// </summary>
    /// <remarks>
    /// Add this to <see cref="Time.unscaledTimeAsDouble"/> to estimate current Server Time.
    /// </remarks>
    public readonly double SmoothOffset
    {
        get
        {
            return _emaOffset;
        }
    }

    /// <summary>
    /// Updates the moving average with a new time sample from a network packet.
    /// </summary>
    /// <param name="serverTime">The remote timestamp provided by the server/sender.</param>
    /// <param name="localTime">The local system time when the packet was received.</param>
    public void Update(double serverTime, double localTime)
    {
        // calculate the raw delta between the two clocks
        var currentOffset = serverTime - localTime;

        // initialize the EMA with the first sample to avoid starting from zero
        if (!_initialized)
        {
            _emaOffset = currentOffset;
            _initialized = true;
            return;
        }

        // exponential moving average formula: Sn = αY + (1-α)Sn-1
        // this weights the new sample by alpha and the previous history by (1-alpha)
        _emaOffset = (_alpha * currentOffset) + ((1f - _alpha) * _emaOffset);
    }
}