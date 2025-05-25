using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public abstract class Snapshotter<T> where T : ISnapshot
    {
        private readonly SortedList<double, T> _buffer;
        private double _localTimeline;
        private double _localTimeScale;
        private readonly SnapshotInterpolationSettings _interpolationSettings;
        private ExponentialMovingAverage _driftEma;
        private ExponentialMovingAverage _deliveryTimeEma;
        private readonly int _sendRate;
        private readonly float _sendInterval;
        private double _bufferTimeMultiplier;
        private object _bufferLock;

        protected Snapshotter()
        {
            _buffer = new(32);
            _localTimeScale = Time.timeScale;
            _sendRate = Singleton<IFikaNetworkManager>.Instance.SendRate;
            _interpolationSettings = new();
            _bufferTimeMultiplier = _interpolationSettings.bufferTimeMultiplier;
            _driftEma = new(_sendRate * _interpolationSettings.driftEmaDuration);
            _deliveryTimeEma = new(_sendRate * _interpolationSettings.deliveryTimeEmaDuration);
            _sendInterval = 1f / _sendRate;
            _bufferLock = new();
        }

        private double BufferTime
        {
            get
            {
                return _sendInterval * _bufferTimeMultiplier;
            }
        }

        /// <summary>
        /// Checks the <see cref="_buffer"/> and <see cref="ObservedCoopPlayer.Interpolate(ref PlayerStatePacket, ref PlayerStatePacket, double)"/>s any snapshots
        /// </summary>
        public void ManualUpdate(float unscaledDeltaTime)
        {
            if (_buffer.Count > 0)
            {
                SnapshotInterpolation.Step(_buffer, unscaledDeltaTime, ref _localTimeline, _localTimeScale, out T fromSnapshot,
                    out T toSnapshot, out double ratio);
                Interpolate(toSnapshot, fromSnapshot, (float)ratio);
            }
        }

        /// <summary>
        /// Interpolates states in the <see cref="_buffer"/>
        /// </summary>
        /// <param name="to">Goal state</param>
        /// <param name="from">State to lerp from</param>
        /// <param name="ratio">Interpolation ratio</param>
        public abstract void Interpolate(in T to, in T from, float ratio);

        /// <summary>
        /// Inserts a snapshot to the <see cref="_buffer"/>
        /// </summary>
        /// <param name="snapshot"></param>
        public void Insert(T snapshot, double networkTime)
        {
            //localTimeline > snapshot.RemoteTime
            lock (_bufferLock)
            {
                if (_buffer.Count > _interpolationSettings.bufferLimit)
                {
                    _buffer.Clear();
                }

                snapshot.LocalTime = networkTime;

                _bufferTimeMultiplier = SnapshotInterpolation.DynamicAdjustment(_sendInterval,
                    _deliveryTimeEma.StandardDeviation, _interpolationSettings.dynamicAdjustmentTolerance);

                SnapshotInterpolation.InsertAndAdjust(_buffer, _interpolationSettings.bufferLimit, snapshot, ref _localTimeline, ref _localTimeScale,
                    _sendInterval, BufferTime, _interpolationSettings.catchupSpeed, _interpolationSettings.slowdownSpeed, ref _driftEma,
                    _interpolationSettings.catchupNegativeThreshold, _interpolationSettings.catchupPositiveThreshold, ref _deliveryTimeEma); 
            }
        }

        /// <summary>
        /// Clears the <see cref="_buffer"/>
        /// </summary>
        public void Clear()
        {
            _buffer.Clear();
        }
    }
}
