using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
	public class FikaNetworkManagerCreatedEvent : FikaEvent
	{
		public IFikaNetworkManager Manager { get; }

		internal FikaNetworkManagerCreatedEvent(IFikaNetworkManager manager)
		{
			Manager = manager;
		}
	}
}
