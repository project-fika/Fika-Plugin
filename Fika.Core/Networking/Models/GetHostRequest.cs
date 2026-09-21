using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct GetHostRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    public GetHostRequest(string serverId)
    {
        ServerId = serverId;
    }
}