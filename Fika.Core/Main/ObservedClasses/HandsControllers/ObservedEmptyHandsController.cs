// © 2025 Lacyway All Rights Reserved

using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal class ObservedEmptyHandsController : Player.EmptyHandsController
{
    private ObservedPlayer _observedPlayer;

    public static ObservedEmptyHandsController Create(ObservedPlayer observedPlayer)
    {
        var controller = smethod_6<ObservedEmptyHandsController>(observedPlayer);
        controller._observedPlayer = observedPlayer;
        return controller;
    }

    public override bool CanChangeCompassState(bool newState)
    {
        return false;
    }

    public override void OnCanUsePropChanged(bool canUse)
    {
        // Do nothing
    }

    public override void SetCompassState(bool active)
    {
        // Do nothing
    }

    public override void CompassStateHandler(bool isActive)
    {
        _observedPlayer.CreateObservedCompass();
        _objectInHandsAnimator.ShowCompass(isActive);
        _observedPlayer.SetPropVisibility(isActive);
    }
}
