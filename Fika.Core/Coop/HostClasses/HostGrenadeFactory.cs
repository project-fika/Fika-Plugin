using EFT;
using UnityEngine;

namespace Fika.Core.Coop.HostClasses
{
	public class HostGrenadeFactory : GrenadeFactoryClass
	{
		public override Grenade AddGrenade(GameObject gameObject)
		{
			return gameObject.AddComponent<CoopHostGrenade>();
		}

		public override SmokeGrenade AddSmokeGrenade(GameObject gameObject)
		{
			return gameObject.AddComponent<CoopHostSmokeGrenade>();
		}

		public override StunGrenade AddStunGrenade(GameObject gameObject)
		{
			return gameObject.AddComponent<CoopHostStunGrenade>();
		}
	}
}
