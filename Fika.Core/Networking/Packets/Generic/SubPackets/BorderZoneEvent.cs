using Comfort.Common;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class BorderZoneEvent : IPoolSubPacket
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
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<BorderZoneEvent>(EGenericSubPacketType.BorderZone);
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

        var borderZones = Singleton<GameWorld>.Instance.BorderZones;
        if (borderZones == null || borderZones.Length == 0)
        {
            return;
        }

        foreach (var borderZone in borderZones)
        {
            if (borderZone.Id == ZoneId)
            {
                var players = Singleton<GameWorld>.Instance.RegisteredPlayers;
                foreach (var iPlayer in players)
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
