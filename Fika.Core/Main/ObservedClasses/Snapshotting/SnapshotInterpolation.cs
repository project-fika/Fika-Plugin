using System;
using System.Runtime.CompilerServices;
using Fika.Core.Networking.Packets.Player;

namespace Fika.Core.Main.ObservedClasses.Snapshotting;

/// <summary>
/// Based on <see href="https://github.com/MirrorNetworking/Mirror/blob/master/Assets/Mirror/Core/SnapshotInterpolation/SnapshotInterpolation.cs"/>
/// </summary>
public static class SnapshotInterpolation
{
    public static double Timescale(double drift, double catchupSpeed, double slowdownSpeed, double absoluteCatchupNegativeThreshold, double absoluteCatchupPositiveThreshold)
    {
        if (drift > absoluteCatchupPositiveThreshold)
        {
            return 1 + catchupSpeed;
        }

        if (drift < absoluteCatchupNegativeThreshold)
        {
            return 1 - slowdownSpeed;
        }

        return 1;
    }

    public static double DynamicAdjustment(double sendInterval, double jitterStandardDeviation, double dynamicAdjustmentTolerance)
    {
        var intervalWithJitter = sendInterval + jitterStandardDeviation;
        var multiples = intervalWithJitter / sendInterval;

        var safezone = multiples + dynamicAdjustmentTolerance;

        return Mathd.Clamp(safezone, 0, 5);
    }

    public static double TimelineClamp(double localTimeline, double bufferTime, double latestRemoteTime)
    {
        var targetTime = latestRemoteTime - bufferTime;
        var lowerBound = targetTime - bufferTime;
        var upperBound = targetTime + bufferTime;

        return Mathd.Clamp(localTimeline, lowerBound, upperBound);
    }

    public static void InsertAndAdjust(PlayerStatePacket[] buffer, int bufferCount, in PlayerStatePacket snapshot, ref double localTimeline, ref double localTimescale,
        float sendInterval, double bufferTime, double catchupSpeed, double slowdownSpeed, ref ExponentialMovingAverage driftEma, float catchupNegativeThreshold,
        float catchupPositiveThreshold, ref ExponentialMovingAverage deliveryTimeEma)
    {
        if (bufferCount == 1)
        {
            localTimeline = snapshot.RemoteTime - bufferTime;
        }

        if (bufferCount >= 2)
        {
            var previousLocalTime = buffer[bufferCount - 2].LocalTime;
            var latestLocalTime = buffer[bufferCount - 1].LocalTime;

            deliveryTimeEma.Add(latestLocalTime - previousLocalTime);
        }

        localTimeline = TimelineClamp(localTimeline, bufferTime, snapshot.RemoteTime);
        driftEma.Add(snapshot.RemoteTime - localTimeline);

        var drift = driftEma.Value - bufferTime;

        localTimescale = Timescale(drift, catchupSpeed, slowdownSpeed,
            sendInterval * catchupNegativeThreshold, sendInterval * catchupPositiveThreshold
        );
    }

    public static void Sample(PlayerStatePacket[] buffer, int bufferCount, double localTimeline, out int from, out int to, out float t)
    {
        // handle empty or single-packet buffers
        if (bufferCount == 0)
        {
            from = to = 0;
            t = 0;
            return;
        }

        if (bufferCount == 1)
        {
            from = to = 0;
            t = 0;
            return;
        }

        // if timeline is behind the very first packet
        if (localTimeline <= buffer[0].RemoteTime)
        {
            from = to = 0;
            t = 0;
            return;
        }

        // if timeline is ahead of the very last packet
        if (localTimeline >= buffer[bufferCount - 1].RemoteTime)
        {
            from = to = bufferCount - 1;
            t = 0;
            return;
        }

        // binary Search for the range
        var low = 0;
        var high = bufferCount - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            if (buffer[mid].RemoteTime <= localTimeline)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        // after the search:
        // 'low' is the first index with RemoteTime > localTimeline (the "to" snapshot)
        // 'low - 1' is the last index with RemoteTime <= localTimeline (the "from" snapshot)
        to = low;
        from = low - 1;

        // calculate interpolation factor t
        ref readonly var first = ref buffer[from];
        ref readonly var second = ref buffer[to];

        var duration = second.RemoteTime - first.RemoteTime;
        if (duration > 0.0001) // prevent division by zero
        {
            t = (float)((localTimeline - first.RemoteTime) / duration);
        }
        else
        {
            t = 0f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StepTime(double deltaTime, ref double localTimeline, double localTimescale)
    {
        localTimeline += deltaTime * localTimescale;
    }

    public static void StepInterpolation(PlayerStatePacket[] buffer, ref int bufferCount, double localTimeline,
        out PlayerStatePacket fromSnapshot, out PlayerStatePacket toSnapshot, out float t)
    {
        // find the indices (Sample needs to be updated to take the array)
        Sample(buffer, bufferCount, localTimeline, out var fromIndex, out var toIndex, out t);

        // assign the snapshots
        fromSnapshot = buffer[fromIndex];
        toSnapshot = buffer[toIndex];

        // remove old data (anything older than 'from' is no longer needed)
        if (fromIndex > 0)
        {
            var remaining = bufferCount - fromIndex;
            if (remaining > 0)
            {
                // shift the relevant data to the start of the array
                Array.Copy(buffer, fromIndex, buffer, 0, remaining);
                bufferCount = remaining;
            }
            else
            {
                bufferCount = 0;
            }
        }
    }

    public static void Step(PlayerStatePacket[] buffer, ref int bufferCount, double deltaTime, ref double localTimeline,
        double localTimescale, out PlayerStatePacket fromSnapshot, out PlayerStatePacket toSnapshot, out float t)
    {
        StepTime(deltaTime, ref localTimeline, localTimescale);
        StepInterpolation(buffer, ref bufferCount, localTimeline, out fromSnapshot, out toSnapshot, out t);
    }
}
