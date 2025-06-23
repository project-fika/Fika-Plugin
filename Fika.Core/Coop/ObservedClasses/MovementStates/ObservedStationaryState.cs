using EFT;
using System;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedStationaryState(MovementContext movementContext) : StationaryStateClass(movementContext)
    {
        public override bool OutOfOperationRange
        {
            get
            {
                return false;
            }
        }

        public override void Enter(bool isFromSameState)
        {
            base.Enter(isFromSameState);
            StationaryWeapon = MovementContext.StationaryWeapon;
            StationaryWeapon.ObserverMagazineAmmoCount = StationaryWeapon.GetMagazineCount();
            if (OutOfOperationRange)
            {
                MovementContext.DropStationary(StationaryPacketStruct.EStationaryCommand.Denied);
                return;
            }
            Bool_2 = false;
            Vector2_0 = new Vector2(StationaryWeapon.Yaw, StationaryWeapon.Pitch);
            Float_5 = 0.5f;
            if (isFromSameState)
            {
                return;
            }
            Stage = EStationaryStage.In;
            MovementContext.StateLocksInventory = true;
            MovementContext.SetRotationLimit(MovementContext.StationaryWeapon.YawLimit, MovementContext.StationaryWeapon.PitchLimit);
            MovementContext.SetStationaryWeapon(new Action<Player.AbstractHandsController, Player.AbstractHandsController>(SetStationaryCallback));
            MovementContext.LeftStanceController.DisableLeftStanceAnimFromBodyAction();
        }

        public void SetStationaryCallback(Player.AbstractHandsController arg1, Player.AbstractHandsController newContoller)
        {
            Class1306 @class = new()
            {
                StationaryStateClass = this
            };
            MovementContext.SetStationaryStrategy();
            MovementContext.StateLocksInventory = false;
            Transform_0 = newContoller.HandsHierarchy.GetTransform(ECharacterWeaponBones.weapon);
            StationaryWeapon.SetPivots(newContoller.HandsHierarchy);
            @class.firearm = newContoller as Player.FirearmController;
            StationaryWeapon.Hide(MovementContext.IsAI);
            MovementContext.RotationAction = ((StationaryWeapon.Animation == EFT.Interactive.StationaryWeapon.EStationaryAnimationType.AGS_17)
                ? MovementContext.AGSRotationFunction : MovementContext.UtesRotationFunction);
            Stage = EStationaryStage.Main;
            MovementContext.OnHandsControllerChanged += method_3;
            MovementContext.HandsChangingEvent += method_2;
            Action_0 = new Action(@class.method_1);
        }
    }
}
