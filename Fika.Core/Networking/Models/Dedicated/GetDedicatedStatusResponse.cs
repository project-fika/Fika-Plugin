using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct GetDedicatedStatusResponse(bool available)
	{
		[DataMember(Name = "available")]
		public bool Available = available;
	}
}
