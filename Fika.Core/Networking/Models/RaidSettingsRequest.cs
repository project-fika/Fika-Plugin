using System.Runtime.Serialization;
using Fika.Core.Main.Components;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct RaidSettingsRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    public RaidSettingsRequest()
    {
        ServerId = CoopHandler.GetServerId();
    }
}