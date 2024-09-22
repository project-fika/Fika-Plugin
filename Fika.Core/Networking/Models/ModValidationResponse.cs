using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct ModValidationResponse
	{
		[DataMember(Name = "forbidden")]
		public string[] Forbidden;

		[DataMember(Name = "missingRequired")]
		public string[] MissingRequired;

		[DataMember(Name = "hashMismatch")]
		public string[] HashMismatch;
	}
}