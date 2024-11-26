using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct MatchJoinResponse
	{
		[DataMember(Name = "gameVersion")]
		public string GameVersion;

		[DataMember(Name = "fikaVersion")]
		public string FikaVersion;

		public MatchJoinResponse(string gameVersion, string fikaVersion)
		{
			GameVersion = gameVersion;
			FikaVersion = fikaVersion;
		}
	}
}