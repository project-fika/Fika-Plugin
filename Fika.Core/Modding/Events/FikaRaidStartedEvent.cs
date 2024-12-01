namespace Fika.Core.Modding.Events
{
	public class FikaRaidStartedEvent : FikaEvent
	{
		public bool IsServer;

		internal FikaRaidStartedEvent(bool isServer)
		{
			IsServer = isServer;
		}
	}
}
