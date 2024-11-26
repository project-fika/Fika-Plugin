using Fika.Core.Coop.ObservedClasses.Snapshotting;
using LiteNetLib.Utils;
using UnityEngine;
using static BaseBallistic;

namespace Fika.Core.Networking
{
	public struct PlayerStatePacket(int netId, Vector3 position, Vector2 rotation, Vector2 headRotation, Vector2 movementDirection,
		EPlayerState state, float tilt, int step, int animatorStateIndex, float characterMovementSpeed, bool isProne,
		float poseLevel, bool isSprinting, BasePhysicalClass.GStruct36 stamina, int blindfire, float weaponOverlap,
		bool leftStanceDisabled, bool isGrounded, bool hasGround, ESurfaceSound surfaceSound, double remoteTime) : INetSerializable, ISnapshot
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
		public BasePhysicalClass.GStruct36 Stamina = stamina;
		public int Blindfire = blindfire;
		public float WeaponOverlap = weaponOverlap;
		public bool LeftStanceDisabled = leftStanceDisabled;
		public bool IsGrounded = isGrounded;
		public bool HasGround = hasGround;
		public ESurfaceSound SurfaceSound = surfaceSound;

		// Snapshot
		public double RemoteTime { get; set; } = remoteTime;
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
	}
}