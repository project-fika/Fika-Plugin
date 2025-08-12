namespace Fika.Core.Main.ObservedClasses.Snapshotting;

internal class ObservedState
{
    internal ObservedState(Vector3 position, Vector2 rotation)
    {
        Position = position;
        Rotation = rotation;
        PoseLevel = 1f;
        IsGrounded = true;
    }

    public bool ShouldUpdate;
    public Vector3 Position;
    public Vector2 Rotation;
    public Vector3 HeadRotation;
    public Vector2 MovementDirection;
    public EPlayerState State;
    public float Tilt;
    public int Step;
    public float MovementSpeed;
    public float SprintSpeed;
    public bool IsProne;
    public float PoseLevel;
    public bool IsSprinting;
    public BasePhysicalClass.PhysicalStateStruct Stamina;
    public int Blindfire;
    public float WeaponOverlap;
    public bool LeftStanceDisabled;
    public bool IsGrounded;
    public bool IsMoving;
}
