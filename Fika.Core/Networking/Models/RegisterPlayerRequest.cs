using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct RegisterPlayerRequest
{
    [JsonPropertyName("crc")]
    public int Crc;

    [JsonPropertyName("locationId")]
    public string LocationId;

    [JsonPropertyName("variantId")]
    public int VariantId;

    public RegisterPlayerRequest(int crc, string locationId, int variantId)
    {
        Crc = crc;
        LocationId = locationId;
        VariantId = variantId;
    }
}