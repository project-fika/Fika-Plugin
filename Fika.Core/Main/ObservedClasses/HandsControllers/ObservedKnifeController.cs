// © 2025 Lacyway All Rights Reserved

using EFT.InventoryLogic;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.ObservedClasses
{
    internal class ObservedKnifeController : EFT.Player.KnifeController
    {
        public static ObservedKnifeController Create(FikaPlayer player, KnifeComponent item)
        {
            ObservedKnifeController controller = smethod_9<ObservedKnifeController>(player, item);
            return controller;
        }
    }
}
