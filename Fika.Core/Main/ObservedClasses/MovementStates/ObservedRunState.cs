// © 2026 Lacyway All Rights Reserved

using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedRunState : MovePlayerState
{
    public ObservedRunState(MovementContext movementContext) : base(movementContext)
    {
        MovementContext = movementContext;
    }

    public override bool HasNoInputForLongTime()
    {
        return false;
    }

    public override void ManualAnimatorMoveUpdate(float deltaTime)
    {
        if (IsStateHasExit)
        {
            return;
        }
        SetupDirection(deltaTime);
        if (_resetSidestepValue)
        {
            MovementContext.SetSidestep(Mathf.Lerp(_initialSidestepValue, 0f, _resetSidestepTime / _resetSidestepDuration));
            _resetSidestepTime += deltaTime;
            if (_resetSidestepTime > _resetSidestepDuration)
            {
                MovementContext.SetSidestep(0f);
                _resetSidestepValue = false;
            }
        }
        UpdateRotation(deltaTime);
        if (_sprintPressedAndPostponed)
        {
            MovementContext.EnableSprint(true);
            _sprintPressedAndPostponed = false;
        }
        if (MovementContext.IsSprintEnabled)
        {
            MovementContext.PlayerAnimatorEnableSprint(true, false);
        }
    }

    private void SetupDirection(float deltaTime)
    {
        MovementContext.MovementDirection = Direction;
        SetSmoothDiscreteDirection(MovementDirectionExtension.ConvertToMovementDirection(Direction), deltaTime);
    }

    public override void UpdatePosition(float deltaTime)
    {
        // Do nothing
    }

    public override void EnableSprint(bool enabled, bool isToggle = false)
    {
        MovementContext.EnableSprint(enabled);
    }
}
