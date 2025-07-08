using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Fika.Core.Networking.Http
{
    [DataContract]
    public struct SetSettingsResponse
    {
        [DataMember(Name = "success")]
        public bool Success;
    }
}
