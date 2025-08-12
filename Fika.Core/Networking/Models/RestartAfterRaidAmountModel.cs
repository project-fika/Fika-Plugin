using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct RestartAfterRaidAmountModel
{
    [DataMember(Name = "amount")]
    public int Amount;

    public RestartAfterRaidAmountModel(int amount)
    {
        Amount = amount;
    }
}
