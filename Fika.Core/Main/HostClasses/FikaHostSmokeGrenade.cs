namespace Fika.Core.Main.HostClasses;

public class FikaHostSmokeGrenade : SmokeGrenade
{
    public override bool HasNetData
    {
        get
        {
            return true;
        }
    }
}
