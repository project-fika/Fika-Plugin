using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models.Headless;

[DataContract]
public struct AvailableHeadlessClientsRequest
{
    [JsonPropertyName("headlessSessionID")]
    public string HeadlessSessionID { get; set; }
    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    public AvailableHeadlessClientsRequest(string headlessSessionID, string alias)
    {
        HeadlessSessionID = headlessSessionID;
        Alias = alias;
    }
}
