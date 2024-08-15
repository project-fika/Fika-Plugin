using Fika.Core.Networking;

namespace Fika.Core.Modding.Events
{
	public class FikaClientCreatedEvent : FikaEvent
	{
		public FikaClient Client { get; }

		internal FikaClientCreatedEvent(FikaClient client)
		{
			this.Client = client;
		}
	}
}
