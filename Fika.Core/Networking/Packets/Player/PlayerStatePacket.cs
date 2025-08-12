using Fika.Core.Main.ObservedClasses.Snapshotting;
using Fika.Core.Main.Players;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.Player;

public struct PlayerStatePacket : INetSerializable
{
    /// <summary>
    /// The remote timestamp (when it was sent locally by the remote)
    /// </summary>
    public double RemoteTime;
    /// <summary>
    /// The local timestamp (when it was received on our end)
    /// </summary>
    public double LocalTime;

    public Vector3 Position;
    public Vector2 HeadRotation;
    public Vector2 Rotation;
    public Vector2 MovementDirection;

    public float Tilt;
    public float MovementSpeed;
    public float SprintSpeed;
    public float PoseLevel;
    public float WeaponOverlap;

    public int NetId;
    public int Step;
    public int Blindfire;

    public EPlayerState State;

    public bool IsProne;
    public bool IsSprinting;
    public bool LeftStanceDisabled;
    public bool IsGrounded;

    public BasePhysicalClass.PhysicalStateStruct Physical;

    public PlayerStatePacket(int netId)
    {
        NetId = netId;
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        byte boolFlags = 0;
        if (IsProne) boolFlags |= 1 << 0;
        if (IsSprinting) boolFlags |= 1 << 1;
        if (LeftStanceDisabled) boolFlags |= 1 << 2;
        if (IsGrounded) boolFlags |= 1 << 3;
        writer.Put(boolFlags);

        writer.Put(RemoteTime);
        writer.Put(LocalTime);

        writer.Put((byte)NetId);
        writer.PutPackedInt(Step, -1, 1);
        writer.PutPackedInt(Blindfire, -1, 1);
        writer.PutEnum(State);

        writer.PutUnmanaged(Position);
        writer.PutHeadRotation(HeadRotation);
        writer.PutRotation(Rotation);
        writer.PutMovementDirection(MovementDirection);

        writer.PutPackedFloat(Tilt, -5f, 5f, EFloatCompression.High);
        writer.PutPackedFloat(MovementSpeed, 0f, 1f, EFloatCompression.High);
        writer.PutPackedFloat(SprintSpeed, 0f, 1f, EFloatCompression.High);
        writer.PutPackedFloat(PoseLevel, 0f, 1f, EFloatCompression.High);
        writer.PutPackedFloat(WeaponOverlap, 0f, 1f, EFloatCompression.High);

        writer.PutPhysical(Physical);
    }

    public void Deserialize(NetDataReader reader)
    {
        byte boolFlags = reader.GetByte();
        IsProne = (boolFlags & 1 << 0) != 0;
        IsSprinting = (boolFlags & 1 << 1) != 0;
        LeftStanceDisabled = (boolFlags & 1 << 2) != 0;
        IsGrounded = (boolFlags & 1 << 3) != 0;

        RemoteTime = reader.GetDouble();
        LocalTime = reader.GetDouble();

        NetId = reader.GetByte();
        Step = reader.GetPackedInt(-1, 1);
        Blindfire = reader.GetPackedInt(-1, 1);
        State = reader.GetEnum<EPlayerState>();

        Position = reader.GetUnmanaged<Vector3>();
        HeadRotation = reader.GetHeadRotation();
        Rotation = reader.GetRotation();
        MovementDirection = reader.GetMovementDirection();

        Tilt = reader.GetPackedFloat(-5f, 5f, EFloatCompression.High);
        MovementSpeed = reader.GetPackedFloat(0f, 1f, EFloatCompression.High);
        SprintSpeed = reader.GetPackedFloat(0f, 1f, EFloatCompression.High);
        PoseLevel = reader.GetPackedFloat(0f, 1f, EFloatCompression.High);
        WeaponOverlap = reader.GetPackedFloat(0f, 1f, EFloatCompression.High);

        Physical = reader.GetPhysical();
    }

    public void UpdateData(FikaPlayer player, bool isMoving)
    {
        Position = player.Position;
        Rotation = player.Rotation;
        HeadRotation = player.HeadRotation;
        MovementDirection = isMoving ? player.MovementContext.MovementDirection : Vector2.zero;
        State = player.CurrentManagedState.Name;
        Tilt = player.MovementContext.IsInMountedState ? player.MovementContext.MountedSmoothedTilt : player.MovementContext.SmoothedTilt;
        Step = player.MovementContext.Step;
        MovementSpeed = player.MovementContext.SmoothedCharacterMovementSpeed;
        SprintSpeed = player.MovementContext.SprintSpeed;
        IsProne = player.IsInPronePose;
        PoseLevel = player.PoseLevel;
        IsSprinting = player.MovementContext.IsSprintEnabled;
        Physical = player.Physical.SerializationStruct;
        Blindfire = player.MovementContext.BlindFire;
        WeaponOverlap = player.ObservedOverlap;
        LeftStanceDisabled = player.LeftStanceDisabled;
        IsGrounded = player.MovementContext.IsGrounded;
        RemoteTime = NetworkTimeSync.NetworkTime;
    }
}