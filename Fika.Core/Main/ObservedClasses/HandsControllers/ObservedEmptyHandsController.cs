// © 2025 Lacyway All Rights Reserved

using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses
{
    internal class ObservedEmptyHandsController : Player.EmptyHandsController
    {
        public static ObservedEmptyHandsController Create(FikaPlayer player)
        {
            return smethod_6<ObservedEmptyHandsController>(player);
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
