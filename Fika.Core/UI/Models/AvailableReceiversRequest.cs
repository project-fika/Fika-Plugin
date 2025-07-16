using System.Runtime.Serialization;

namespace Fika.Core.UI.Models
{
    [DataContract]
    public struct AvailableReceiversRequest(string id)
    {
        [DataMember(Name = "id")]
        public string Id = id;
    }
}