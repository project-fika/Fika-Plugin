using System.Runtime.Serialization;
using EFT;
using EFT.Bots;
using Fika.Core.Main.Utils;
using JsonType;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models.Headless;

[DataContract]
public struct StartHeadlessRequest
{
    [JsonPropertyName("headlessSessionID")]
    public string HeadlessSessionID { get; set; }

    [JsonPropertyName("time")]
    public EDateTime Time { get; set; }

    [JsonPropertyName("locationId")]
    public string LocationId { readonly get; set; }

    [JsonPropertyName("spawnPlace")]
    public EPlayersSpawnPlace SpawnPlace { readonly get; set; }

    [JsonPropertyName("metabolismDisabled")]
    public bool MetabolismDisabled { readonly get; set; }

    [JsonPropertyName("timeAndWeatherSettings")]
    public TimeAndWeatherSettings TimeAndWeatherSettings { readonly get; set; }

    [JsonPropertyName("botSettings")]
    public BotControllerSettings BotSettings { readonly get; set; }

    [JsonPropertyName("wavesSettings")]
    public WavesSettings WavesSettings { readonly get; set; }

    [JsonPropertyName("side")]
    public ESideType Side { readonly get; set; }

    [JsonPropertyName("customRaidSettings")]
    public FikaCustomRaidSettings CustomRaidSettings { readonly get; set; }

    [JsonPropertyName("useEvent")]
    public bool UseEvent { readonly get; set; }
}
