using System;
using EFT;

namespace Fika.Core.Main.ObservedClasses.MovementStates;

public class ObservedStationaryState(MovementContext movementContext) : StationaryPlayerState(movementContext)
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
            MovementContext.DropStationary(StationaryWeaponPacket.EStationaryCommand.Denied);
            return;
        }
        _denied = false;
        _exitRotation = new Vector2(StationaryWeapon.Yaw, StationaryWeapon.Pitch);
        _outTime = 0.5f;
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
        CG_Spawned @class = new()
        {
            StationaryPlayerState = this
        };
        MovementContext.SetStationaryStrategy();
        MovementContext.StateLocksInventory = false;
        _weaponTransform = newContoller.HandsHierarchy.GetTransform(ECharacterWeaponBones.weapon);
        StationaryWeapon.SetPivots(newContoller.HandsHierarchy);
        @class.firearm = newContoller as Player.FirearmController;
        StationaryWeapon.Hide(MovementContext.IsAI);
        MovementContext.RotationAction = StationaryWeapon.Animation == EFT.Interactive.StationaryWeapon.EStationaryAnimationType.AGS_17
            ? MovementContext.AGSRotationFunction : MovementContext.UtesRotationFunction;
        Stage = EStationaryStage.Main;
        MovementContext.OnHandsControllerChanged += OnHandsControllerChanged;
        MovementContext.HandsChangingEvent += MovementContextOnHandsChangedEvent;
        _handsChangingEventUnsubscribe = new Action(@class.method_1);
    }
}
