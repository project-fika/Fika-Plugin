using Fika.Core.Coop.ObservedClasses.Snapshotting;
using Fika.Core.Coop.Players;
using LiteNetLib.Utils;
using UnityEngine;
using static BaseBallistic;

namespace Fika.Core.Networking
{
    public struct PlayerStatePacket : INetSerializable, ISnapshot
    {
        public int NetId;
        public Vector3 Position;
        public Vector2 Rotation;
        public Vector3 HeadRotation;
        public Vector2 MovementDirection;
        public EPlayerState State;
        public float Tilt;
        public int Step;
        public float CharacterMovementSpeed;
        public bool IsProne;
        public float PoseLevel;
        public bool IsSprinting;
        public BasePhysicalClass.PhysicalStateStruct Stamina;
        public int Blindfire;
        public float WeaponOverlap;
        public bool LeftStanceDisabled;
        public bool IsGrounded;

        public PlayerStatePacket(int netId)
        {
            NetId = netId;
        }

        // Snapshot
        public double RemoteTime { get; set; }
        public double LocalTime { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(Position);
            writer.Put(Rotation);
            writer.Put(HeadRotation);
            writer.Put(MovementDirection);
            writer.Put((byte)State);
            writer.Put(Tilt);
            writer.Put(Step);
            writer.Put(CharacterMovementSpeed);
            writer.Put(IsProne);
            writer.Put(PoseLevel);
            writer.Put(IsSprinting);
            writer.Put(Stamina);
            writer.Put(Blindfire);
            writer.Put(WeaponOverlap);
            writer.Put(LeftStanceDisabled);
            writer.Put(IsGrounded);
            writer.Put(RemoteTime);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Position = reader.GetVector3();
            Rotation = reader.GetVector2();
            HeadRotation = reader.GetVector3();
            MovementDirection = reader.GetVector2();
            State = (EPlayerState)reader.GetByte();
            Tilt = reader.GetFloat();
            Step = reader.GetInt();
            CharacterMovementSpeed = reader.GetFloat();
            IsProne = reader.GetBool();
            PoseLevel = reader.GetFloat();
            IsSprinting = reader.GetBool();
            Stamina = reader.GetPhysical();
            Blindfire = reader.GetInt();
            WeaponOverlap = reader.GetFloat();
            LeftStanceDisabled = reader.GetBool();
            IsGrounded = reader.GetBool();
            RemoteTime = reader.GetDouble();
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
            CharacterMovementSpeed = player.MovementContext.SmoothedCharacterMovementSpeed;
            IsProne = player.IsInPronePose;
            PoseLevel = player.PoseLevel;
            IsSprinting = player.MovementContext.IsSprintEnabled;
            Stamina = player.Physical.SerializationStruct;
            Blindfire = player.MovementContext.BlindFire;
            WeaponOverlap = player.ObservedOverlap;
            LeftStanceDisabled = player.LeftStanceDisabled;
            IsGrounded = player.MovementContext.IsGrounded;
            RemoteTime = NetworkTimeSync.NetworkTime;
        }
    }
}