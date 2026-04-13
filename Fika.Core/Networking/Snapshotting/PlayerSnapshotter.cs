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
    /// <summary> Capacity must be a power of two to allow bitwise wrapping via <see cref="_mask"/>. </summary>
    private const int _capacity = 16;
    private const int _mask = _capacity - 1;

    /// <summary> Contiguous memory block of snapshots to maximize CPU L1/L2 cache hits. </summary>
    private readonly T[] _buffer = new T[_capacity];

    /// <summary> Clock synchronization manager. </summary>
    private TimeSyncEMA _timeSync;

    /// <summary> Manages the dynamic interpolation delay for this entity. </summary>
    private AdaptiveJitterBuffer _adaptiveJitterBuffer;

    /// <summary> Total number of snapshots added over the lifetime of this object. Used to calculate ring indices. </summary>
    private long _totalAdded;
    private double _lastLocalTime;
    private double _lastRemoteTime;

    /// <summary>
    /// Inserts a new snapshot into the ring buffer and updates the clock synchronization offset.
    /// </summary>
    /// <param name="snapshot">The state data received from the network. Passed by <see langword="in"/> to avoid copying the big struct.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSnapshot(in T snapshot)
    {
        if (_totalAdded > 0)
        {
            var newestIdx = (int)((_totalAdded - 1) & _mask);
            var newestTime = _buffer[newestIdx].RemoteTime;

            // sequence validation: drop out-of-order or duplicate packets
            if (snapshot.RemoteTime <= newestTime)
            {
                return;
            }

            // calculate the physical time it took for the packet to arrive versus the expected server interval
            var localDelta = snapshot.LocalTime - _lastLocalTime;
            var remoteDelta = snapshot.RemoteTime - _lastRemoteTime;

            // update the jitter variance
            _adaptiveJitterBuffer.Update(localDelta, remoteDelta);
        }

        // O(1) insertion: direct write to the masked index
        _buffer[_totalAdded & _mask] = snapshot;
        _totalAdded++;

        // update the EMA Offset (ServerTime - LocalTime) to synchronize the playback timeline
        _timeSync.Update(snapshot.RemoteTime, snapshot.LocalTime);

        _lastLocalTime = snapshot.LocalTime;
        _lastRemoteTime = snapshot.RemoteTime;
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
        t = 0;

        var count = (int)Math.Min(_totalAdded, _capacity);
        if (count < 2)
        {
            return EBufferState.Stale;
        }

        // consume the dynamic delay from the AdaptiveJitterBuffer
        var renderTime = localTime + _timeSync.SmoothOffset - _adaptiveJitterBuffer.CurrentDelay;
        var offset = Math.Max(0, _totalAdded - _capacity);

        // check if we need to extrapolate before searching
        var newestIdx = (int)((offset + count - 1) & _mask);
        if (renderTime > _buffer[newestIdx].RemoteTime)
        {
            var timeSinceNewest = renderTime - _buffer[newestIdx].RemoteTime;

            // hard limit extrapolation to 100ms. beyond this, predictions diverge too far from reality.
            if (timeSinceNewest > 0.1d)
            {
                return EBufferState.Stale;
            }

            fromIdx = newestIdx; // use this index's data + velocity * timeSinceNewest
            t = (float)timeSinceNewest;
            return EBufferState.Extrapolating;
        }

        var low = 0;
        var high = count - 1;
        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (_buffer[(offset + mid) & _mask].RemoteTime < renderTime)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        // if low is 0, the renderTime is older than our oldest buffered snapshot
        if (low == 0)
        {
            return EBufferState.Stale;
        }

        fromIdx = (int)((offset + low - 1) & _mask);
        toIdx = (int)((offset + low) & _mask);

        // access via ref readonly to avoid stack-copying the large ISnapshot structs
        ref readonly var snapFrom = ref _buffer[fromIdx];
        ref readonly var snapTo = ref _buffer[toIdx];

        var range = snapTo.RemoteTime - snapFrom.RemoteTime;
        t = range > 0 ? (float)((renderTime - snapFrom.RemoteTime) / range) : 0f;

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
    /// Clears the snapshotter and resets it to default
    /// </summary>
    public void Clear()
    {
        _totalAdded = 0;
        _timeSync = default;
        _adaptiveJitterBuffer = default;
        _lastLocalTime = 0;
        _lastRemoteTime = 0;
    }
}