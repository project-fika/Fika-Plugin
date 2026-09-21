using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models.Admin;

[DataContract]
public struct SetSettingsRequest
{
    [JsonPropertyName("friendlyFire")]
    public bool FriendlyFire;

    [JsonPropertyName("freeCam")]
    public bool FreeCam;

    [JsonPropertyName("spectateFreeCam")]
    public bool SpectateFreeCam;

    [JsonPropertyName("sharedQuestProgression")]
    public bool SharedQuestProgression;

    [JsonPropertyName("averageLevel")]
    public bool AverageLevel;

    public SetSettingsRequest(bool friendlyFire, bool freeCam, bool spectateFreeCam, bool sharedQuestProgression, bool averageLevel)
    {
        FriendlyFire = friendlyFire;
        FreeCam = freeCam;
        SpectateFreeCam = spectateFreeCam;
        SharedQuestProgression = sharedQuestProgression;
        AverageLevel = averageLevel;
    }
}
