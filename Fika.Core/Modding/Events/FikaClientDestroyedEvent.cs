using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
	public class FikaClientDestroyedEvent : FikaEvent
	{
		public FikaClient Client { get; }

		internal FikaClientDestroyedEvent(FikaClient client)
		{
			Client = client;
		}
	}
}
