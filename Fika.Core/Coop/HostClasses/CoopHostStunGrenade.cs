namespace Fika.Core.Coop.HostClasses
{
	public class CoopHostStunGrenade : StunGrenade
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
