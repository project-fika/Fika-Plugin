using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct BorderZoneEventPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.BorderZoneEvent;

    public BorderZoneEventPacket(string profileId, int zoneId)
    {
        ProfileId = profileId;
        ZoneId = zoneId;
    }

    public BorderZoneEventPacket(NetDataReader reader)
    {
        ProfileId = reader.GetString();
        ZoneId = reader.GetInt();
    }

    public readonly string ProfileId;
    public readonly int ZoneId;

    public readonly void Execute()
    {
        if (!Singleton<GameWorld>.Instantiated)
        {
            return;
        }

        var borderZones = Singleton<GameWorld>.Instance.BorderZones;
        if (borderZones == null || borderZones.Length == 0)
        {
            return;
        }

        foreach (var borderZone in borderZones)
        {
            if (borderZone.Id == ZoneId)
            {
                foreach (var iPlayer in Singleton<GameWorld>.Instance.RegisteredPlayers)
                {
                    if (iPlayer.ProfileId == ProfileId)
                    {
                        var playerBridge = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(ProfileId);
                        if (playerBridge != null)
                        {
                            borderZone.ProcessIncomingPacket(playerBridge);
                        }
                        break;
                    }
                }
            }
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ProfileId);
        writer.Put(ZoneId);
    }
}
