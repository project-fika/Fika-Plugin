using EFT;

namespace Fika.Core.Main.HostClasses
{
    public class CoopHostGrenade : Grenade
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
