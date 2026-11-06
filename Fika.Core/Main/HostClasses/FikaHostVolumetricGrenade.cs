using System;

namespace Fika.Core.Main.HostClasses;

public class FikaHostVolumetricGrenade : VolumetricGrenade
{
    public FikaHostVolumetricGrenade(IntPtr pointer) : base(pointer)
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
