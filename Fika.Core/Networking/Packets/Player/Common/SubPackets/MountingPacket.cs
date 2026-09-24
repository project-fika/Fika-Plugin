using EFT.WeaponMounting;
using Fika.Core.Main.ObservedClasses;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct MountingPacket
{
    public MountingPacket(EFT.MountingPacket.EMountingCommand command, bool isMounted,
        Vector3 mountDirection, Vector3 mountingPoint, float currentMountingPointVerticalOffset, short mountingDirection)
    {
        Command = command;
        IsMounted = isMounted;
        MountDirection = mountDirection;
        MountingPoint = mountingPoint;
        CurrentMountingPointVerticalOffset = currentMountingPointVerticalOffset;
        MountingDirection = mountingDirection;
    }

    public MountingPacket(NetDataReader reader)
    {
        Command = (EFT.MountingPacket.EMountingCommand)reader.GetByte();
        if (Command == EFT.MountingPacket.EMountingCommand.Update)
        {
            CurrentMountingPointVerticalOffset = reader.GetFloat();
        }
        if (Command is <= EFT.MountingPacket.EMountingCommand.Exit)
        {
            IsMounted = reader.GetBool();
        }
        ;
        if (Command == EFT.MountingPacket.EMountingCommand.Enter)
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

    public MountingPacket(EFT.MountingPacket.EMountingCommand command, bool isMounted, Vector3 mountDirection,
        Vector3 mountingPoint, Vector3 targetPos, float targetPoseLevel, float targetHandsRotation, Vector2 poseLimit,
        Vector2 pitchLimit, Vector2 yawLimit, Quaternion targetBodyRotation, float currentMountingPointVerticalOffset,
        short mountingDirection, float transitionTime)
    {
        Command = command;
        IsMounted = isMounted;
        MountDirection = mountDirection;
        MountingPoint = mountingPoint;
        TargetPos = targetPos;
        TargetPoseLevel = targetPoseLevel;
        TargetHandsRotation = targetHandsRotation;
        PoseLimit = poseLimit;
        PitchLimit = pitchLimit;
        YawLimit = yawLimit;
        TargetBodyRotation = targetBodyRotation;
        CurrentMountingPointVerticalOffset = currentMountingPointVerticalOffset;
        MountingDirection = mountingDirection;
        TransitionTime = transitionTime;
    }

    public readonly EFT.MountingPacket.EMountingCommand Command;
    public readonly bool IsMounted;
    public readonly Vector3 MountDirection;
    public readonly Vector3 MountingPoint;
    public readonly Vector3 TargetPos;
    public readonly float TargetPoseLevel;
    public readonly float TargetHandsRotation;
    public readonly Vector2 PoseLimit;
    public readonly Vector2 PitchLimit;
    public readonly Vector2 YawLimit;
    public readonly Quaternion TargetBodyRotation;
    public readonly float CurrentMountingPointVerticalOffset;
    public readonly short MountingDirection;
    public readonly float TransitionTime;

    public readonly void Execute(FikaPlayer player)
    {
        switch (Command)
        {
            case EFT.MountingPacket.EMountingCommand.Enter:
                {
                    player.MovementContext.PlayerMountingPointData.SetData(new MountPointData(MountingPoint, MountDirection,
                        (EMountSideDirection)MountingDirection), default, default, default,
                        default, default, default, default, default);
                    player.MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset = CurrentMountingPointVerticalOffset;
                    player.MovementContext.EnterMountedState();
                }
                break;
            case EFT.MountingPacket.EMountingCommand.Exit:
                {
                    player.MovementContext.ExitMountedState();
                }
                break;
            case EFT.MountingPacket.EMountingCommand.Update:
                {
                    player.MovementContext.PlayerMountingPointData.CurrentMountingPointVerticalOffset = CurrentMountingPointVerticalOffset;
                }
                break;
            case EFT.MountingPacket.EMountingCommand.StartLeaving:
                {
                    if (player.MovementContext is ObservedMovementContext observedMovementContext)
                    {
                        observedMovementContext.ObservedStartExitingMountedState();
                    }
                }
                break;
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Command);
        if (Command == EFT.MountingPacket.EMountingCommand.Update)
        {
            writer.Put(CurrentMountingPointVerticalOffset);
        }
        if (Command is <= EFT.MountingPacket.EMountingCommand.Exit)
        {
            writer.Put(IsMounted);
        }
        if (Command == EFT.MountingPacket.EMountingCommand.Enter)
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
}
