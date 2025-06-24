// © 2025 Lacyway All Rights Reserved

using EFT;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
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
            bool isZero = SetupDirection(deltaTime);
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
            if (!isZero)
            {
                //MovementContext.ApplyRotation(Quaternion.AngleAxis(MovementContext.Yaw, Vector3.up));
                method_0(deltaTime);
            }
            else
            {
                method_0(deltaTime);
                MovementContext.PlayerAnimatorEnableInert(false);
            }
            if (Bool_1)
            {
                MovementContext.EnableSprint(true);
                Bool_1 = false;
            }
            if (MovementContext.IsSprintEnabled && MovementContext.PoseLevel > 0.9f && MovementContext.SmoothedCharacterMovementSpeed >= 1f)
            {
                MovementContext.PlayerAnimatorEnableSprint(true, false);
            }

            if (Bool_2)
            {
                Float_5 = 0f;
            }
            else
            {
                Float_5 += deltaTime;
            }
            Bool_2 = false;
            if (Float_5 > Float_0)
            {
                Direction = Vector2.zero;
            }
        }

        private bool SetupDirection(float deltaTime)
        {
            /*Direction = method_7(Direction);
            MovementContext.MovementDirection = method_8(Direction);*/
            bool isZero = Direction.IsZero();
            MovementContext.MovementDirection = Direction;
            //Vector2 vector = Direction;//(isZero ? MovementContext.MovementDirection : Direction);
            method_3(GClass1907.ConvertToMovementDirection(Direction), deltaTime);
            return isZero;
        }

        public override void UpdatePosition(float deltaTime)
        {
            if (!MovementContext.IsGrounded)
            {
                MovementContext.PlayerAnimatorEnableFallingDown(true);
            }
        }

        public override void EnableSprint(bool enabled, bool isToggle = false)
        {
            MovementContext.EnableSprint(enabled);
        }
    }
}
