// © 2025 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses.HandsControllers;

internal class ObservedKnifeController : EFT.Player.KnifeController
{
    private ObservedPlayer _observedPlayer;

    public static ObservedKnifeController Create(ObservedPlayer observerdPlayer, KnifeComponent item)
    {
        var controller = smethod_9<ObservedKnifeController>(observerdPlayer, item);
        controller._observedPlayer = observerdPlayer;
        return controller;
    }

    public override void CompassStateHandler(bool isActive)
    {
        _observedPlayer.CreateObservedCompass();
        _objectInHandsAnimator.ShowCompass(isActive);
    }
}
