using System;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Snapshotting;

/// <summary>
/// Dynamically calculates the required interpolation delay based on network jitter. <br/>
/// Aims to minimize latency during stable connections and expand the buffer during spikes to prevent stutter.
/// </summary>
/// <remarks>
/// This follows an asymmetric EMA approach: it expands rapidly to absorb lag spikes but contracts slowly 
/// to maintain visual stability during recovery.
/// </remarks>
public struct AdaptiveJitterBuffer
{
    /// <summary> Default minimum interpolation delay (50ms). </summary>
    public const double DefaultBaseDelay = 0.05d;

    /// <summary> Default maximum allowed interpolation delay (250ms). </summary>
    public const double DefaultMaxDelay = 0.25d;

    /// <summary> The minimum interpolation delay. </summary>
    private readonly double _baseDelay;

    /// <summary> The maximum allowed interpolation delay. </summary>
    private readonly double _maxDelay;

    /// <summary> The smoothed variance in packet arrival times. </summary>
    private double _currentJitter;

    /// <summary>
    /// Initializes a new instance of <see cref="AdaptiveJitterBuffer"/> with custom delay bounds.
    /// </summary>
    /// <param name="baseDelay">The minimum interpolation delay in seconds (must be > 0). Defaults to 0.05s.</param>
    /// <param name="maxDelay">The maximum allowed interpolation delay in seconds (must be >= baseDelay). Defaults to 0.25s.</param>
    public AdaptiveJitterBuffer(double baseDelay = DefaultBaseDelay, double maxDelay = DefaultMaxDelay)
    {
        _baseDelay = baseDelay > 0d ? baseDelay : DefaultBaseDelay;
        _maxDelay = maxDelay >= _baseDelay ? maxDelay : Math.Max(_baseDelay, DefaultMaxDelay);
        _currentJitter = 0d;
    }

    /// <summary>
    /// Gets the configured minimum interpolation delay.
    /// </summary>
    public readonly double BaseDelay => _baseDelay > 0d ? _baseDelay : DefaultBaseDelay;

    /// <summary>
    /// Gets the configured maximum interpolation delay.
    /// </summary>
    public readonly double MaxDelay => _maxDelay > 0d ? _maxDelay : DefaultMaxDelay;

    /// <summary>
    /// Gets the current smoothed jitter variance in seconds.
    /// </summary>
    public readonly double CurrentJitter => _currentJitter;

    /// <summary>
    /// Gets the calculated delay to be used in the Snapshotter.
    /// </summary>
    /// <remarks>
    /// (<see cref="BaseDelay"/> + <see cref="_currentJitter"/>), clamped between <see cref="BaseDelay"/> and <see cref="MaxDelay"/>.
    /// </remarks>
    public readonly double CurrentDelay
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var min = BaseDelay;
            var max = MaxDelay;
            var value = min + _currentJitter;

#if NETSTANDARD2_1_OR_GREATER
            return Math.Clamp(value, min, max);
#else
                if (value < min)
                {
                    return min;
                }

                if (value > max)
                {
                    return max;
                }

                return value;
#endif
        }
    }

    /// <summary>
    /// Updates the jitter estimation based on the delta between packet arrivals.
    /// </summary>
    /// <param name="packetArrivalDeltaTime">The actual local time elapsed since the last packet arrived.</param>
    /// <param name="expectedTickDelta">The intended time elapsed between server snapshots (e.g., 0.05 for 20Hz).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(double packetArrivalDeltaTime, double expectedTickDelta)
    {
        // calculate the absolute deviation from the expected arrival interval
        // high variance indicates an unstable network path (jitter)
        var variance = Math.Abs(packetArrivalDeltaTime - expectedTickDelta);

        // asymmetric exponential moving average
        // if variance is increasing (spike), use a high alpha (0.2) to expand the buffer quickly
        // if variance is decreasing (recovery), use a low alpha (0.01) to shrink the buffer slowly
        var alpha = variance > _currentJitter ? 0.2d : 0.01d;

        _currentJitter += alpha * (variance - _currentJitter);
    }

    /// <summary>
    /// Resets the accumulated jitter to zero while preserving delay bounds.
    /// </summary>
    public void Reset()
    {
        _currentJitter = 0d;
    }
}