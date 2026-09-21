using System.Runtime.Serialization;
using Fika.Core.Main.Components;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct PlayerSpawnRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    [JsonPropertyName("profileId")]
    public string ProfileId;

    [JsonPropertyName("groupId")]
    public string GroupId;

    public PlayerSpawnRequest(string profileId, string groupId)
    {
        ServerId = CoopHandler.GetServerId();
        ProfileId = profileId;
        GroupId = groupId;
    }
}