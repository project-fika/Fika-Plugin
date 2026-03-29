using System;
using System.Runtime.CompilerServices;
using Fika.Core.Networking.Packets.Player;

namespace Fika.Core.Networking.Snapshotting;

/// <summary>
/// A high-performance, zero-allocation circular buffer designed for <see cref="PlayerStateSnapshot"/> interpolation. <br/>
/// Utilizes bitwise masking for O(1) insertions and binary search for O(log N) sampling.
/// </summary>
/// <remarks>
/// Heavily inspired by the id Tech 3 networking model (used in Quake III Arena) <br/>
/// Written by <b>Lacyway</b>
/// </remarks>
public sealed class PlayerSnapshotter
{
    /// <summary> Capacity must be a power of two to allow bitwise wrapping via <see cref="_mask"/>. </summary>
    private const int _capacity = 16;
    private const int _mask = _capacity - 1;

    /// <summary> Contiguous memory block of snapshots to maximize CPU L1/L2 cache hits. </summary>
    private readonly PlayerStateSnapshot[] _buffer = new PlayerStateSnapshot[_capacity];

    /// <summary>
    /// Value-type clock synchronization manager.
    /// </summary>
    private TimeSyncEMA _timeSync;

    /// <summary> Total number of snapshots added over the lifetime of this object. Used to calculate ring indices. </summary>
    private long _totalAdded;

    /// <summary>
    /// Inserts a new snapshot into the ring buffer and updates the clock synchronization offset.
    /// </summary>
    /// <param name="snapshot">The state data received from the network. Passed by <see langword="in"/> to avoid copying the big struct.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSnapshot(in PlayerStateSnapshot snapshot)
    {
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
    /// <param name="interpolationDelay">The amount of time (in seconds) to stay behind the server to mask network jitter.</param>
    /// <param name="fromIdx">The index of the snapshot immediately before the render time.</param>
    /// <param name="toIdx">The index of the snapshot immediately after the render time.</param>
    /// <param name="t">The 0-1 interpolation factor (lerp amount) between the two snapshots.</param>
    /// <returns><see langword="true"/> if valid interpolation boundaries were found; otherwise, <see langword="false"/>.</returns>
    public bool GetInterpolationIndices(double localTime, double interpolationDelay, out int fromIdx, out int toIdx, out float t)
    {
        fromIdx = toIdx = -1;
        t = 0;

        var count = (int)Math.Min(_totalAdded, _capacity);
        if (count < 2)
        {
            return false;
        }

        // calculate the target point on the synchronized timeline
        var renderTime = localTime + _timeSync.SmoothOffset - interpolationDelay;
        var offset = Math.Max(0, _totalAdded - _capacity);

        // binary Search: O(log N), efficiently finds the interpolation window in the circular buffer
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
            return false;
        }

        fromIdx = (int)((offset + low - 1) & _mask);
        toIdx = (int)((offset + low) & _mask);

        // access via ref readonly to avoid stack-copying the large PlayerStateSnapshot struct
        ref readonly var snapFrom = ref _buffer[fromIdx];
        ref readonly var snapTo = ref _buffer[toIdx];

        var range = snapTo.RemoteTime - snapFrom.RemoteTime;
        t = range > 0 ? (float)((renderTime - snapFrom.RemoteTime) / range) : 0f;

        return true;
    }

    /// <summary>
    /// Retrieves a reference to a snapshot in the buffer.
    /// </summary>
    /// <param name="index">The masked index retrieved from <see cref="GetInterpolationIndices"/>.</param>
    /// <returns>A <see langword="readonly"/> reference to the snapshot, preventing struct copies.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly PlayerStateSnapshot GetSnapshot(int index)
    {
        return ref _buffer[index];
    }
}