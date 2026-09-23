using System;
using System.Runtime.CompilerServices;

namespace Fika.Core.Networking.Snapshotting;

/// <summary>
/// A high-performance, zero-allocation circular buffer designed for <see cref="ISnapshot"/> (<typeparamref name="T"/>) interpolation. <br/>
/// Utilizes bitwise masking for O(1) insertions and binary search for O(log N) sampling.
/// </summary>
/// <remarks>
/// Heavily inspired by the id Tech 3 networking model (used in Quake III Arena) <br/>
/// Written by <b>Lacyway</b>
/// </remarks>
public sealed class PlayerSnapshotter<T> where T : struct, ISnapshot
{
    /// <summary> Default capacity for the ring buffer. Must be a power of two. </summary>
    private const int _defaultCapacity = 32;

    /// <summary> Default maximum duration (in seconds) to allow velocity-based extrapolation. </summary>
    private const double _defaultMaxExtrapolationTime = 0.1d;

    /// <summary> Capacity must be a power of two to allow bitwise wrapping via <see cref="_mask"/>. </summary>
    private readonly int _capacity;
    private readonly int _mask;

    /// <summary> Contiguous memory block of snapshots to maximize CPU L1/L2 cache hits. </summary>
    private readonly T[] _buffer;

    /// <summary> Clock synchronization manager. </summary>
    private TimeSyncEMA _timeSync;

    /// <summary> Manages the dynamic interpolation delay for this entity. </summary>
    private AdaptiveJitterBuffer _adaptiveJitterBuffer;

    /// <summary> Maximum duration allowed for extrapolation before declaring the state stale. </summary>
    private readonly double _maxExtrapolationTime;

    /// <summary> Total number of snapshots added over the lifetime of this object. Used to calculate ring indices. </summary>
    private int _totalAdded;

    /// <summary>
    /// Initializes a new instance of <see cref="Snapshotter{T}"/> with default capacity and delay settings.
    /// </summary>
    public PlayerSnapshotter() : this(_defaultCapacity, AdaptiveJitterBuffer.DefaultBaseDelay, AdaptiveJitterBuffer.DefaultMaxDelay, _defaultMaxExtrapolationTime)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Snapshotter{T}"/> with custom capacity, delay, and extrapolation bounds.
    /// </summary>
    /// <param name="capacity">Buffer size. Must be a power of two and at least 2. Defaults to 32.</param>
    /// <param name="baseDelay">Minimum interpolation delay in seconds. Defaults to 0.05s.</param>
    /// <param name="maxDelay">Maximum interpolation delay in seconds. Defaults to 0.25s.</param>
    /// <param name="maxExtrapolationTime">Maximum allowed extrapolation duration in seconds. Defaults to 0.1s.</param>
    public PlayerSnapshotter(int capacity = _defaultCapacity, double baseDelay = AdaptiveJitterBuffer.DefaultBaseDelay,
        double maxDelay = AdaptiveJitterBuffer.DefaultMaxDelay,
        double maxExtrapolationTime = _defaultMaxExtrapolationTime)
    {
        if (capacity < 2 || (capacity & (capacity - 1)) != 0)
        {
            throw new ArgumentException("Capacity must be a power of two and at least 2.", nameof(capacity));
        }

        if (maxExtrapolationTime < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExtrapolationTime), "Max extrapolation time cannot be negative.");
        }

        _capacity = capacity;
        _mask = capacity - 1;
        _buffer = new T[capacity];
        _timeSync = new TimeSyncEMA();
        _adaptiveJitterBuffer = new AdaptiveJitterBuffer(baseDelay, maxDelay);
        _maxExtrapolationTime = maxExtrapolationTime;
    }

    /// <summary> Gets the total capacity of the circular buffer. </summary>
    public int Capacity => _capacity;

    /// <summary> Gets the number of valid snapshots currently stored in the buffer. </summary>
    public int Count => Math.Min(_totalAdded, _capacity);

    /// <summary> Gets whether the buffer contains no snapshots. </summary>
    public bool IsEmpty => _totalAdded == 0;

    /// <summary> Gets the current smoothed clock offset (ServerTime - LocalTime). </summary>
    public double SmoothOffset => _timeSync.SmoothOffset;

    /// <summary> Gets the current calculated adaptive interpolation delay. </summary>
    public double CurrentDelay => _adaptiveJitterBuffer.CurrentDelay;

    /// <summary> Gets the current smoothed jitter variance. </summary>
    public double CurrentJitter => _adaptiveJitterBuffer.CurrentJitter;

    /// <summary> Gets the maximum allowed extrapolation time in seconds. </summary>
    public double MaxExtrapolationTime => _maxExtrapolationTime;

    /// <summary> Gets the index of the newest snapshot in the buffer, or -1 if empty. </summary>
    public int NewestIndex => _totalAdded > 0 ? ((_totalAdded - 1) & _mask) : -1;

    /// <summary> Gets the index of the oldest snapshot in the buffer, or -1 if empty. </summary>
    public int OldestIndex => _totalAdded > 0 ? (_totalAdded < _capacity ? 0 : (_totalAdded & _mask)) : -1;

    /// <summary>
    /// Inserts a new snapshot into the ring buffer and updates the clock synchronization offset.
    /// </summary>
    /// <param name="snapshot">The state data received from the network. Passed by <see langword="in"/> to avoid copying the big struct.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSnapshot(in T snapshot)
    {
        if (_totalAdded > 0)
        {
            var newestIdx = (_totalAdded - 1) & _mask;
            ref readonly var newestSnap = ref _buffer[newestIdx];

            // sequence validation: drop out-of-order or duplicate packets
            if (snapshot.RemoteTime <= newestSnap.RemoteTime)
            {
                return;
            }

            // calculate physical arrival delta vs remote tick delta
            var localDelta = snapshot.LocalTime - newestSnap.LocalTime;
            var remoteDelta = snapshot.RemoteTime - newestSnap.RemoteTime;

            // update the jitter variance
            _adaptiveJitterBuffer.Update(localDelta, remoteDelta);
        }

        // O(1) insertion: direct write to the masked index
        _buffer[_totalAdded & _mask] = snapshot;
        _totalAdded++;

        // update the EMA Offset (ServerTime - LocalTime) to synchronize the playback timeline
        _timeSync.Update(snapshot.RemoteTime, snapshot.LocalTime);
    }

    /// <summary>
    /// Identifies the two snapshots surrounding the calculated render time. <br/>
    /// Returns indices instead of struct copies to prioritize CPU speed.
    /// </summary>
    /// <param name="localTime">Current local system time (usually <see cref="Time.unscaledTimeAsDouble"/>).</param>
    /// <param name="fromIdx">The index of the snapshot immediately before the render time.</param>
    /// <param name="toIdx">The index of the snapshot immediately after the render time.</param>
    /// <param name="t">The 0-1 interpolation factor (lerp amount) between the two snapshots.</param>
    /// <returns>The <see cref="EBufferState"/> representing whether the buffer is currently interpolating, extrapolating, or stale.</returns>
    public EBufferState GetInterpolationIndices(double localTime, out int fromIdx, out int toIdx, out float t)
    {
        fromIdx = toIdx = -1;
        t = 0f;

        var count = Math.Min(_totalAdded, _capacity);
        if (count < 2)
        {
            return EBufferState.Stale;
        }

        // consume the dynamic delay from the AdaptiveJitterBuffer
        var renderTime = localTime + _timeSync.SmoothOffset - _adaptiveJitterBuffer.CurrentDelay;

        var newestIdx = (_totalAdded - 1) & _mask;
        ref readonly var newestSnap = ref _buffer[newestIdx];

        // check if we need to extrapolate before searching
        if (renderTime > newestSnap.RemoteTime)
        {
            var timeSinceNewest = renderTime - newestSnap.RemoteTime;

            // hard limit extrapolation. beyond this, predictions diverge too far from reality.
            if (timeSinceNewest > _maxExtrapolationTime)
            {
                return EBufferState.Stale;
            }

            fromIdx = newestIdx; // use this index's data + velocity * timeSinceNewest
            t = (float)timeSinceNewest;
            return EBufferState.Extrapolating;
        }

        var oldestIdx = _totalAdded < _capacity ? 0 : (_totalAdded & _mask);
        ref readonly var oldestSnap = ref _buffer[oldestIdx];

        // O(1) stale pre-check: if renderTime is older than our oldest buffered snapshot
        if (renderTime < oldestSnap.RemoteTime)
        {
            return EBufferState.Stale;
        }

        var low = 0;
        var high = count - 1;
        while (low < high)
        {
            var mid = (low + high) >> 1;
            if (_buffer[(oldestIdx + mid) & _mask].RemoteTime < renderTime)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        // boundary edge case: if low == 0, renderTime exactly equals oldestSnap.RemoteTime
        if (low == 0)
        {
            fromIdx = oldestIdx;
            toIdx = (oldestIdx + 1) & _mask;
            t = 0f;
            return EBufferState.Interpolating;
        }

        fromIdx = (oldestIdx + low - 1) & _mask;
        toIdx = (oldestIdx + low) & _mask;

        // access via ref readonly to avoid stack-copying the large ISnapshot structs
        ref readonly var snapFrom = ref _buffer[fromIdx];
        ref readonly var snapTo = ref _buffer[toIdx];

        var range = snapTo.RemoteTime - snapFrom.RemoteTime;
        if (range > 0d)
        {
            var factor = (float)((renderTime - snapFrom.RemoteTime) / range);
            t = factor < 0f ? 0f : (factor > 1f ? 1f : factor);
        }
        else
        {
            t = 0f;
        }

        return EBufferState.Interpolating;
    }

    /// <summary>
    /// Retrieves a reference to a snapshot in the buffer.
    /// </summary>
    /// <param name="index">The masked index retrieved from <see cref="GetInterpolationIndices"/>.</param>
    /// <returns>A <see langword="readonly"/> reference to the snapshot, preventing struct copies.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetSnapshot(int index)
    {
        return ref _buffer[index];
    }

    /// <summary>
    /// Clears the snapshotter and resets state to initial baseline while preserving configuration.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _totalAdded = 0;
        _timeSync.Reset();
        _adaptiveJitterBuffer.Reset();
    }
}