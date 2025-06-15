using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public class ObservedSnapshotter(ObservedCoopPlayer observedPlayer) : Snapshotter<PlayerStatePacket>
    {
        private readonly ObservedCoopPlayer _player = observedPlayer;

        public override void Interpolate(in PlayerStatePacket to, in PlayerStatePacket from, float ratio)
        {
            _player.CurrentPlayerState.Rotation = new Vector2(Mathf.LerpAngle(from.Rotation.x, to.Rotation.x, ratio),
                Mathf.Lerp(from.Rotation.y, to.Rotation.y, ratio));
            _player.CurrentPlayerState.HeadRotation = Vector3.LerpUnclamped(from.HeadRotation, to.HeadRotation, ratio);
            _player.CurrentPlayerState.PoseLevel = from.PoseLevel + (to.PoseLevel - from.PoseLevel);
            _player.CurrentPlayerState.Position = Vector3.LerpUnclamped(from.Position, to.Position, ratio);
            _player.CurrentPlayerState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, ratio);
            _player.CurrentPlayerState.MovementDirection = Vector2.Lerp(from.MovementDirection, to.MovementDirection, ratio);
            _player.CurrentPlayerState.State = to.State;
            _player.CurrentPlayerState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, ratio);
            _player.CurrentPlayerState.Step = to.Step;
            _player.CurrentPlayerState.CharacterMovementSpeed = to.CharacterMovementSpeed;
            _player.CurrentPlayerState.IsProne = to.IsProne;
            _player.CurrentPlayerState.PoseLevel = Mathf.LerpUnclamped(from.PoseLevel, to.PoseLevel, ratio);
            _player.CurrentPlayerState.IsSprinting = to.IsSprinting;
            _player.CurrentPlayerState.Stamina = to.Stamina;
            _player.CurrentPlayerState.Blindfire = to.Blindfire;
            _player.CurrentPlayerState.WeaponOverlap = Mathf.LerpUnclamped(from.WeaponOverlap, to.WeaponOverlap, ratio);
            _player.CurrentPlayerState.LeftStanceDisabled = to.LeftStanceDisabled;
            _player.CurrentPlayerState.IsGrounded = to.IsGrounded;
        }
    }
}
