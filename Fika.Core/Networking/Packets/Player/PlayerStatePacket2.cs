using Fika.Core.Main.ObservedClasses.Snapshotting;
using Fika.Core.Main.Players;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fika.Core.Networking.Packets.Player;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerStatePacket2
{
    /// <summary>8 bytes, offset 0</summary>
    public double RemoteTime;

    /// <summary>8 bytes, offset 8</summary>
    public double LocalTime;

    /// <summary>12 bytes, offset 16 (Vector3 is 3 floats)</summary>
    public Vector3 Position;

    /// <summary>4 bytes, offset 28</summary>
    private float _rotYaw;

    /// <summary>2 bytes, offset 32</summary>
    private ushort _tiltPacked;

    /// <summary>2 bytes, offset 34</summary>
    private ushort _movementSpeedPacked;

    /// <summary>2 bytes, offset 36</summary>
    private ushort _sprintSpeedPacked;

    /// <summary>2 bytes, offset 38</summary>
    private ushort _poseLevelPacked;

    /// <summary>2 bytes, offset 40</summary>
    private ushort _weaponOverlapPacked;

    /// <summary>1 byte, offset 42</summary>
    private byte _headRotYawPacked;

    /// <summary>1 byte, offset 43</summary>
    private byte _headRotPitchPacked;

    /// <summary>1 byte, offset 44</summary>
    private byte _rotPitchPacked;

    /// <summary>1 byte, offset 45</summary>
    private byte _moveDirXPacked;

    /// <summary>1 byte, offset 46</summary>
    private byte _moveDirYPacked;

    /// <summary>1 byte, offset 47</summary>
    public byte NetId;

    /// <summary>1 byte, offset 48</summary>
    private byte _stepPacked;

    /// <summary>1 byte, offset 49</summary>
    private byte _blindfirePacked;

    /// <summary>1 byte, offset 50</summary>
    public EPlayerState State;  // Stored as byte

    /// <summary>1 byte, offset 51</summary>
    private byte _boolFlags;    // 7 bools packed into 1 byte

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

    public Vector2 HeadRotation
    {
        readonly get
        {
            return new Vector2(UnpackByteToFloat(_headRotYawPacked, -50f, 20f), UnpackByteToFloat(_headRotPitchPacked, -40f, 40f));
        }

        set
        {
            _headRotYawPacked = PackFloatToByte(value.x, -50f, 20f);
            _headRotPitchPacked = PackFloatToByte(value.y, -40f, 40f);
        }
    }

    public Vector2 Rotation
    {
        readonly get
        {
            return new Vector2(_rotYaw, UnpackByteToFloat(_rotPitchPacked, -90f, 90f));
        }

        set
        {
            _rotYaw = value.x;
            _rotPitchPacked = PackFloatToByte(value.y, -90f, 90f);
        }
    }

    public Vector2 MovementDirection
    {
        readonly get
        {
            return new Vector2(UnpackByteToFloat(_moveDirXPacked, -1f, 1f), UnpackByteToFloat(_moveDirYPacked, -1f, 1f));
        }

        set
        {
            _moveDirXPacked = PackFloatToByte(value.x, -1f, 1f);
            _moveDirYPacked = PackFloatToByte(value.y, -1f, 1f);
        }
    }

    public float Tilt
    {
        readonly get
        {
            return UnpackUShortToFloat(_tiltPacked, -5f, 5f);
        }

        set
        {
            _tiltPacked = PackFloatToUShort(value, -5f, 5f);
        }
    }

    public float MovementSpeed
    {
        readonly get
        {
            return UnpackUShortToFloat(_movementSpeedPacked, 0f, 1f);
        }

        set
        {
            _movementSpeedPacked = PackFloatToUShort(value, 0f, 1f);
        }
    }

    public float SprintSpeed
    {
        readonly get
        {
            return UnpackUShortToFloat(_sprintSpeedPacked, 0f, 1f);
        }

        set
        {
            _sprintSpeedPacked = PackFloatToUShort(value, 0f, 1f);
        }
    }

    public float PoseLevel
    {
        readonly get
        {
            return UnpackUShortToFloat(_poseLevelPacked, 0f, 1f);
        }

        set
        {
            _poseLevelPacked = PackFloatToUShort(value, 0f, 1f);
        }
    }

    public float WeaponOverlap
    {
        readonly get
        {
            return UnpackUShortToFloat(_weaponOverlapPacked, 0f, 1f);
        }

        set
        {
            _weaponOverlapPacked = PackFloatToUShort(value, 0f, 1f);
        }
    }

    public int Step
    {
        readonly get
        {
            return UnpackByteToInt(_stepPacked, -1, 1);
        }

        set
        {
            _stepPacked = PackIntToByte(value, -1, 1);
        }
    }

    public int Blindfire
    {
        readonly get
        {
            return UnpackByteToInt(_blindfirePacked, -1, 1);
        }

        set
        {
            _blindfirePacked = PackIntToByte(value, -1, 1);
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

    private static byte PackFloatToByte(float value, float min, float max)
    {
        float clamped = Mathf.Clamp(value, min, max);
        int maxInt = 255;
        int quantized = Mathf.RoundToInt((clamped - min) / (max - min) * maxInt);
        return (byte)quantized;
    }

    private static ushort PackFloatToUShort(float value, float min, float max)
    {
        float clamped = Mathf.Clamp(value, min, max);
        int maxInt = ushort.MaxValue;
        int quantized = Mathf.RoundToInt((clamped - min) / (max - min) * maxInt);
        return (ushort)quantized;
    }

    private static byte PackIntToByte(int value, int minValue, int maxValue)
    {
        int minTarget = 0;
        int maxTarget = byte.MaxValue;

        int clampedValue = Mathf.Clamp(value, minValue, maxValue) - minValue;
        int rangeInput = maxValue - minValue;

        float normalized = (float)clampedValue / rangeInput;
        int scaled = (int)(minTarget + normalized * (maxTarget - minTarget));

        return (byte)Mathf.Clamp(scaled, minTarget, maxTarget);
    }

    private static float UnpackByteToFloat(byte packed, float min, float max)
    {
        float normalized = packed / (float)byte.MaxValue;
        return min + normalized * (max - min);
    }

    private static float UnpackUShortToFloat(ushort packed, float min, float max)
    {
        float normalized = packed / (float)ushort.MaxValue;
        return min + normalized * (max - min);
    }

    private static int UnpackByteToInt(byte packed, int minValue, int maxValue)
    {
        float normalized = packed / (float)byte.MaxValue;
        return (int)(minValue + normalized * (maxValue - minValue));
    }

    public static byte PackBools(params bool[] bools)
    {
        byte flags = 0;
        for (int i = 0; i < 7; i++)
        {
            if (bools[i])
            {
                flags |= (byte)(1 << i);
            }
        }
        return flags;
    }

    public static PlayerStatePacket2 FromBuffer(in ArraySegment<byte> buffer)
    {
        ref byte firstByte = ref buffer.Array[buffer.Offset];
        return Unsafe.ReadUnaligned<PlayerStatePacket2>(ref firstByte);
    }

    public void UpdateFromPlayer(FikaPlayer player, bool isMoving)
    {
        RemoteTime = NetworkTimeSync.NetworkTime;

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
    }

    public static PlayerStatePacket2 CreateFromPlayer(FikaPlayer player, bool isMoving)
    {
        PlayerStatePacket2 packet = new()
        {
            RemoteTime = NetworkTimeSync.NetworkTime,
            LocalTime = 0,

            Position = player.Position,

            _headRotYawPacked = PackFloatToByte(player.HeadRotation.x, -50f, 20f),
            _headRotPitchPacked = PackFloatToByte(player.HeadRotation.y, -40f, 40f),

            _rotYaw = player.Rotation.x,
            _rotPitchPacked = PackFloatToByte(player.Rotation.y, -90f, 90f),

            _moveDirXPacked = PackFloatToByte(isMoving ? player.MovementContext.MovementDirection.x : 0f, -1f, 1f),
            _moveDirYPacked = PackFloatToByte(isMoving ? player.MovementContext.MovementDirection.y : 0f, -1f, 1f),

            _tiltPacked = PackFloatToUShort(player.MovementContext.IsInMountedState ? player.MovementContext.MountedSmoothedTilt : player.MovementContext.SmoothedTilt, -5f, 5f),
            _movementSpeedPacked = PackFloatToUShort(player.MovementContext.SmoothedCharacterMovementSpeed, 0f, 1f),
            _sprintSpeedPacked = PackFloatToUShort(player.MovementContext.SprintSpeed, 0f, 1f),
            _poseLevelPacked = PackFloatToUShort(player.PoseLevel, 0f, 1f),
            _weaponOverlapPacked = PackFloatToUShort(player.ObservedOverlap, 0f, 1f),

            NetId = (byte)player.NetId,

            _stepPacked = PackIntToByte(player.MovementContext.Step, -1, 1),
            _blindfirePacked = PackIntToByte(player.MovementContext.BlindFire, -1, 1),

            State = player.CurrentManagedState.Name,

            _boolFlags = PackBools(
                player.IsInPronePose,
                player.MovementContext.IsSprintEnabled,
                player.LeftStanceDisabled,
                player.MovementContext.IsGrounded,
                player.Physical.SerializationStruct.StaminaExhausted,
                player.Physical.SerializationStruct.OxygenExhausted,
                player.Physical.SerializationStruct.HandsExhausted
            )
        };

        return packet;
    }
}