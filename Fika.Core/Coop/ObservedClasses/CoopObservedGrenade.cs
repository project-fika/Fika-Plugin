using EFT;

namespace Fika.Core.Coop.ObservedClasses
{
	public class CoopObservedGrenade : Grenade
	{
		public override void ApplyNetPacket(GrenadeDataPacketStruct packet)
		{
			base.ApplyNetPacket(packet);
		}
	}
}
