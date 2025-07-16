namespace Fika.Core.Main.HostClasses
{
    public class CoopHostSmokeGrenade : SmokeGrenade
    {
        public override bool HasNetData
        {
            get
            {
                return true;
            }
        }
    }
}
