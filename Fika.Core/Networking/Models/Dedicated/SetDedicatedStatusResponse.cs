using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct SetDedicatedStatusResponse
	{
		[DataMember(Name = "sessionId")]
		public string SessionId { get; set; }

		[DataMember(Name = "status")]
		public DedicatedStatus Status { get; set; }
	}
}
