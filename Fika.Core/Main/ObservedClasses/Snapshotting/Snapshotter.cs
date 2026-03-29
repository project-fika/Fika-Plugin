using System;
using System.Runtime.CompilerServices;
using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;

namespace Fika.Core.Main.ObservedClasses.Snapshotting;

public class Snapshotter
{
    private const int _maxSnapshots = SnapshotInterpolationSettings.BufferLimit;

    private readonly PlayerStateSnapshot[] _buffer;
    private int _bufferCount;

    private double _localTimeline;
    private double _localTimeScale;
    private readonly SnapshotInterpolationSettings _interpolationSettings;
    private ExponentialMovingAverage _driftEma;
    private ExponentialMovingAverage _deliveryTimeEma;
    private readonly int _sendRate;
    private readonly float _sendInterval;
    private double _bufferTimeMultiplier;
    private readonly ObservedPlayer _player;
    private readonly bool _isZombie;

    private const float _movementDeadZoneSqr = 0.05f * 0.05f;
    private const float _velocityDeadZoneSqr = 0.20f * 0.20f;

    internal Snapshotter(ObservedPlayer observedPlayer)
    {
        _buffer = new PlayerStateSnapshot[_maxSnapshots];
        _localTimeScale = Time.timeScale;
        _sendRate = Singleton<IFikaNetworkManager>.Instance.SendRate;
        _interpolationSettings = new();
        _bufferTimeMultiplier = _interpolationSettings.BufferTimeMultiplier;
        _driftEma = new(_sendRate * _interpolationSettings.DriftEmaDuration);
        _deliveryTimeEma = new(_sendRate * _interpolationSettings.DeliveryTimeEmaDuration);
        _sendInterval = 1f / _sendRate;
        _player = observedPlayer;
        _isZombie = observedPlayer.UsedSimplifiedSkeleton;
    }

    private double BufferTime
    {
        get
        {
            return _sendInterval * _bufferTimeMultiplier;
        }
    }

    /// <summary>
    /// Checks the <see cref="_buffer"/> and <see cref="Interpolate(in PlayerStateData, in PlayerStateData, float)"/>s any snapshots
    /// </summary>
    public void ManualUpdate(float unscaledDeltaTime)
    {
        if (_bufferCount > 0)
        {
            SnapshotInterpolation.Step(_buffer, ref _bufferCount, unscaledDeltaTime, ref _localTimeline, _localTimeScale,
                out var fromIndex, out var toIndex, out var ratio);

            Interpolate(in _buffer[fromIndex], in _buffer[toIndex], ratio);
        }
    }

    /// <summary>
    /// Interpolates states in the <see cref="_buffer"/>
    /// </summary>
    /// <param name="from">State to lerp from</param>
    /// <param name="to">Goal state</param>
    /// <param name="ratio">Interpolation ratio</param>
    public void Interpolate(in PlayerStateSnapshot from, in PlayerStateSnapshot to, float ratio)
    {
        var currentState = _player.CurrentPlayerState;
        currentState.ShouldUpdate = true;

        currentState.Rotation = new Vector2(
            Mathf.LerpAngle(from.Data.Rotation.x, to.Data.Rotation.x, ratio),
            Mathf.LerpUnclamped(from.Data.Rotation.y, to.Data.Rotation.y, ratio)
        );

        currentState.HeadRotation = Vector3.LerpUnclamped(from.Data.HeadRotation, to.Data.HeadRotation, ratio);
        currentState.Position = Vector3.LerpUnclamped(from.Data.Position, to.Data.Position, ratio);

        var newDir = currentState.MovementDirection = Vector2.LerpUnclamped(from.Data.MovementDirection, to.Data.MovementDirection, ratio);
        if (!_isZombie && (to.Data.State is EPlayerState.Idle or EPlayerState.Transition || newDir.sqrMagnitude < _movementDeadZoneSqr))
        {
            currentState.MovementDirection = Vector2.zero;
            currentState.IsMoving = false;
        }
        else
        {
            currentState.MovementDirection = newDir;
            currentState.IsMoving = true;
        }

        currentState.State = to.Data.State;
        currentState.Tilt = Mathf.LerpUnclamped(from.Data.Tilt, to.Data.Tilt, ratio);
        currentState.Step = to.Data.Step;
        currentState.MovementSpeed = Mathf.LerpUnclamped(from.Data.MovementSpeed, to.Data.MovementSpeed, ratio);
        currentState.SprintSpeed = Mathf.LerpUnclamped(from.Data.SprintSpeed, to.Data.SprintSpeed, ratio);
        currentState.IsProne = to.Data.IsProne;
        currentState.PoseLevel = Mathf.LerpUnclamped(from.Data.PoseLevel, to.Data.PoseLevel, ratio);
        currentState.IsSprinting = to.Data.IsSprinting;
        currentState.Stamina = to.Data.Physical;
        currentState.Blindfire = to.Data.Blindfire;
        currentState.WeaponOverlap = Mathf.LerpUnclamped(from.Data.WeaponOverlap, to.Data.WeaponOverlap, ratio);
        currentState.LeftStanceDisabled = to.Data.LeftStanceDisabled;
        currentState.IsGrounded = to.Data.IsGrounded;
        var velocity = Vector3.LerpUnclamped(from.Data.Velocity, to.Data.Velocity, ratio);
        if (velocity.sqrMagnitude < _velocityDeadZoneSqr)
        {
            velocity = Vector3.zero;
        }

        currentState.Velocity = velocity;
    }

    /// <summary>
    /// Inserts a snapshot to the <see cref="_buffer"/>
    /// </summary>
    /// <param name="snapshot">The snapshot to insert</param>
    /// <param name="networkTime">The current network time</param>
    public void Insert(in PlayerStateSnapshot snapshot, double networkTime)
    {
        if (_bufferCount >= _maxSnapshots)
        {
            _bufferCount = 0;
        }

        var low = 0;
        var high = _bufferCount - 1;
        var index = -1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var midTime = _buffer[mid].RemoteTime;

            if (midTime == snapshot.RemoteTime)
            {
                index = mid; // found exact match
                break;
            }

            if (midTime < snapshot.RemoteTime)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        if (index != -1)
        {
            _buffer[index] = snapshot;
        }
        else
        {
            index = low;
            if (index < _bufferCount)
            {
                Array.Copy(_buffer, index, _buffer, index + 1, _bufferCount - index);
            }
            _buffer[index] = snapshot;
            _bufferCount++;
        }

        _bufferTimeMultiplier = SnapshotInterpolation.DynamicAdjustment(_sendInterval, _deliveryTimeEma.StandardDeviation,
            _interpolationSettings.DynamicAdjustmentTolerance);

        SnapshotInterpolation.InsertAndAdjust(_buffer, _bufferCount, in _buffer[index],
            ref _localTimeline, ref _localTimeScale, _sendInterval,
            BufferTime, _interpolationSettings.CatchupSpeed, _interpolationSettings.SlowdownSpeed, ref _driftEma,
            _interpolationSettings.CatchupNegativeThreshold, _interpolationSettings.CatchupPositiveThreshold, ref _deliveryTimeEma);
    }
}