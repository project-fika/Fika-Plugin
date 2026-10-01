using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Snapshotting;

/// <summary>
/// Manages the synchronization between server and local clocks using an Exponential Moving Average (EMA). <br/>
/// This smooths out network jitter and clock drift to provide a stable time offset for interpolation.
/// </summary>
public struct TimeSyncEMA
{
    /// <summary>
    /// Default smoothing factor for the EMA calculation.
    /// </summary>
    private const double _defaultAlpha = 0.1d;

    /// <summary>
    /// The smoothing factor for the EMA calculation.
    /// </summary>
    private readonly double _alpha;

    /// <summary> The current smoothed difference between Server Time and Local Time. </summary>
    private double _emaOffset;

    /// <summary> Tracks whether the first sample has been taken to initialize the baseline offset. </summary>
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of <see cref="TimeSyncEMA"/> with a custom smoothing factor.
    /// </summary>
    /// <param name="alpha">The smoothing factor (0.0 to 1.0). Defaults to 0.1.</param>
    public TimeSyncEMA(double alpha = _defaultAlpha)
    {
        _alpha = alpha > 0d && alpha <= 1d ? alpha : _defaultAlpha;
        _emaOffset = 0d;
        _initialized = false;
    }

    /// <summary>
    /// Gets the smoothing factor being used.
    /// </summary>
    private readonly double Alpha => _alpha > 0d ? _alpha : _defaultAlpha;

    /// <summary>
    /// Gets whether the baseline offset has been initialized with at least one sample.
    /// </summary>
    public readonly bool IsInitialized => _initialized;

    /// <summary>
    /// Gets the current smoothed offset.
    /// </summary>
    /// <remarks>
    /// Add this to client local time (e.g. <c>Time.unscaledTimeAsDouble</c>) to estimate current Server Time.
    /// </remarks>
    public readonly double SmoothOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _emaOffset;
    }

    /// <summary>
    /// Updates the moving average with a new time sample from a network packet.
    /// </summary>
    /// <param name="serverTime">The remote timestamp provided by the server/sender.</param>
    /// <param name="localTime">The local system time when the packet was received.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // exponential moving average formula: Sn = Sn-1 + α(Y - Sn-1)
        // mathematically identical to αY + (1-α)Sn-1, but avoids steady-state drift and saves an instruction
        var alpha = Alpha;
        _emaOffset += alpha * (currentOffset - _emaOffset);
    }

    /// <summary>
    /// Resets the EMA state to uninitialized while preserving configuration.
    /// </summary>
    public void Reset()
    {
        _emaOffset = 0d;
        _initialized = false;
    }
}