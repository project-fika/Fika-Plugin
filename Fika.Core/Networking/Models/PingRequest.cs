using System.Runtime.Serialization;
using Fika.Core.Main.Utils;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct PingRequest
{
    [JsonPropertyName("serverId")]
    public string ServerId;

    public PingRequest()
    {
        ServerId = FikaBackendUtils.GroupId;
    }
}