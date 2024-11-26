using Fika.Core.Coop.Utils;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct PingRequest
	{
		[DataMember(Name = "serverId")]
		public string ServerId;

		public PingRequest()
		{
			ServerId = FikaBackendUtils.GroupId;
		}
	}
}