using Comfort.Common;
using EFT.Animations;
using EFT;
using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BaseBallistic;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

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
                Interpolate(ref toSnapshot, ref fromSnapshot, ratio);
            }
        }

        /// <summary>
        /// Inserts a snapshot to the <see cref="buffer"/>
        /// </summary>
        /// <param name="snapshot"></param>
        public void Insert(PlayerStatePacket snapshot)
        {
            snapshot.LocalTime = NetworkTimeSync.Time;
            interpolationSettings.bufferTimeMultiplier = SnapshotInterpolation.DynamicAdjustment(sendInterval,
                deliveryTimeEma.StandardDeviation, interpolationSettings.dynamicAdjustmentTolerance);
            SnapshotInterpolation.InsertAndAdjust(buffer, interpolationSettings.bufferLimit, snapshot, ref localTimeline, ref localTimeScale,
                sendInterval, BufferTime, interpolationSettings.catchupSpeed, interpolationSettings.slowdownSpeed, ref driftEma,
                interpolationSettings.catchupNegativeThreshold, interpolationSettings.catchupPositiveThreshold, ref deliveryTimeEma);
        }

        public void Interpolate(ref PlayerStatePacket to, ref PlayerStatePacket from, double ratio)
        {
            float interpolateRatio = (float)ratio;

            player.CurrentPlayerState.Rotation = new Vector2(Mathf.LerpAngle(from.Rotation.x, to.Rotation.x, interpolateRatio),
                Mathf.LerpUnclamped(from.Rotation.y, to.Rotation.y, interpolateRatio));
            player.CurrentPlayerState.HeadRotation = Vector3.LerpUnclamped(from.HeadRotation, to.HeadRotation, interpolateRatio);
            player.CurrentPlayerState.PoseLevel = from.PoseLevel + (to.PoseLevel - from.PoseLevel);
            player.CurrentPlayerState.Position = Vector3.LerpUnclamped(from.Position, to.Position, interpolateRatio);
            player.CurrentPlayerState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, interpolateRatio);
            player.CurrentPlayerState.MovementDirection = to.MovementDirection;
            player.CurrentPlayerState.State = to.State;
            player.CurrentPlayerState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, interpolateRatio);
            player.CurrentPlayerState.Step = to.Step;
            player.CurrentPlayerState.AnimatorStateIndex = to.AnimatorStateIndex;
            player.CurrentPlayerState.CharacterMovementSpeed = to.CharacterMovementSpeed;
            player.CurrentPlayerState.IsProne = to.IsProne;
            player.CurrentPlayerState.PoseLevel = Mathf.LerpUnclamped(from.PoseLevel, to.PoseLevel, interpolateRatio);
            player.CurrentPlayerState.IsSprinting = to.IsSprinting;
            player.CurrentPlayerState.Stamina = to.Stamina;
            player.CurrentPlayerState.Blindfire = to.Blindfire;
            player.CurrentPlayerState.WeaponOverlap = Mathf.LerpUnclamped(from.WeaponOverlap, to.WeaponOverlap, interpolateRatio);
            player.CurrentPlayerState.LeftStanceDisabled = to.LeftStanceDisabled;
            player.CurrentPlayerState.IsGrounded = to.IsGrounded;
            player.CurrentPlayerState.HasGround = to.HasGround;
            player.CurrentPlayerState.SurfaceSound = to.SurfaceSound;
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
