using System.Runtime.Serialization;
using EFT;
using EFT.Bots;
using JsonType;

namespace Fika.Core.Networking.Models.Headless;

[DataContract]
public struct StartHeadlessRequest
{
    [DataMember(Name = "headlessSessionID")]
    public string HeadlessSessionID { get; set; }
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

    [DataMember(Name = "customWeather")]
    public bool CustomWeather { readonly get; set; }

    [DataMember(Name = "useEvent")]
    public bool UseEvent { readonly get; set; }
}
