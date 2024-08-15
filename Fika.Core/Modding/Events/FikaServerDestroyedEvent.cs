using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
	public class FikaServerDestroyedEvent : FikaEvent
	{
		public FikaServer Server { get; }

		internal FikaServerDestroyedEvent(FikaServer server)
		{
			Server = server;
		}
	}
}
