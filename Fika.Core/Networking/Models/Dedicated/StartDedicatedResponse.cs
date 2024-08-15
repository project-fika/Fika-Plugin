using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models.Dedicated
{
	[DataContract]
	public struct StartDedicatedResponse
	{
		[DataMember(Name = "matchId")]
		public string MatchId { get; set; }

		[DataMember(Name = "error")]
		public string Error { get; set; }
	}
}
