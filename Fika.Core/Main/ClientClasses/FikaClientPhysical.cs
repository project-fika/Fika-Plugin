using EFT;
using Fika.Core.Main.Utils;
using System;
using Il2CppInterop.Runtime.Injection;

namespace Fika.Core.Main.ClientClasses;

public class FikaClientPhysical : Physical
{
    public FikaClientPhysical(IntPtr pointer) : base(pointer)
    {
    }

    public FikaClientPhysical() : base(Il2CppInjection.Allocate<FikaClientPhysical>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<Physical>(this);
    }

    public override void Init(IPlayer player)
    {
        base.Init(player);

        var settings = FikaBackendUtils.CustomRaidSettings;
        if (settings.DisableArmStamina)
        {
            HandsStamina.ForceMode = true;
        }
        if (settings.DisableLegStamina)
        {
            Stamina.ForceMode = true;
        }
        if (settings.DisableOverload)
        {
            EncumberDisabled = true;
        }
    }
}
