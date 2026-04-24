using System;

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
    /// <summary> The minimum interpolation delay (50ms). Even on a perfect connection, we need a small buffer to interpolate between two frames. </summary>
    private const double _baseDelay = 0.05d;

    /// <summary> The maximum allowed interpolation delay (250ms). Caps latency to prevent the player from falling too far behind reality. </summary>
    private const double _maxDelay = 0.25d;

    /// <summary> The smoothed variance in packet arrival times. </summary>
    private double _currentJitter;

    /// <summary>
    /// Gets the calculated delay to be used in the Snapshotter.
    /// </summary>
    /// <remarks>
    /// (<see cref="_baseDelay"/> + <see cref="_currentJitter"/>), clamped between <see cref="_baseDelay"/> and <see cref="_maxDelay"/>.
    /// </remarks>
    public readonly double CurrentDelay
    {
        get
        {
            return Math.Clamp(_baseDelay + _currentJitter, _baseDelay, _maxDelay);
        }
    }

    /// <summary>
    /// Updates the jitter estimation based on the delta between packet arrivals.
    /// </summary>
    /// <param name="packetArrivalDeltaTime">The actual local time elapsed since the last packet arrived.</param>
    /// <param name="expectedTickDelta">The intended time elapsed between server snapshots (e.g., 0.05 for 20Hz).</param>
    public void Update(double packetArrivalDeltaTime, double expectedTickDelta)
    {
        // calculate the absolute deviation from the expected arrival interval
        // high variance indicates an unstable network path (jitter)
        var variance = Math.Abs(packetArrivalDeltaTime - expectedTickDelta);

        // asymmetric exponential moving average
        // if variance is increasing (spike), use a high alpha (0.2) to expand the buffer quickly
        // if variance is decreasing (recovery), use a low alpha (0.01) to shrink the buffer slowly
        var alpha = variance > _currentJitter ? 0.2d : 0.01d;

        _currentJitter = (alpha * variance) + ((1d - alpha) * _currentJitter);
    }
}