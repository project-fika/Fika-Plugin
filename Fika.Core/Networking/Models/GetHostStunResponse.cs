using System.Runtime.Serialization;
using System.Text.Json.Serialization;

[DataContract]
public struct GetHostStunResponse
{
    [JsonPropertyName("requestType")]
    public string RequestType;

    [JsonPropertyName("sessionId")]
    public string SessionId;

    [JsonPropertyName("StunIp")]
    public string StunIp;

    [JsonPropertyName("StunPort")]
    public int StunPort;

    public GetHostStunResponse(string sessionId, string stunIp, int stunPort)
    {
        RequestType = GetType().Name;
        SessionId = sessionId;
        StunIp = stunIp;
        StunPort = stunPort;
    }
}