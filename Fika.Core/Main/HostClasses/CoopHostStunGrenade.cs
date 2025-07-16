namespace Fika.Core.Main.HostClasses
{
    public class CoopHostStunGrenade : StunGrenade
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
