using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct SetDedicatedStatusRequest
	{
		[DataMember(Name = "sessionId")]
		public string SessionId { get; set; }

		[DataMember(Name = "status")]
		public DedicatedStatus Status { get; set; }

		public SetDedicatedStatusRequest(string sessionId, DedicatedStatus status)
		{
			SessionId = sessionId;
			Status = status;
		}
	}
}
