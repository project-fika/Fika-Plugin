using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public class FikaSnapshotter
    {
        private readonly SortedList<double, PlayerStatePacket> buffer;
        private double localTimeline;
        private double localTimeScale;
        private readonly SnapshotInterpolationSettings interpolationSettings;
        private ExponentialMovingAverage driftEma;
        private ExponentialMovingAverage deliveryTimeEma;
        private readonly ObservedCoopPlayer player;
        private readonly int sendRate;
        private readonly float sendInterval;

        public FikaSnapshotter(ObservedCoopPlayer player)
        {
            buffer = [];
            localTimeScale = Time.timeScale;
            this.player = player;
            double smoothingRate = FikaPlugin.SmoothingRate.Value switch
            {
                FikaPlugin.ESmoothingRate.Low => 1.5,
                FikaPlugin.ESmoothingRate.Medium => 2,
                FikaPlugin.ESmoothingRate.High => 2.5,
                _ => 2,
            };
            sendRate = Singleton<IFikaNetworkManager>.Instance.SendRate;
            interpolationSettings = new(smoothingRate);
            driftEma = new(sendRate * interpolationSettings.driftEmaDuration);
            deliveryTimeEma = new(sendRate * interpolationSettings.deliveryTimeEmaDuration);
            sendInterval = 1f / sendRate;
        }

        private double BufferTime
        {
            get
            {
                return sendInterval * interpolationSettings.bufferTimeMultiplier;
            }
        }

        /// <summary>
        /// Checks the <see cref="buffer"/> and <see cref="ObservedCoopPlayer.Interpolate(ref PlayerStatePacket, ref PlayerStatePacket, double)"/>s any snapshots
        /// </summary>
        public void ManualUpdate()
        {
            if (buffer.Count > 0)
            {
                SnapshotInterpolation.Step(buffer, Time.unscaledDeltaTime, ref localTimeline, localTimeScale, out PlayerStatePacket fromSnapshot,
                    out PlayerStatePacket toSnapshot, out double ratio);
                player.Interpolate(ref toSnapshot, ref fromSnapshot, ratio);
            }
        }

        /// <summary>
        /// Inserts a snapshot to the <see cref="buffer"/>
        /// </summary>
        /// <param name="snapshot"></param>
        public void Insert(PlayerStatePacket snapshot)
        {
            snapshot.LocalTime = NetworkTimeSync.Time;
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
