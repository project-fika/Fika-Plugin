using System.Runtime.Serialization;

namespace Fika.Core.UI.Models
{
    [DataContract]
    public struct AvailableReceiversRequest
    {
        [DataMember(Name = "id")]
        public string Id;

        public AvailableReceiversRequest(string id)
        {
            Id = id;
        }
    }
}