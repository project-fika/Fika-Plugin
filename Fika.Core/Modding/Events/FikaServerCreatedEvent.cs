using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
	public class FikaServerCreatedEvent : FikaEvent
	{
		public FikaServer Server { get; }

		internal FikaServerCreatedEvent(FikaServer server)
		{
			Server = server;
		}
	}
}
