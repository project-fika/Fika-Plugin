using EFT.InventoryLogic;
using Fika.Core.Main.ClientClasses.HandsControllers;
using Fika.Core.Main.Players;
using UnityEngine;

namespace Fika.Core.Main.BotClasses
{
    public class BotFirearmController : FikaClientFirearmController
    {
        public override Vector3 WeaponDirection
        {
            get
            {
                return _coopPlayer.LookDirection;
            }
        }

        public static BotFirearmController Create(FikaBot player, Weapon weapon)
        {
            BotFirearmController controller = smethod_6<BotFirearmController>(player, weapon);
            controller._coopPlayer = player;
            return controller;
        }
    }
}
