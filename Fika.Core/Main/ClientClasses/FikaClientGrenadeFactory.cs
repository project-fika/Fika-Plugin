using EFT;

namespace Fika.Core.Main.ClientClasses;

public class FikaClientGrenadeFactory : GrenadeFactoryClass
{
    public override Grenade AddGrenade(GameObject gameObject)
    {
        return gameObject.AddComponent<FikaClientGrenade>();
    }

    public override SmokeGrenade AddSmokeGrenade(GameObject gameObject)
    {
        return gameObject.AddComponent<FikaClientSmokeGrenade>();
    }

    public override StunGrenade AddStunGrenade(GameObject gameObject)
    {
        return gameObject.AddComponent<FikaClientStunGrenade>();
    }
}
