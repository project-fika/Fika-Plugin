// © 2025 Lacyway All Rights Reserved

using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses
{
    internal class CoopObservedEmptyHandsController : EFT.Player.EmptyHandsController
    {
        public static CoopObservedEmptyHandsController Create(FikaPlayer player)
        {
            return smethod_6<CoopObservedEmptyHandsController>(player);
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
    }
}
