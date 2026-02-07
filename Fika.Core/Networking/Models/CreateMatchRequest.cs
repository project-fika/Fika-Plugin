using System;
using System.Runtime.Serialization;
using EFT;
using JsonType;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct CreateMatch(string raidCode, MongoID serverId, Guid serverGuid, string hostUsername, bool isSpectator,
    long timestamp, RaidSettings settings, uint crc32, ESideType side, EDateTime time)
{
    [DataMember(Name = "raidCode")]
    public string RaidCode = raidCode;

    [DataMember(Name = "serverId")]
    public MongoID ServerId = serverId;

    [DataMember(Name = "serverGuid")]
    public Guid ServerGuid = serverGuid;

    [DataMember(Name = "hostUsername")]
    public string HostUsername = hostUsername;

    [DataMember(Name = "timestamp")]
    public long Timestamp = timestamp;

    [DataMember(Name = "settings")]
    public RaidSettings Settings = settings;

    [DataMember(Name = "gameVersion")]
    public string GameVersion = FikaPlugin.EFTVersionMajor;

    [DataMember(Name = "crc32")]
    public uint Crc32 = crc32;

    [DataMember(Name = "side")]
    public ESideType Side = side;

    [DataMember(Name = "time")]
    public EDateTime Time = time;

    [DataMember(Name = "isSpectator")]
    public bool IsSpectator = isSpectator;
}