using Fika.Core.Coop.Players;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    public class ObservedSnapshotter(ObservedCoopPlayer observedPlayer) : Snapshotter<PlayerStatePacket>
    {
        private readonly ObservedCoopPlayer player = observedPlayer;

        public override void Interpolate(in PlayerStatePacket to, in PlayerStatePacket from, float ratio)
        {
            player.CurrentPlayerState.Rotation = new Vector2(Mathf.LerpAngle(from.Rotation.x, to.Rotation.x, ratio),
                Mathf.LerpUnclamped(from.Rotation.y, to.Rotation.y, ratio));
            player.CurrentPlayerState.HeadRotation = Vector3.LerpUnclamped(from.HeadRotation, to.HeadRotation, ratio);
            player.CurrentPlayerState.PoseLevel = from.PoseLevel + (to.PoseLevel - from.PoseLevel);
            player.CurrentPlayerState.Position = Vector3.LerpUnclamped(from.Position, to.Position, ratio);
            player.CurrentPlayerState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, ratio);
            player.CurrentPlayerState.MovementDirection = Vector2.Lerp(from.MovementDirection, to.MovementDirection, ratio);
            player.CurrentPlayerState.State = to.State;
            player.CurrentPlayerState.Tilt = Mathf.LerpUnclamped(from.Tilt, to.Tilt, ratio);
            player.CurrentPlayerState.Step = to.Step;
            player.CurrentPlayerState.CharacterMovementSpeed = to.CharacterMovementSpeed;
            player.CurrentPlayerState.IsProne = to.IsProne;
            player.CurrentPlayerState.PoseLevel = Mathf.LerpUnclamped(from.PoseLevel, to.PoseLevel, ratio);
            player.CurrentPlayerState.IsSprinting = to.IsSprinting;
            player.CurrentPlayerState.Stamina = to.Stamina;
            player.CurrentPlayerState.Blindfire = to.Blindfire;
            player.CurrentPlayerState.WeaponOverlap = Mathf.LerpUnclamped(from.WeaponOverlap, to.WeaponOverlap, ratio);
            player.CurrentPlayerState.LeftStanceDisabled = to.LeftStanceDisabled;
            player.CurrentPlayerState.IsGrounded = to.IsGrounded;
        }
    }
}
