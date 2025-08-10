using EFT;

namespace Fika.Core.Main.HostClasses
{
    public class HostGrenadeFactory : GrenadeFactoryClass
    {
        public override Grenade AddGrenade(GameObject gameObject)
        {
            return gameObject.AddComponent<FikaHostGrenade>();
        }

        public override SmokeGrenade AddSmokeGrenade(GameObject gameObject)
        {
            return gameObject.AddComponent<FikaHostSmokeGrenade>();
        }

        public override StunGrenade AddStunGrenade(GameObject gameObject)
        {
            return gameObject.AddComponent<FikaHostStunGrenade>();
        }
    }
}
