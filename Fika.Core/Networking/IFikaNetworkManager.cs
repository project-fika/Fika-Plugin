using Fika.Core.Coop.Components;

namespace Fika.Core.Networking
{
	public interface IFikaNetworkManager
	{
		public CoopHandler CoopHandler { get; set; }
	}
}
