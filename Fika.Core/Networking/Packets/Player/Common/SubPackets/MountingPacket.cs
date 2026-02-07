using EFT.WeaponMounting;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public sealed class MountingPacket : IPoolSubPacket
{
    private MountingPacket()
    {

    }

    public static MountingPacket CreateInstance()
    {
        return new();
    }

    public static MountingPacket FromValue(MountingPacketStruct.EMountingCommand command, bool isMounted,
        Vector3 mountDirection, Vector3 mountingPoint, float currentMountingPointVerticalOffset, short mountingDirection)
    {
        var packet = CommonSubPacketPoolManager.Instance.GetPacket<MountingPacket>(ECommonSubPacketType.Mounting);
        packet.Command = command;
        packet.IsMounted = isMounted;
        packet.MountDirection = mountDirection;
        packet.MountingPoint = mountingPoint;
        packet.CurrentMountingPointVerticalOffset = currentMountingPointVerticalOffset;
        packet.MountingDirection = mountingDirection;
        return packet;
    }

    public MountingPacketStruct.EMountingCommand Command;
    public bool IsMounted;
    public Vector3 MountDirection;
    public Vector3 MountingPoint;
    public Vector3 TargetPos;
    public float TargetPoseLevel;
    public float TargetHandsRotation;
    public Vector2 PoseLimit;
    public Vector2 PitchLimit;
    public Vector2 YawLimit;
    public Quaternion TargetBodyRotation;
    public float CurrentMountingPointVerticalOffset;
    public short MountingDirection;
    public float TransitionTime;

    public void Execute(FikaPlayer player)
    {
        switch (Command)
        {
            case MountingPacketStruct.EMountingCommand.Enter:
                {
                    player.MovementContext.PlayerMountingPointData.SetData(new MountPointData(MountingPoint, MountDirection,
                        (EMountSideDirection)MountingDirection), TargetPos, TargetPoseLevel, TargetHandsRotation,
                        TransitionTime, TargetBodyRotation, PoseLimit, PitchLimit, YawLimit);
                    player.MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset = CurrentMountingPointVerticalOffset;
                    player.MovementContext.EnterMountedState();
                }
                break;
            case MountingPacketStruct.EMountingCommand.Exit:
                {
                    player.MovementContext.ExitMountedState();
                }
                break;
            case MountingPacketStruct.EMountingCommand.Update:
                {
                    player.MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset = CurrentMountingPointVerticalOffset;
                }
                break;
            case MountingPacketStruct.EMountingCommand.StartLeaving:
                {
                    if (player.MovementContext is ObservedMovementContext observedMovementContext)
                    {
                        observedMovementContext.ObservedStartExitingMountedState();
                    }
                }
                break;
            default:
                break;
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Command);
        if (Command == MountingPacketStruct.EMountingCommand.Update)
        {
            writer.Put(CurrentMountingPointVerticalOffset);
        }
        if (Command is <= MountingPacketStruct.EMountingCommand.Exit)
        {
            writer.Put(IsMounted);
        }
        if (Command == MountingPacketStruct.EMountingCommand.Enter)
        {
            writer.PutUnmanaged(MountDirection);
            writer.PutUnmanaged(MountingPoint);
            writer.Put(MountingDirection);
            writer.Put(TransitionTime);
            writer.PutUnmanaged(TargetPos);
            writer.Put(TargetPoseLevel);
            writer.Put(TargetHandsRotation);
            writer.PutUnmanaged(TargetBodyRotation);
            writer.PutUnmanaged(PoseLimit);
            writer.PutUnmanaged(PitchLimit);
            writer.PutUnmanaged(YawLimit);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Command = (MountingPacketStruct.EMountingCommand)reader.GetByte();
        if (Command == MountingPacketStruct.EMountingCommand.Update)
        {
            CurrentMountingPointVerticalOffset = reader.GetFloat();
        }
        if (Command is <= MountingPacketStruct.EMountingCommand.Exit)
        {
            IsMounted = reader.GetBool();
        }
        ;
        if (Command == MountingPacketStruct.EMountingCommand.Enter)
        {
            MountDirection = reader.GetUnmanaged<Vector3>();
            MountingPoint = reader.GetUnmanaged<Vector3>();
            MountingDirection = reader.GetShort();
            TransitionTime = reader.GetFloat();
            TargetPos = reader.GetUnmanaged<Vector3>();
            TargetPoseLevel = reader.GetFloat();
            TargetHandsRotation = reader.GetFloat();
            TargetBodyRotation = reader.GetUnmanaged<Quaternion>();
            PoseLimit = reader.GetUnmanaged<Vector2>();
            PitchLimit = reader.GetUnmanaged<Vector2>();
            YawLimit = reader.GetUnmanaged<Vector2>();
        }
    }

    public void Dispose()
    {
        Command = default;
        IsMounted = false;
        MountDirection = default;
        MountingPoint = default;
        TargetPos = default;
        TargetPoseLevel = 0f;
        TargetHandsRotation = 0f;
        PoseLimit = default;
        PitchLimit = default;
        YawLimit = default;
        TargetBodyRotation = default;
        CurrentMountingPointVerticalOffset = 0f;
        MountingDirection = 0;
        TransitionTime = 0f;
    }
}
