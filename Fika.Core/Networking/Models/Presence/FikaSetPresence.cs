using System;
using System.Runtime.Serialization;
using static Fika.Core.UI.FikaUIGlobals;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models.Presence;

[DataContract]
public struct FikaSetPresence
{
    [JsonPropertyName("activity")]
    public EFikaPlayerPresence Presence;

    [JsonPropertyName("raidInformation")]
    public RaidInformation? RaidInformation;

    public FikaSetPresence(EFikaPlayerPresence presence)
    {
        Presence = presence;
    }

    [Obsolete("Currently not used, handled on server", true)]
    public FikaSetPresence(EFikaPlayerPresence presence, RaidInformation? raidInformation)
    {
        Presence = presence;
        RaidInformation = raidInformation;
    }
}
