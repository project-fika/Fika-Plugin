using EFT;
using UnityEngine;

namespace Fika.Core.Coop.HostClasses
{
    public class HostGrenadeFactory : GClass676
    {
        public override Grenade AddGrenade(GameObject gameObject)
        {
            return gameObject.AddComponent<CoopHostGrenade>();
        }
    }
}
