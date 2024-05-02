using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
{
    [DataContract]
    public struct SpawnPointResponse
    {
        [DataMember(Name = "spawnpoint")]
        public string SpawnPoint;

        public SpawnPointResponse(string spawnPoint)
        {
            SpawnPoint = spawnPoint;
        }
    }
}