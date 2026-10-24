using Comfort.Common;
using EFT;
using Fika.Core.Main.Components;
using Fika.Core.Main.Patches.Lighthouse;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct LighthouseAggressionPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.LighthouseAggression;

    public LighthouseAggressionPacket(int victimNetId, string aggressorProfileId)
    {
        VictimNetId = victimNetId;
        AggressorProfileId = aggressorProfileId;
    }

    public LighthouseAggressionPacket(NetDataReader reader)
    {
        VictimNetId = reader.GetInt();
        AggressorProfileId = reader.GetString();
    }

    public readonly int VictimNetId;
    public readonly string AggressorProfileId;

    public readonly void Execute()
    {
        var zone = LighthouseTraderZone_Patches.Zone;
        if (zone == null || !CoopHandler.TryGetCoopHandler(out var coopHandler))
        {
            return;
        }

        if (!coopHandler.Players.TryGetValue(VictimNetId, out var victim) || !zone.allowedPlayers.Contains(victim))
        {
            return;
        }

        var aggressor = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(AggressorProfileId);
        if (aggressor != null)
        {
            zone.SetAgressor(victim, aggressor);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(VictimNetId);
        writer.Put(AggressorProfileId);
    }
}
