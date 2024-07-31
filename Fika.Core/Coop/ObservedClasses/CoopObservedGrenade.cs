using EFT;

namespace Fika.Core.Coop.ObservedClasses
{
    public class CoopObservedGrenade : Grenade
    {
        public override void ApplyNetPacket(GStruct129 packet)
        {
            base.ApplyNetPacket(packet);
        }
    }
}
