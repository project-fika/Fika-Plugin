using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public abstract class Snapshotter<T> where T : ISnapshot
    {
        private readonly SortedList<double, T> buffer;
        private double localTimeline;
        private double localTimeScale;
        private readonly SnapshotInterpolationSettings interpolationSettings;
        private ExponentialMovingAverage driftEma;
        private ExponentialMovingAverage deliveryTimeEma;
        private readonly int sendRate;
        private readonly float sendInterval;
        private double bufferTimeMultiplier;

        protected Snapshotter()
        {
            buffer = new(32);
            localTimeScale = Time.timeScale;
            double smoothingRate = FikaPlugin.SmoothingRate.Value switch
            {
                FikaPlugin.ESmoothingRate.Low => 1.5,
                FikaPlugin.ESmoothingRate.Medium => 2,
                FikaPlugin.ESmoothingRate.High => 2.5,
                _ => 2,
            };
            sendRate = Singleton<IFikaNetworkManager>.Instance.SendRate;
            interpolationSettings = new(smoothingRate);
            bufferTimeMultiplier = interpolationSettings.bufferTimeMultiplier;
            driftEma = new(sendRate * interpolationSettings.driftEmaDuration);
            deliveryTimeEma = new(sendRate * interpolationSettings.deliveryTimeEmaDuration);
            sendInterval = 1f / sendRate;
        }

        private double BufferTime
        {
            get
            {
                return sendInterval * bufferTimeMultiplier;
            }
        }

        /// <summary>
        /// Checks the <see cref="buffer"/> and <see cref="ObservedCoopPlayer.Interpolate(ref PlayerStatePacket, ref PlayerStatePacket, double)"/>s any snapshots
        /// </summary>
        public void ManualUpdate()
        {
            if (buffer.Count > 0)
            {
                SnapshotInterpolation.Step(buffer, Time.unscaledDeltaTime, ref localTimeline, localTimeScale, out T fromSnapshot,
                    out T toSnapshot, out double ratio);
                Interpolate(toSnapshot, fromSnapshot, (float)ratio);
            }
        }

        /// <summary>
        /// Interpolates states in the <see cref="buffer"/>
        /// </summary>
        /// <param name="to">Goal state</param>
        /// <param name="from">State to lerp from</param>
        /// <param name="ratio">Interpolation ratio</param>
        public abstract void Interpolate(in T to, in T from, float ratio);

        /// <summary>
        /// Inserts a snapshot to the <see cref="buffer"/>
        /// </summary>
        /// <param name="snapshot"></param>
        public void Insert(T snapshot)
        {
            //localTimeline > snapshot.RemoteTime
            if (buffer.Count > interpolationSettings.bufferLimit)
            {
                buffer.Clear();
            }

            snapshot.LocalTime = NetworkTimeSync.NetworkTime;

            bufferTimeMultiplier = SnapshotInterpolation.DynamicAdjustment(sendInterval,
                deliveryTimeEma.StandardDeviation, interpolationSettings.dynamicAdjustmentTolerance);

            SnapshotInterpolation.InsertAndAdjust(buffer, interpolationSettings.bufferLimit, snapshot, ref localTimeline, ref localTimeScale,
                sendInterval, BufferTime, interpolationSettings.catchupSpeed, interpolationSettings.slowdownSpeed, ref driftEma,
                interpolationSettings.catchupNegativeThreshold, interpolationSettings.catchupPositiveThreshold, ref deliveryTimeEma);
        }

        /// <summary>
        /// Clears the <see cref="buffer"/>
        /// </summary>
        public void Clear()
        {
            buffer.Clear();
        }
    }
}
