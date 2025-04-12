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
        public int AnimatorStateIndex;
        public float CharacterMovementSpeed;
        public bool IsProne;
        public float PoseLevel;
        public bool IsSprinting;
        public BasePhysicalClass.PhysicalStateStruct Stamina;
        public int Blindfire;
        public float WeaponOverlap;
        public bool LeftStanceDisabled;
        public bool IsGrounded;
        public bool HasGround;
        public ESurfaceSound SurfaceSound;

        public PlayerStatePacket(int netId)
        {
            NetId = netId;
        }

        /*public PlayerStatePacket(int netId, Vector3 position, Vector2 rotation, Vector2 headRotation, Vector2 movementDirection,
            EPlayerState state, float tilt, int step, int animatorStateIndex, float characterMovementSpeed, bool isProne,
            float poseLevel, bool isSprinting, BasePhysicalClass.PhysicalStateStruct stamina, int blindfire, float weaponOverlap,
            bool leftStanceDisabled, bool isGrounded, bool hasGround, ESurfaceSound surfaceSound, double remoteTime)
        {
            NetId = netId;
            Position = position;
            Rotation = rotation;
            HeadRotation = headRotation;
            MovementDirection = movementDirection;
            State = state;
            Tilt = tilt;
            Step = step;
            AnimatorStateIndex = animatorStateIndex;
            CharacterMovementSpeed = characterMovementSpeed;
            IsProne = isProne;
            PoseLevel = poseLevel;
            IsSprinting = isSprinting;
            Stamina = stamina;
            Blindfire = blindfire;
            WeaponOverlap = weaponOverlap;
            LeftStanceDisabled = leftStanceDisabled;
            IsGrounded = isGrounded;
            HasGround = hasGround;
            SurfaceSound = surfaceSound;
            RemoteTime = remoteTime;
        }*/

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
            writer.Put(AnimatorStateIndex);
            writer.Put(CharacterMovementSpeed);
            writer.Put(IsProne);
            writer.Put(PoseLevel);
            writer.Put(IsSprinting);
            writer.Put(Stamina);
            writer.Put(Blindfire);
            writer.Put(WeaponOverlap);
            writer.Put(LeftStanceDisabled);
            writer.Put(IsGrounded);
            writer.Put(HasGround);
            writer.Put((byte)SurfaceSound);
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
            AnimatorStateIndex = reader.GetInt();
            CharacterMovementSpeed = reader.GetFloat();
            IsProne = reader.GetBool();
            PoseLevel = reader.GetFloat();
            IsSprinting = reader.GetBool();
            Stamina = reader.GetPhysical();
            Blindfire = reader.GetInt();
            WeaponOverlap = reader.GetFloat();
            LeftStanceDisabled = reader.GetBool();
            IsGrounded = reader.GetBool();
            HasGround = reader.GetBool();
            SurfaceSound = (ESurfaceSound)reader.GetByte();
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
            AnimatorStateIndex = player.CurrentAnimatorStateIndex;
            CharacterMovementSpeed = player.MovementContext.SmoothedCharacterMovementSpeed;
            IsProne = player.IsInPronePose;
            PoseLevel = player.PoseLevel;
            IsSprinting = player.MovementContext.IsSprintEnabled;
            Stamina = player.Physical.SerializationStruct;
            Blindfire = player.MovementContext.BlindFire;
            WeaponOverlap = player.ObservedOverlap;
            LeftStanceDisabled = player.LeftStanceDisabled;
            IsGrounded = player.MovementContext.IsGrounded;
            HasGround = player.HasGround;
            SurfaceSound = player.CurrentSurface;
            RemoteTime = NetworkTimeSync.NetworkTime;
        }
    }
}