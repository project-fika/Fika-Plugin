using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
    [DataContract]
    public struct AvailableHeadlessClientsRequest
    {
        [DataMember(Name = "headlessSessionID")]
        public string HeadlessSessionID { get; set; }
        [DataMember(Name = "alias")]
        public string Alias { get; set; }

        public AvailableHeadlessClientsRequest(string headlessSessionID, string alias)
        {
            HeadlessSessionID = headlessSessionID;
            Alias = alias;
        }
    }
}
