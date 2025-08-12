using System.Runtime.Serialization;

namespace Fika.Core.Networking.Models.Admin;

[DataContract]
public struct SetSettingsRequest
{
    [DataMember(Name = "friendlyFire")]
    public bool FriendlyFire;

    [DataMember(Name = "freeCam")]
    public bool FreeCam;

    [DataMember(Name = "spectateFreeCam")]
    public bool SpectateFreeCam;

    [DataMember(Name = "sharedQuestProgression")]
    public bool SharedQuestProgression;

    [DataMember(Name = "averageLevel")]
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
