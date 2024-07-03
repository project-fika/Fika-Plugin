using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Models
{
    [DataContract]
    public struct SetDedicatedStatusResponse
    {
        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }
    }
}
