namespace Fika.Core.Coop.HostClasses
{
	public class CoopHostSmokeGrenade : SmokeGrenade
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
