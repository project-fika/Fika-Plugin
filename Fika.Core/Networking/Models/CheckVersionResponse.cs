using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct CheckVersionResponse(string version)
	{
		[DataMember(Name = "version")]
		public string Version = version;
	}
}