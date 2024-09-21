using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct RaidSettingsRequest
	{
		[DataMember(Name = "serverId")]
		public string ServerId;

		public RaidSettingsRequest()
		{
			ServerId = CoopHandler.GetServerId();
		}
	}
}