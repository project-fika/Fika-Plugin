using System.Collections.Generic;
using EFT;
using Fika.Core.Main.Components;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public sealed class ClientViewFilter : ViewFilter
{
    public ClientViewFilter(IntPtr pointer) : base(pointer)
    {
    }

    public ClientViewFilter() : base(Il2CppInjection.Allocate<ClientViewFilter>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public override HashSet<EBodyModelPart> AllowedParts
    {
        get
        {
            return [EBodyModelPart.Body, EBodyModelPart.Feet, EBodyModelPart.Head, EBodyModelPart.Hands];
        }
    }
}
