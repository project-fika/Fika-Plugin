using System.Collections.Generic;
using EFT;
using Fika.Core.Main.Components;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ObservedClasses;

public sealed class ObservedViewFilter : ViewFilter
{
    public ObservedViewFilter(IntPtr pointer) : base(pointer)
    {
    }

    public ObservedViewFilter() : base(Il2CppInjection.Allocate<ObservedViewFilter>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public static readonly ObservedViewFilter Default = new();

    public override HashSet<EBodyModelPart> AllowedParts
    {
        get
        {
            return [EBodyModelPart.Body, EBodyModelPart.Feet, EBodyModelPart.Head];
        }
    }
}
