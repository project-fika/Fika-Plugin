using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public class MuffledState : IPoolSubPacket
{
    public int NetId;
    public bool Muffled;

    private MuffledState() { }

    public static MuffledState CreateInstance()
    {
        return new MuffledState();
    }

    public static MuffledState FromValue(int netId, bool muffled)
    {
        MuffledState packet = GenericSubPacketPoolManager.Instance.GetPacket<MuffledState>(EGenericSubPacketType.MuffledState);
        packet.NetId = netId;
        packet.Muffled = muffled;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        if (CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
        {
            if (coopHandler.Players.TryGetValue(NetId, out FikaPlayer fikaPlayer) && fikaPlayer is ObservedPlayer observed)
            {
                observed.SetMuffledState(Muffled);
                return;
            }

            FikaGlobals.LogError($"MuffledState: Could not find player with id {NetId} or they were not observed!");
            return;
        }

        FikaGlobals.LogWarning($"MuffledState: Could not get CoopHandler!");
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Muffled);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Muffled = reader.GetBool();
    }

    public void Dispose()
    {
        NetId = 0;
        Muffled = false;
    }
}
