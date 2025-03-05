using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Networking.Models
{
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
}
