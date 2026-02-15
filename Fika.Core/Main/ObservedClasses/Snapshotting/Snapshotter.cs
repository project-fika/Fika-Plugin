using System;
using System.Runtime.CompilerServices;
using Comfort.Common;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;

namespace Fika.Core.Main.ObservedClasses.Snapshotting;

public unsafe class Snapshotter
{
    private const int _maxSnapshots = 32;
    private readonly PlayerStatePacket[] _buffer;
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
        _buffer = new PlayerStatePacket[_maxSnapshots];
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
    /// Checks the <see cref="_buffer"/> and <see cref="Interpolate(in PlayerStatePacket, in PlayerStatePacket, float)"/>s any snapshots
    /// </summary>
    public void ManualUpdate(float unscaledDeltaTime)
    {
        if (_bufferCount > 0)
        {
            SnapshotInterpolation.Step(_buffer, ref _bufferCount, unscaledDeltaTime, ref _localTimeline, _localTimeScale,
                out var fromSnapshot, out var toSnapshot, out var ratio);
            Interpolate(in fromSnapshot, in toSnapshot, ratio);
        }
    }

    /// <summary>
    /// Interpolates states in the <see cref="_buffer"/>
    /// </summary>
    /// <param name="from">State to lerp from</param>
    /// <param name="to">Goal state</param>
    /// <param name="ratio">Interpolation ratio</param>
    public void Interpolate(in PlayerStatePacket from, in PlayerStatePacket to, float ratio)
    {
        var currentState = _player.CurrentPlayerState;
        currentState.ShouldUpdate = true;

        currentState.Rotation = new Vector2(
            Mathf.LerpAngle(from.Rotation.x, to.Rotation.x, ratio),
            Mathf.LerpUnclamped(from.Rotation.y, to.Rotation.y, ratio)
        );

        currentState.HeadRotation = Vector3.LerpUnclamped(from.HeadRotation, to.HeadRotation, ratio);
        currentState.Position = Vector3.LerpUnclamped(from.Position, to.Position, ratio);

        var newDir = currentState.MovementDirection = Vector2.LerpUnclamped(from.MovementDirection, to.MovementDirection, ratio);
        if (!_isZombie && (to.State is EPlayerState.Idle or EPlayerState.Transition || newDir.sqrMagnitude < _movementDeadZoneSqr))
        {
            currentState.MovementDirection = Vector2.zero;
            currentState.IsMoving = false;
        }
        else
        {
            currentState.MovementDirection = newDir;
            currentState.IsMoving = true;
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
        var velocity = Vector3.LerpUnclamped(from.Velocity, to.Velocity, ratio);
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
    public void Insert(in PlayerStatePacket snapshot, double networkTime)
    {
        if (_bufferCount >= _interpolationSettings.BufferLimit)
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

        fixed (PlayerStatePacket* p = &_buffer[index])
        {
            *&p->LocalTime = networkTime;
        }

        _bufferTimeMultiplier = SnapshotInterpolation.DynamicAdjustment(_sendInterval, _deliveryTimeEma.StandardDeviation,
            _interpolationSettings.DynamicAdjustmentTolerance);

        SnapshotInterpolation.InsertAndAdjust(_buffer, _bufferCount, in _buffer[index],
            ref _localTimeline, ref _localTimeScale, _sendInterval,
            BufferTime, _interpolationSettings.CatchupSpeed, _interpolationSettings.SlowdownSpeed, ref _driftEma,
            _interpolationSettings.CatchupNegativeThreshold, _interpolationSettings.CatchupPositiveThreshold, ref _deliveryTimeEma);
    }
}
