using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
	public class FikaNetworkManagerDestroyedEvent : FikaEvent
	{
		public IFikaNetworkManager Manager { get; }

		public FikaNetworkManagerDestroyedEvent(IFikaNetworkManager server)
		{
			Manager = server;
		}
	}
}
