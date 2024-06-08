using System.Runtime.Serialization;

[DataContract]
public struct GetHostStunResponse
{
    [DataMember(Name = "requestType")]
    public string RequestType;

    [DataMember(Name = "clientId")]
    public string ClientId;

    [DataMember(Name = "StunIp")]
    public string StunIp;

    [DataMember(Name = "StunPort")]
    public int StunPort;

    public GetHostStunResponse(string clientId, string stunIp, int stunPort)
    {
        RequestType = GetType().Name;
        ClientId = clientId;
        StunIp = stunIp;
        StunPort = stunPort;
    }
}