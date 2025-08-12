// © 2025 Lacyway All Rights Reserved

using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedRunState : RunStateClass
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
        if (Bool_0)
        {
            return;
        }
        SetupDirection(deltaTime);
        if (Bool_3)
        {
            MovementContext.SetSidestep(Mathf.Lerp(Float_11, 0f, Float_12 / Float_13));
            Float_12 += deltaTime;
            if (Float_12 > Float_13)
            {
                MovementContext.SetSidestep(0f);
                Bool_3 = false;
            }
        }
        method_0(deltaTime);
        if (Bool_1)
        {
            MovementContext.EnableSprint(true);
            Bool_1 = false;
        }
        if (MovementContext.IsSprintEnabled)
        {
            MovementContext.PlayerAnimatorEnableSprint(true, false);
        }
    }

    private void SetupDirection(float deltaTime)
    {
        MovementContext.MovementDirection = Direction;
        method_3(GClass1909.ConvertToMovementDirection(Direction), deltaTime);
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
