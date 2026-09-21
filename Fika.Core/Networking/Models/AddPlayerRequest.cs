using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct AddPlayerRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    [JsonPropertyName("profileId")]
    public string ProfileId;

    [JsonPropertyName("isSpectator")]
    public bool IsSpectator;

    public AddPlayerRequest(string serverId, string profileId, bool isSpectator)
    {
        ServerId = serverId;
        ProfileId = profileId;
        IsSpectator = isSpectator;
    }
}