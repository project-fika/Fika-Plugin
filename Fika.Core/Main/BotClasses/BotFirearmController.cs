using EFT.InventoryLogic;
using Fika.Core.Main.ClientClasses.HandsControllers;
using Fika.Core.Main.Players;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.BotClasses;

public sealed class BotFirearmController : FikaClientFirearmController
{
    public BotFirearmController(IntPtr pointer) : base(pointer)
    {
    }

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
        controller._isGrenadeLauncher = weapon.IsGrenadeLauncher;
        controller._packet = new()
        {
            NetId = player.NetId
        };
        return controller;
    }
}
