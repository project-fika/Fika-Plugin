using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models.Headless;

[DataContract]
public struct StartHeadlessResponse
{
    [JsonPropertyName("matchId")]
    public string MatchId { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }
}
