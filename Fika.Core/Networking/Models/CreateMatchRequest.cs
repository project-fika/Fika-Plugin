using System;
using System.Runtime.Serialization;
using EFT;
using Fika.Core.Main.Utils;
using JsonType;
using System.Text.Json.Serialization;

namespace Fika.Core.Networking.Models;

[DataContract]
public struct CreateMatch(string raidCode, MongoID serverId, Guid serverGuid, string hostUsername, bool isSpectator,
    long timestamp, RaidSettings settings, uint crc32, ESideType side, EDateTime time, FikaCustomRaidSettings customRaidSettings)
{
    [JsonPropertyName("raidCode")]
    public string RaidCode = raidCode;

    [JsonPropertyName("serverId")]
    public MongoID ServerId = serverId;

    [JsonPropertyName("serverGuid")]
    public Guid ServerGuid = serverGuid;

    [JsonPropertyName("hostUsername")]
    public string HostUsername = hostUsername;

    [JsonPropertyName("timestamp")]
    public long Timestamp = timestamp;

    [JsonPropertyName("settings")]
    public RaidSettings Settings = settings;

    [JsonPropertyName("gameVersion")]
    public string GameVersion = FikaPlugin.EFTVersionMajor;

    [JsonPropertyName("crc32")]
    public uint Crc32 = crc32;

    [JsonPropertyName("side")]
    public ESideType Side = side;

    [JsonPropertyName("time")]
    public EDateTime Time = time;

    [JsonPropertyName("isSpectator")]
    public bool IsSpectator = isSpectator;

    [JsonPropertyName("customRaidSettings")]
    public FikaCustomRaidSettings CustomRaidSettings = customRaidSettings;
}