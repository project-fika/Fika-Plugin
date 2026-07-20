using EFT.InventoryLogic;
using Fika.Core.Main.ClientClasses.HandsControllers;
using Fika.Core.Main.Players;

namespace Fika.Core.Main.BotClasses;

public sealed class BotFirearmController : FikaClientFirearmController
{
    public override Vector3 WeaponDirection
    {
        get
        {
            return _fikaPlayer.LookDirection;
        }
    }

    public static BotFirearmController Create(FikaBot player, Weapon weapon)
    {
        var controller = CreateController<BotFirearmController>(player, weapon);
        controller._fikaPlayer = player;
        controller._packet = new()
        {
            NetId = player.NetId
        };
        return controller;
    }
}
