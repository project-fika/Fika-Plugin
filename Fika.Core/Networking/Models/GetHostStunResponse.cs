using System.Runtime.Serialization;

[DataContract]
public struct GetHostStunResponse
{
    [DataMember(Name = "requestType")]
    public string RequestType;

    [DataMember(Name = "sessionId")]
    public string SessionId;

    [DataMember(Name = "StunIp")]
    public string StunIp;

    [DataMember(Name = "StunPort")]
    public int StunPort;

    public GetHostStunResponse(string sessionId, string stunIp, int stunPort)
    {
        RequestType = GetType().Name;
        SessionId = sessionId;
        StunIp = stunIp;
        StunPort = stunPort;
    }
}