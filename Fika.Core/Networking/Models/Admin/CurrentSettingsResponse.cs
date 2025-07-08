using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Fika.Core.Networking.Http
{
    [DataContract]
    public struct CurrentSettingsResponse
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

        public CurrentSettingsResponse(bool friendlyFire, bool freeCam, bool spectateFreeCam, bool sharedQuestProgression, bool averageLevel)
        {
            FriendlyFire = friendlyFire;
            FreeCam = freeCam;
            SpectateFreeCam = spectateFreeCam;
            SharedQuestProgression = sharedQuestProgression;
            AverageLevel = averageLevel;
        }
    }
}
