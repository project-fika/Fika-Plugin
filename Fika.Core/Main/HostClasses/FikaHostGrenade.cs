using EFT;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.HostClasses;

public class FikaHostGrenade : Grenade
{
    public FikaHostGrenade(IntPtr pointer) : base(pointer)
    {
    }

    public override bool HasNetData
    {
        get
        {
            return true;
        }
    }
}
