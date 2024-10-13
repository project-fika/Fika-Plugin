using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct AddPlayerRequest
	{
		[DataMember(Name = "serverId")]
		public string ServerId;

		[DataMember(Name = "profileId")]
		public string ProfileId;

		[DataMember(Name = "isSpectator")]
		public bool IsSpectator;

		public AddPlayerRequest(string serverId, string profileId, bool isSpectator)
		{
			ServerId = serverId;
			ProfileId = profileId;
			IsSpectator = isSpectator;
		}
	}
}