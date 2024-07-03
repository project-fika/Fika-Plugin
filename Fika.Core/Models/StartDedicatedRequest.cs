using EFT;
using EFT.Bots;
using JsonType;
using System.Runtime.Serialization;

namespace Fika.Core.Models
{
    [DataContract]
    public struct StartDedicatedRequest
    {
        [DataMember(Name = "expectedNumberOfPlayers")]
        public int ExpectedNumPlayers { get; set; }

        [DataMember(Name = "time")]
        public EDateTime Time { get; set; }

        [DataMember(Name = "locationId")]
        public string LocationId { readonly get; set; }

        [DataMember(Name = "spawnPlace")]
        public EPlayersSpawnPlace SpawnPlace { readonly get; set; }

        [DataMember(Name = "metabolismDisabled")]
        public bool MetabolismDisabled { readonly get; set; }

        [DataMember(Name = "timeAndWeatherSettings")]
        public TimeAndWeatherSettings TimeAndWeatherSettings { readonly get; set; }

        [DataMember(Name = "botSettings")]
        public BotControllerSettings BotSettings { readonly get; set; }

        [DataMember(Name = "wavesSettings")]
        public WavesSettings WavesSettings { readonly get; set; }

        [DataMember(Name = "side")]
        public ESideType Side { readonly get; set; }
    }
}
