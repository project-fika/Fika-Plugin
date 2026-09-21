using EFT;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.HostClasses;

public class HostGrenadeFactory : GrenadeFactory
{
    public HostGrenadeFactory(IntPtr pointer) : base(pointer)
    {
    }

    public HostGrenadeFactory() : base(Il2CppInjection.Allocate<HostGrenadeFactory>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<GrenadeFactory>(this);
    }

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

    public override TearGasGrenade AddTearGasGrenade(GameObject gameObject)
    {
        return gameObject.AddComponent<FikaHostTearGasGrenade>();
    }
}
