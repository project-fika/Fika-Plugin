using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.UI.Models;

[DataContract]
public struct AvailableReceiversRequest(string id)
{
    [JsonPropertyName("id")]
    public string Id = id;
}