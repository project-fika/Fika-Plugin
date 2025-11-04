using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public record DownloadProfileResponse
{
    [DataMember(Name = "profile")]
    public JObject Profile { get; set; }

    [DataMember(Name = "modData")]
    public Dictionary<string, string> ModData { get; set; }

    [DataMember(Name = "errmsg", EmitDefaultValue = true, IsRequired = false)]
    public string ErrorMessage { get; set; }
}
