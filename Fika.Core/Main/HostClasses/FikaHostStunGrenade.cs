namespace Fika.Core.Main.HostClasses
{
    public class FikaHostStunGrenade : StunGrenade
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
