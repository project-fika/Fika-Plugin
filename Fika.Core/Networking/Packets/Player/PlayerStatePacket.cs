using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Players;
using LiteNetLib.Utils;
using UnityEngine;

namespace Fika.Core.Networking
{
    public struct PlayerStatePacket : INetSerializable, ISnapshot
    {
        public double RemoteTime { get; set; }
        public double LocalTime { get; set; }

        public Vector3 Position;
        public Vector3 HeadRotation;
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

            writer.Put(NetId);
            writer.Put(Step);
            writer.Put(Blindfire);
            writer.Put((byte)State);

            writer.Put(Position);
            writer.Put(HeadRotation);
            writer.Put(Rotation);
            writer.Put(MovementDirection);

            writer.Put(Tilt);
            writer.Put(MovementSpeed);
            writer.Put(SprintSpeed);
            writer.Put(PoseLevel);
            writer.Put(WeaponOverlap);

            writer.PutPhysical(Physical);
        }

        public void Deserialize(NetDataReader reader)
        {
            byte boolFlags = reader.GetByte();
            IsProne = (boolFlags & (1 << 0)) != 0;
            IsSprinting = (boolFlags & (1 << 1)) != 0;
            LeftStanceDisabled = (boolFlags & (1 << 2)) != 0;
            IsGrounded = (boolFlags & (1 << 3)) != 0;

            RemoteTime = reader.GetDouble();
            LocalTime = reader.GetDouble();

            NetId = reader.GetInt();
            Step = reader.GetInt();
            Blindfire = reader.GetInt();
            State = (EPlayerState)reader.GetByte();

            Position = reader.GetVector3();
            HeadRotation = reader.GetVector3();
            Rotation = reader.GetVector2();
            MovementDirection = reader.GetVector2();

            Tilt = reader.GetFloat();
            MovementSpeed = reader.GetFloat();
            SprintSpeed = reader.GetFloat();
            PoseLevel = reader.GetFloat();
            WeaponOverlap = reader.GetFloat();

            Physical = reader.GetPhysical();
        }

        public void UpdateData(CoopPlayer player, bool isMoving)
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
}