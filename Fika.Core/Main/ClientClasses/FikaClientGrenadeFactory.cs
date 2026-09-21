using EFT;
using System;
using Il2CppInterop.Runtime.Injection;
using Fika.Core.Main.Utils;

namespace Fika.Core.Main.ClientClasses;

public class FikaClientGrenadeFactory : GrenadeFactory
{
    public FikaClientGrenadeFactory(IntPtr pointer) : base(pointer)
    {
    }

    public FikaClientGrenadeFactory() : base(Il2CppInjection.Allocate<FikaClientGrenadeFactory>())
    {
        ClassInjector.DerivedConstructorBody(this);
        ClassInjector.InvokeBaseConstructor<GrenadeFactory>(this);
    }

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

    public override TearGasGrenade AddTearGasGrenade(GameObject gameObject)
    {
        return gameObject.AddComponent<FikaClientTearGasGrenade>();
    }
}
