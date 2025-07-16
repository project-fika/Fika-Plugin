using EFT;

namespace Fika.Core.Main.ObservedClasses
{
    public class FikaObservedGrenade : Grenade
    {
        public override void ApplyNetPacket(GrenadeDataPacketStruct packet)
        {
            base.ApplyNetPacket(packet);
        }
    }
}
