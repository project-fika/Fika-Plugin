using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct GetHostStunRequest
{
    [JsonPropertyName("requestType")]
    public string RequestType;

    [JsonPropertyName("sessionId")]
    public string SessionId;

    [JsonPropertyName("serverId")]
    public string ServerId;

    [JsonPropertyName("stunIp")]
    public string StunIp;

    [JsonPropertyName("stunPort")]
    public int StunPort;

    public GetHostStunRequest(string serverId, string sessionId, string stunIp, int stunPort)
    {
        RequestType = GetType().Name;
        SessionId = sessionId;
        ServerId = serverId;
        StunIp = stunIp;
        StunPort = stunPort;
    }
}