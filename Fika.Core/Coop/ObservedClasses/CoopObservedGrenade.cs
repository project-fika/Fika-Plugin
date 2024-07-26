using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses
{
    public class CoopObservedGrenade : Grenade
    {
        public override void ApplyNetPacket(GStruct128 packet)
        {
            base.ApplyNetPacket(packet);
        }
    }
}
