using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct RaidSettingsResponse(bool metabolismDisabled, string playersSpawnPlace)
    {
        [DataMember(Name = "metabolismDisabled")]
        public bool MetabolismDisabled = metabolismDisabled;
        [DataMember(Name = "playersSpawnPlace")]
        public string PlayersSpawnPlace = playersSpawnPlace;
    }
}