using System.Runtime.Serialization;
using Fika.Core.Main.Components;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct PlayerLeftRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    [JsonPropertyName("profileId")]
    public string ProfileId;

    public PlayerLeftRequest(string profileId)
    {
        ServerId = CoopHandler.GetServerId();
        ProfileId = profileId;
    }
}