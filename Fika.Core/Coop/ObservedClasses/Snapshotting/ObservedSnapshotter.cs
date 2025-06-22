using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public class ObservedSnapshotter(ObservedCoopPlayer observedPlayer) : Snapshotter<PlayerStatePacket>
    {
        private readonly ObservedCoopPlayer _player = observedPlayer;
        private readonly float _deadZone = 0.05f * 0.05f;

        public override void Interpolate(in PlayerStatePacket to, in PlayerStatePacket from, float ratio)
        {
            ObservedState currentState = _player.CurrentPlayerState;

            currentState.Rotation = new Vector2(
                Mathf.LerpAngle(from.Rotation.x, to.Rotation.x, ratio),
                Mathf.LerpUnclamped(from.Rotation.y, to.Rotation.y, ratio)
            );

            currentState.HeadRotation = Vector3.LerpUnclamped(from.HeadRotation, to.HeadRotation, ratio);
            currentState.Position = Vector3.LerpUnclamped(from.Position, to.Position, ratio);

            Vector2 movDir = Vector2.LerpUnclamped(from.MovementDirection, to.MovementDirection, ratio);
            currentState.MovementDirection = movDir.sqrMagnitude < _deadZone ? Vector2.zero : movDir.normalized;

            currentState.State = to.State;
            currentState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, ratio);
            currentState.Step = to.Step;
            currentState.MovementSpeed = Mathf.LerpUnclamped(from.MovementSpeed, to.MovementSpeed, ratio);
            currentState.SprintSpeed = Mathf.LerpUnclamped(from.SprintSpeed, to.SprintSpeed, ratio);
            currentState.IsProne = to.IsProne;
            currentState.PoseLevel = Mathf.LerpUnclamped(from.PoseLevel, to.PoseLevel, ratio);
            currentState.IsSprinting = to.IsSprinting;
            currentState.Stamina = to.Physical;
            currentState.Blindfire = to.Blindfire;
            currentState.WeaponOverlap = Mathf.LerpUnclamped(from.WeaponOverlap, to.WeaponOverlap, ratio);
            currentState.LeftStanceDisabled = to.LeftStanceDisabled;
            currentState.IsGrounded = to.IsGrounded;
        }
    }
}
