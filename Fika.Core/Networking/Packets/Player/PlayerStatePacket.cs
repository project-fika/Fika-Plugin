using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fika.Core.Main.ObservedClasses.Snapshotting;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player;

/// <summary>
/// State packet for a player
/// </summary>
/// <remarks>
/// Assumes little-endian architecture
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PlayerStatePacket
{
    /// <summary>
    /// Size in bytes of a <see cref="PlayerStatePacket"/>
    /// </summary>
    public static readonly byte PacketSize = (byte)Unsafe.SizeOf<PlayerStatePacket>();

    /// <summary>8 bytes, offset 0</summary>
    public readonly double RemoteTime;

    /// <summary>8 bytes, offset 8</summary>
    public readonly double LocalTime;

    /// <summary>12 bytes, offset 16 (Vector3 is 3 floats)</summary>
    public readonly Vector3 Position;

    /// <summary>4 bytes, offset 28</summary>
    private readonly float _rotYaw;

    /// <summary>2 bytes, offset 32</summary>
    private readonly ushort _tiltPacked;

    /// <summary>2 bytes, offset 34</summary>
    private readonly ushort _movementSpeedPacked;

    /// <summary>2 bytes, offset 36</summary>
    private readonly ushort _sprintSpeedPacked;

    /// <summary>2 bytes, offset 38</summary>
    private readonly ushort _poseLevelPacked;

    /// <summary>2 bytes, offset 40</summary>
    private readonly ushort _weaponOverlapPacked;

    /// <summary>1 byte, offset 42</summary>
    private readonly byte _headRotYawPacked;

    /// <summary>1 byte, offset 43</summary>
    private readonly byte _headRotPitchPacked;

    /// <summary>1 byte, offset 44</summary>
    private readonly byte _rotPitchPacked;

    /// <summary>1 byte, offset 45</summary>
    private readonly byte _moveDirXPacked;

    /// <summary>1 byte, offset 46</summary>
    private readonly byte _moveDirYPacked;

    /// <summary>1 byte, offset 47</summary>
    public readonly byte NetId;

    /// <summary>1 byte, offset 48</summary>
    private readonly byte _stepPacked;

    /// <summary>1 byte, offset 49</summary>
    private readonly byte _blindfirePacked;

    /// <summary>1 byte, offset 50</summary>
    public readonly EPlayerState State;  // Stored as byte

    /// <summary>1 byte, offset 51</summary>
    private readonly byte _boolFlags;    // 7 bools packed into 1 byte

    /// <summary>1 byte, offset 52</summary>
    private readonly byte _velocityDirXPacked;

    /// <summary>1 byte, offset 53</summary>
    private readonly byte _velocityDirYPacked;

    /// <summary>1 byte, offset 54</summary>
    private readonly byte _velocityDirZPacked;

    public BasePhysicalClass.PhysicalStateStruct Physical
    {
        get
        {
            return new()
            {
                StaminaExhausted = StaminaExhausted,
                OxygenExhausted = OxygenExhausted,
                HandsExhausted = HandsExhausted
            };
        }
    }

    public Vector3 Velocity
    {
        get
        {
            return new Vector3(UnpackByteToFloat(_velocityDirXPacked, -25f, 25f),
                UnpackByteToFloat(_velocityDirYPacked, -25f, 25f),
                UnpackByteToFloat(_velocityDirZPacked, -25f, 25f));
        }
    }

    public Vector2 HeadRotation
    {
        get
        {
            return new Vector2(UnpackByteToFloat(_headRotYawPacked, -50f, 20f),
                UnpackByteToFloat(_headRotPitchPacked, -40f, 40f));
        }
    }

    public Vector2 Rotation
    {
        get
        {
            return new Vector2(_rotYaw, UnpackByteToFloat(_rotPitchPacked,
                -90f, 90f));
        }
    }

    public Vector2 MovementDirection
    {
        get
        {
            return new Vector2(UnpackByteToFloat(_moveDirXPacked, -1f, 1f),
                UnpackByteToFloat(_moveDirYPacked, -1f, 1f));
        }
    }

    public float Tilt
    {
        get
        {
            return UnpackUShortToFloat(_tiltPacked, -5f, 5f);
        }
    }

    public float MovementSpeed
    {
        get
        {
            return UnpackUShortToFloat(_movementSpeedPacked, 0f, 1f);
        }
    }

    public float SprintSpeed
    {
        get
        {
            return UnpackUShortToFloat(_sprintSpeedPacked, 0f, 1f);
        }
    }

    public float PoseLevel
    {
        get
        {
            return UnpackUShortToFloat(_poseLevelPacked, 0f, 1f);
        }
    }

    public float WeaponOverlap
    {
        get
        {
            return UnpackUShortToFloat(_weaponOverlapPacked, 0f, 1f);
        }
    }

    public int Step
    {
        get
        {
            return UnpackByteToInt(_stepPacked, -1, 1);
        }
    }

    public int Blindfire
    {
        get
        {
            return UnpackByteToInt(_blindfirePacked, -1, 1);
        }
    }

    public readonly bool IsProne
    {
        get
        {
            return (_boolFlags & (1 << 0)) != 0;
        }
    }

    public readonly bool IsSprinting
    {
        get
        {
            return (_boolFlags & (1 << 1)) != 0;
        }
    }

    public readonly bool LeftStanceDisabled
    {
        get
        {
            return (_boolFlags & (1 << 2)) != 0;
        }
    }

    public readonly bool IsGrounded
    {
        get
        {
            return (_boolFlags & (1 << 3)) != 0;
        }
    }

    public readonly bool StaminaExhausted
    {
        get
        {
            return (_boolFlags & (1 << 4)) != 0;
        }
    }

    public readonly bool OxygenExhausted
    {
        get
        {
            return (_boolFlags & (1 << 5)) != 0;
        }
    }

    public readonly bool HandsExhausted
    {
        get
        {
            return (_boolFlags & (1 << 6)) != 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte PackFloatToByte(float value, float min, float max)
    {
        var clamped = Mathf.Clamp(value, min, max);
        const int maxInt = 255;
        var quantized = Mathf.RoundToInt((clamped - min) / (max - min) * maxInt);
        return (byte)quantized;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort PackFloatToUShort(float value, float min, float max)
    {
        var clamped = Mathf.Clamp(value, min, max);
        const int maxInt = ushort.MaxValue;
        var quantized = Mathf.RoundToInt((clamped - min) / (max - min) * maxInt);
        return (ushort)quantized;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte PackIntToByte(int value, int minValue, int maxValue)
    {
        const int minTarget = 0;
        const int maxTarget = byte.MaxValue;

        var clampedValue = Mathf.Clamp(value, minValue, maxValue) - minValue;
        var rangeInput = maxValue - minValue;

        var normalized = (float)clampedValue / rangeInput;
        var scaled = (int)(minTarget + (normalized * (maxTarget - minTarget)));

        return (byte)Mathf.Clamp(scaled, minTarget, maxTarget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float UnpackByteToFloat(byte packed, float min, float max)
    {
        var normalized = packed / (float)byte.MaxValue;
        return min + (normalized * (max - min));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float UnpackUShortToFloat(ushort packed, float min, float max)
    {
        var normalized = packed / (float)ushort.MaxValue;
        return min + (normalized * (max - min));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int UnpackByteToInt(byte packed, int minValue, int maxValue)
    {
        var normalized = packed / (float)byte.MaxValue;
        return (int)(minValue + (normalized * (maxValue - minValue)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte PackBools(params bool[] bools)
    {
        byte flags = 0;
        for (var i = 0; i < 7; i++)
        {
            if (bools[i])
            {
                flags |= (byte)(1 << i);
            }
        }
        return flags;
    }

    public static PlayerStatePacket FromBuffer(in ArraySegment<byte> buffer)
    {
        ref var firstByte = ref buffer.Array[buffer.Offset];
        return Unsafe.ReadUnaligned<PlayerStatePacket>(ref firstByte);
    }

    public PlayerStatePacket(FikaPlayer player, bool isMoving)
    {
        RemoteTime = NetworkTimeSync.NetworkTime;
        LocalTime = 0;

        Position = player.Position;

        _headRotYawPacked = PackFloatToByte(player.HeadRotation.x, -50f, 20f);
        _headRotPitchPacked = PackFloatToByte(player.HeadRotation.y, -40f, 40f);

        _rotYaw = player.Rotation.x;
        _rotPitchPacked = PackFloatToByte(player.Rotation.y, -90f, 90f);

        _moveDirXPacked = PackFloatToByte(isMoving ? player.MovementContext.MovementDirection.x : 0f, -1f, 1f);
        _moveDirYPacked = PackFloatToByte(isMoving ? player.MovementContext.MovementDirection.y : 0f, -1f, 1f);

        _tiltPacked = PackFloatToUShort(player.MovementContext.IsInMountedState ? player.MovementContext.MountedSmoothedTilt : player.MovementContext.SmoothedTilt, -5f, 5f);
        _movementSpeedPacked = PackFloatToUShort(player.MovementContext.SmoothedCharacterMovementSpeed, 0f, 1f);
        _sprintSpeedPacked = PackFloatToUShort(player.MovementContext.SprintSpeed, 0f, 1f);
        _poseLevelPacked = PackFloatToUShort(player.PoseLevel, 0f, 1f);
        _weaponOverlapPacked = PackFloatToUShort(player.ObservedOverlap, 0f, 1f);

        NetId = (byte)player.NetId;

        _stepPacked = PackIntToByte(player.MovementContext.Step, -1, 1);
        _blindfirePacked = PackIntToByte(player.MovementContext.BlindFire, -1, 1);

        State = player.CurrentManagedState.Name;

        _boolFlags = PackBools(
            player.IsInPronePose,
            player.MovementContext.IsSprintEnabled,
            player.LeftStanceDisabled,
            player.MovementContext.IsGrounded,
            player.Physical.SerializationStruct.StaminaExhausted,
            player.Physical.SerializationStruct.OxygenExhausted,
            player.Physical.SerializationStruct.HandsExhausted
        );

        var velocity = player.Velocity;
        _velocityDirXPacked = PackFloatToByte(velocity.x, -25f, 25f);
        _velocityDirYPacked = PackFloatToByte(velocity.y, -25f, 25f);
        _velocityDirZPacked = PackFloatToByte(velocity.z, -25f, 25f);
    }
}