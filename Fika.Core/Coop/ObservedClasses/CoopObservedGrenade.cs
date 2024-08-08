using EFT;

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
