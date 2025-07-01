using Fika.Core.Networking;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public static class SortedListExtensions
    {
        public static void RemoveRange<T, U>(this SortedList<T, U> list, int amount)
        {
            for (int i = 0; i < amount && i < list.Count; ++i)
            {
                list.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Based on <see href="https://github.com/MirrorNetworking/Mirror/blob/master/Assets/Mirror/Core/SnapshotInterpolation/SnapshotInterpolation.cs"/><br/>(MIT License attached in namespace)
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
            double intervalWithJitter = sendInterval + jitterStandardDeviation;
            double multiples = intervalWithJitter / sendInterval;

            double safezone = multiples + dynamicAdjustmentTolerance;

            return Mathd.Clamp(safezone, 0, 5);
        }

        public static bool InsertIfNotExists(SortedList<double, PlayerStatePacket> buffer, int bufferLimit, in PlayerStatePacket snapshot)
        {
            if (buffer.Count >= bufferLimit)
            {
                return false;
            }

            int before = buffer.Count;
            buffer[snapshot.RemoteTime] = snapshot;
            return buffer.Count > before;
        }

        public static double TimelineClamp(double localTimeline, double bufferTime, double latestRemoteTime)
        {
            double targetTime = latestRemoteTime - bufferTime;
            double lowerBound = targetTime - bufferTime;
            double upperBound = targetTime + bufferTime;
            return Mathd.Clamp(localTimeline, lowerBound, upperBound);
        }

        public static void InsertAndAdjust(SortedList<double, PlayerStatePacket> buffer, int bufferLimit, in PlayerStatePacket snapshot,
            ref double localTimeline, ref double localTimescale, float sendInterval, double bufferTime,
            double catchupSpeed, double slowdownSpeed, ref ExponentialMovingAverage driftEma,
            float catchupNegativeThreshold, float catchupPositiveThreshold, ref ExponentialMovingAverage deliveryTimeEma)
        {
            if (buffer.Count == 0)
            {
                localTimeline = snapshot.RemoteTime - bufferTime;
            }

            if (InsertIfNotExists(buffer, bufferLimit, in snapshot))
            {
                if (buffer.Count >= 2)
                {
                    double previousLocalTime = buffer.Values[buffer.Count - 2].LocalTime;
                    double lastestLocalTime = buffer.Values[buffer.Count - 1].LocalTime;

                    double localDeliveryTime = lastestLocalTime - previousLocalTime;

                    deliveryTimeEma.Add(localDeliveryTime);
                }

                double latestRemoteTime = snapshot.RemoteTime;

                localTimeline = TimelineClamp(localTimeline, bufferTime, latestRemoteTime);

                double timeDiff = latestRemoteTime - localTimeline;

                driftEma.Add(timeDiff);

                double drift = driftEma.Value - bufferTime;

                double absoluteNegativeThreshold = sendInterval * catchupNegativeThreshold;
                double absolutePositiveThreshold = sendInterval * catchupPositiveThreshold;

                localTimescale = Timescale(drift, catchupSpeed, slowdownSpeed, absoluteNegativeThreshold, absolutePositiveThreshold);
            }
        }

        public static void Sample(SortedList<double, PlayerStatePacket> buffer, double localTimeline, out int from, out int to, out double t)
        {
            // this is a wrapper, so we cache it
            IList<PlayerStatePacket> values = buffer.Values;

            for (int i = 0; i < buffer.Count - 1; ++i)
            {
                PlayerStatePacket first = values[i];
                PlayerStatePacket second = values[i + 1];
                if (localTimeline >= first.RemoteTime && localTimeline <= second.RemoteTime)
                {
                    from = i;
                    to = i + 1;
                    t = Mathd.InverseLerp(first.RemoteTime, second.RemoteTime, localTimeline);
                    return;
                }
            }

            if (values[0].RemoteTime > localTimeline)
            {
                from = to = 0;
                t = 0;
                return;
            }

            from = to = buffer.Count - 1;
            t = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StepTime(double deltaTime, ref double localTimeline, double localTimescale)
        {
            localTimeline += deltaTime * localTimescale;
        }

        public static void StepInterpolation(SortedList<double, PlayerStatePacket> buffer, double localTimeline,
            out PlayerStatePacket fromSnapshot, out PlayerStatePacket toSnapshot, out double t)
        {
            Sample(buffer, localTimeline, out int from, out int to, out t);

            fromSnapshot = buffer.Values[from];
            toSnapshot = buffer.Values[to];

            buffer.RemoveRange(from);
        }

        public static void Step(SortedList<double, PlayerStatePacket> buffer, double deltaTime, ref double localTimeline,
            double localTimescale, out PlayerStatePacket fromSnapshot, out PlayerStatePacket toSnapshot, out double t)
        {
            StepTime(deltaTime, ref localTimeline, localTimescale);
            StepInterpolation(buffer, localTimeline, out fromSnapshot, out toSnapshot, out t);
        }
    }
}
