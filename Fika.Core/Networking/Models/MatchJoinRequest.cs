using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct MatchJoinRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    [JsonPropertyName("profileId")]
    public string ProfileId;

    public MatchJoinRequest(string serverId, string profileId)
    {
        ServerId = serverId;
        ProfileId = profileId;
    }
}