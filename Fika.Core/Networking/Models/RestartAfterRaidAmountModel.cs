using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct RestartAfterRaidAmountModel
{
    [JsonPropertyName("amount")]
    public int Amount;

    public RestartAfterRaidAmountModel(int amount)
    {
        Amount = amount;
    }
}
