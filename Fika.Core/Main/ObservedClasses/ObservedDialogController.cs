using System;
using EFT;
using EFT.Dialogs;
using EFT.InventoryLogic;
using EFT.Quests;
using Fika.Core.Main.Utils;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ObservedClasses;

public class ObservedDialogController : BaseTraderDialogController
{
    public ObservedDialogController(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedDialogController(Profile profile, QuestController questController, InventoryController inventoryController) : base(Il2CppInjection.Allocate<ObservedDialogController>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<BaseTraderDialogController>(this, profile, questController, inventoryController);
    }
}
