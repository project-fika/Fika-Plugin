using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public record DownloadProfileResponse
{
    [JsonPropertyName("profile")]
    public JsonElement Profile { get; set; }

    [JsonPropertyName("modData")]
    public Dictionary<string, string> ModData { get; set; }

    [JsonPropertyName("errmsg")]
    public string ErrorMessage { get; set; }
}
