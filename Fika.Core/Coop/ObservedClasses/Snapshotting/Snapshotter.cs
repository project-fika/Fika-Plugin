using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public class Snapshotter
    {
        private readonly SortedList<double, PlayerStatePacket> _buffer;
        private double _localTimeline;
        private double _localTimeScale;
        private readonly SnapshotInterpolationSettings _interpolationSettings;
        private ExponentialMovingAverage _driftEma;
        private ExponentialMovingAverage _deliveryTimeEma;
        private readonly int _sendRate;
        private readonly float _sendInterval;
        private double _bufferTimeMultiplier;
        //private readonly object _bufferLock;
        private readonly ObservedCoopPlayer _player;
        private readonly float _deadZone;

        internal Snapshotter(ObservedCoopPlayer observedPlayer)
        {
            _buffer = new(32);
            _localTimeScale = Time.timeScale;
            _sendRate = Singleton<IFikaNetworkManager>.Instance.SendRate;
            _interpolationSettings = new();
            _bufferTimeMultiplier = _interpolationSettings.bufferTimeMultiplier;
            _driftEma = new(_sendRate * _interpolationSettings.driftEmaDuration);
            _deliveryTimeEma = new(_sendRate * _interpolationSettings.deliveryTimeEmaDuration);
            _sendInterval = 1f / _sendRate;
            //_bufferLock = new();
            _player = observedPlayer;
            _deadZone = 0.05f * 0.05f;
        }

        private double BufferTime
        {
            get
            {
                return _sendInterval * _bufferTimeMultiplier;
            }
        }

        /// <summary>
        /// Checks the <see cref="_buffer"/> and <see cref="Interpolate(in PlayerStatePacket, in PlayerStatePacket, float)"/>s any snapshots
        /// </summary>
        public void ManualUpdate(float unscaledDeltaTime)
        {
            if (_buffer.Count > 0)
            {
                SnapshotInterpolation.Step(_buffer, unscaledDeltaTime, ref _localTimeline, _localTimeScale, out PlayerStatePacket fromSnapshot,
                    out PlayerStatePacket toSnapshot, out double ratio);
                Interpolate(in toSnapshot, in fromSnapshot, (float)ratio);
            }
        }

        /// <summary>
        /// Interpolates states in the <see cref="_buffer"/>
        /// </summary>
        /// <param name="to">Goal state</param>
        /// <param name="from">State to lerp from</param>
        /// <param name="ratio">Interpolation ratio</param>
        public void Interpolate(in PlayerStatePacket to, in PlayerStatePacket from, float ratio)
        {
            ObservedState currentState = _player.CurrentPlayerState;
            currentState.ShouldUpdate = true;

            currentState.Rotation = new Vector2(
                Mathf.LerpAngle(from.Rotation.x, to.Rotation.x, ratio),
                Mathf.LerpUnclamped(from.Rotation.y, to.Rotation.y, ratio)
            );

            currentState.HeadRotation = Vector3.LerpUnclamped(from.HeadRotation, to.HeadRotation, ratio);
            currentState.Position = Vector3.LerpUnclamped(from.Position, to.Position, ratio);

            Vector2 newDir = currentState.MovementDirection = Vector2.LerpUnclamped(from.MovementDirection, to.MovementDirection, ratio);
            if (to.State == EPlayerState.Idle || newDir.sqrMagnitude < _deadZone)
            {
                currentState.MovementDirection = Vector2.zero;
            }
            else
            {
                currentState.MovementDirection = newDir;
            }

            currentState.State = to.State;
            currentState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, ratio);
            currentState.Step = to.Step;
            currentState.MovementSpeed = Mathf.LerpUnclamped(from.MovementSpeed, to.MovementSpeed, ratio);
            currentState.SprintSpeed = Mathf.LerpUnclamped(from.SprintSpeed, to.SprintSpeed, ratio);
            currentState.IsProne = to.IsProne;
            currentState.PoseLevel = Mathf.LerpUnclamped(from.PoseLevel, to.PoseLevel, ratio);
            currentState.IsSprinting = to.IsSprinting;
            currentState.Stamina = to.Physical;
            currentState.Blindfire = to.Blindfire;
            currentState.WeaponOverlap = Mathf.LerpUnclamped(from.WeaponOverlap, to.WeaponOverlap, ratio);
            currentState.LeftStanceDisabled = to.LeftStanceDisabled;
            currentState.IsGrounded = to.IsGrounded;
        }

        /// <summary>
        /// Inserts a snapshot to the <see cref="_buffer"/>
        /// </summary>
        /// <param name="snapshot"></param>
        public void Insert(ref PlayerStatePacket snapshot, double networkTime)
        {
            if (_buffer.Count > _interpolationSettings.bufferLimit)
            {
                _buffer.Clear();
            }

            snapshot.LocalTime = networkTime;

            _bufferTimeMultiplier = SnapshotInterpolation.DynamicAdjustment(_sendInterval,
                _deliveryTimeEma.StandardDeviation, _interpolationSettings.dynamicAdjustmentTolerance);

            SnapshotInterpolation.InsertAndAdjust(_buffer, _interpolationSettings.bufferLimit, in snapshot, ref _localTimeline, ref _localTimeScale,
                _sendInterval, BufferTime, _interpolationSettings.catchupSpeed, _interpolationSettings.slowdownSpeed, ref _driftEma,
                _interpolationSettings.catchupNegativeThreshold, _interpolationSettings.catchupPositiveThreshold, ref _deliveryTimeEma);
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
