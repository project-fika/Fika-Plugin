using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http;

[DataContract]
public struct StartHeadlessResponse
{
    [DataMember(Name = "matchId")]
    public string MatchId { get; set; }

    [DataMember(Name = "error")]
    public string Error { get; set; }
}
