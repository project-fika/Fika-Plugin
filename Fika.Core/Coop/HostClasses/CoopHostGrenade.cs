using EFT;

namespace Fika.Core.Coop.HostClasses
{
	public class CoopHostGrenade : Grenade
	{
		public override bool HasNetData
		{
			get
			{
				return true;
			}
		}
	}
}
