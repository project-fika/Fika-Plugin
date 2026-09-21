using System;

namespace Fika.Core.Main.HostClasses;

public class FikaHostTearGasGrenade : TearGasGrenade
{
    public FikaHostTearGasGrenade(IntPtr pointer) : base(pointer)
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
