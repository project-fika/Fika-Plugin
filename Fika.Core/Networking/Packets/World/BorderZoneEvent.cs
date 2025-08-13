using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Generic;
using Fika.Core.Networking.Pooling;
using System.Collections.Generic;

namespace Fika.Core.Networking.Packets.World;

public class BorderZoneEvent : IPoolSubPacket
{
    public string ProfileId;
    public int ZoneId;

    private BorderZoneEvent() { }

    public static BorderZoneEvent CreateInstance()
    {
        return new BorderZoneEvent();
    }

    public static BorderZoneEvent FromValue(string profileId, int zoneId)
    {
        BorderZoneEvent packet = GenericSubPacketPoolManager.Instance.GetPacket<BorderZoneEvent>(EGenericSubPacketType.BorderZone);
        packet.ProfileId = profileId;
        packet.ZoneId = zoneId;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (!Singleton<GameWorld>.Instantiated)
        {
            return;
        }

        BorderZone[] borderZones = Singleton<GameWorld>.Instance.BorderZones;
        if (borderZones == null || borderZones.Length == 0)
        {
            return;
        }

        foreach (BorderZone borderZone in borderZones)
        {
            if (borderZone.Id == ZoneId)
            {
                List<IPlayer> players = Singleton<GameWorld>.Instance.RegisteredPlayers;
                foreach (IPlayer iPlayer in players)
                {
                    if (iPlayer.ProfileId == ProfileId)
                    {
                        IPlayerOwner playerBridge = Singleton<GameWorld>.Instance.GetAlivePlayerBridgeByProfileID(ProfileId);
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

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ProfileId);
        writer.Put(ZoneId);
    }

    public void Deserialize(NetDataReader reader)
    {
        ProfileId = reader.GetString();
        ZoneId = reader.GetInt();
    }

    public void Dispose()
    {
        ProfileId = null;
        ZoneId = 0;
    }
}
