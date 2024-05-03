using LiteNetLib.Utils;
using UnityEngine;
using static BaseBallistic;

namespace Fika.Core.Networking
{
    public struct PlayerStatePacket(int netId, Vector3 position, Vector2 rotation, Vector2 headRotation, Vector2 movementDirection,
        EPlayerState state, float tilt, int step, int animatorStateIndex, float characterMovementSpeed,
        bool isProne, float poseLevel, bool isSprinting, GClass681.GStruct35 stamina, int blindfire,
        float weaponOverlap, bool leftStanceDisabled, bool isGrounded, bool hasGround, ESurfaceSound surfaceSound, Vector3 surfaceNormal) : INetSerializable
    {
        public int NetId = netId;
        public Vector3 Position = position;
        public Vector2 Rotation = rotation;
        public Vector3 HeadRotation = headRotation;
        public Vector2 MovementDirection = movementDirection;
        public EPlayerState State = state;
        public float Tilt = tilt;
        public int Step = step;
        public int AnimatorStateIndex = animatorStateIndex;
        public float CharacterMovementSpeed = characterMovementSpeed;
        public bool IsProne = isProne;
        public float PoseLevel = poseLevel;
        public bool IsSprinting = isSprinting;
        public GClass681.GStruct35 Stamina = stamina;
        public int Blindfire = blindfire;
        public float WeaponOverlap = weaponOverlap;
        public bool LeftStanceDisabled = leftStanceDisabled;
        public bool IsGrounded = isGrounded;
        public bool HasGround = hasGround;
        public ESurfaceSound SurfaceSound = surfaceSound;
        public Vector3 SurfaceNormal = surfaceNormal;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(Position);
            writer.Put(Rotation);
            writer.Put(HeadRotation);
            writer.Put(MovementDirection);
            writer.Put((int)State);
            //writer.Put(GClass1089.ScaleFloatToByte(Tilt, -5f, 5f));
            writer.Put(Tilt);
            //writer.Put(GClass1089.ScaleIntToByte(Step, -1, 1));
            writer.Put(Step);
            writer.Put(AnimatorStateIndex);
            //writer.Put(GClass1089.ScaleFloatToByte(CharacterMovementSpeed, 0f, 1f));
            writer.Put(CharacterMovementSpeed);
            writer.Put(IsProne);
            //writer.Put(GClass1089.ScaleFloatToByte(PoseLevel, 0f, 1f));
            writer.Put(PoseLevel);
            writer.Put(IsSprinting);
            writer.Put(Stamina);
            writer.Put(Blindfire);
            writer.Put(WeaponOverlap);
            writer.Put(LeftStanceDisabled);
            writer.Put(IsGrounded);
            writer.Put(HasGround);
            writer.Put((int)SurfaceSound);
            writer.Put(SurfaceNormal);
        }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            Position = reader.GetVector3();
            Rotation = reader.GetVector2();
            HeadRotation = reader.GetVector3();
            MovementDirection = reader.GetVector2();
            State = (EPlayerState)reader.GetInt();
            Tilt = reader.GetFloat(); //GClass1089.ScaleByteToFloat(reader.GetByte(), -5f, 5f);
            Step = reader.GetInt(); //GClass1089.ScaleByteToInt(reader.GetByte(), -1, 1);
            AnimatorStateIndex = reader.GetInt();
            CharacterMovementSpeed = reader.GetFloat(); //GClass1089.ScaleByteToFloat(reader.GetByte(), 0f, 1f);
            IsProne = reader.GetBool();
            PoseLevel = reader.GetFloat(); //GClass1089.ScaleByteToFloat(reader.GetByte(), 0f, 1f);
            IsSprinting = reader.GetBool();
            Stamina = reader.GetPhysical();
            Blindfire = reader.GetInt();
            WeaponOverlap = reader.GetFloat();
            LeftStanceDisabled = reader.GetBool();
            IsGrounded = reader.GetBool();
            HasGround = reader.GetBool();
            SurfaceSound = (ESurfaceSound)reader.GetInt();
            SurfaceNormal = reader.GetVector3();
        }
    }
}