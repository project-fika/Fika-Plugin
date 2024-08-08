using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models.Dedicated
{
	[DataContract]
	public struct GetDedicatedStatusResponse(bool available)
	{
		[DataMember(Name = "available")]
		public bool Available = available;
	}
}
