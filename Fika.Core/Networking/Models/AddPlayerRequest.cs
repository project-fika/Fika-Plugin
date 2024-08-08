using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
	[DataContract]
	public struct AddPlayerRequest
	{
		[DataMember(Name = "serverId")]
		public string ServerId;

		[DataMember(Name = "profileId")]
		public string ProfileId;

		public AddPlayerRequest(string serverId, string profileId)
		{
			ServerId = serverId;
			ProfileId = profileId;
		}
	}
}