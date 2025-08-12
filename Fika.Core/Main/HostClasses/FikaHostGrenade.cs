using EFT;

namespace Fika.Core.Main.HostClasses;

public class FikaHostGrenade : Grenade
{
    public override bool HasNetData
    {
        get
        {
            return true;
        }
    }
}
