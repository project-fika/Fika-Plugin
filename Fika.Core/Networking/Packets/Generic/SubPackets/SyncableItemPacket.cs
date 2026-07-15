using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class SyncableItemPacket : IPoolSubPacket
{
    private SyncableItemPacket() { }

    public static SyncableItemPacket CreateInstance()
    {
        return new();
    }

    public static SyncableItemPacket FromValue(int netId, Turnable.EState state)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<SyncableItemPacket>(EGenericSubPacketType.SyncableItem);
        packet.NetId = netId;
        packet.SyncType = ESyncType.LampState;
        packet.LampStates = state;
        return packet;
    }

    public static SyncableItemPacket FromValue(int netId, Vector3 hitPoint)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<SyncableItemPacket>(EGenericSubPacketType.SyncableItem);
        packet.NetId = netId;
        packet.SyncType = ESyncType.WindowBreak;
        packet.HitPoint = hitPoint;
        return packet;
    }

    public int NetId;
    public ESyncType SyncType;
    public Turnable.EState LampStates;
    public Vector3 HitPoint;

    public void Execute(FikaPlayer player = null)
    {
        if (SyncType is ESyncType.LampState)
        {
            if (Singleton<GameWorld>.Instance is FikaClientGameWorld clientGameWorld)
            {
                if (!clientGameWorld.TurnableDict.TryGetValue(NetId, out var turnable))
                {
                    FikaGlobals.LogWarning($"Could not find 'Turnable' with Id [{NetId}]");
                    return;
                }

                if (turnable.LampState != LampStates)
                {
                    turnable.Switch(LampStates);
                }
            }
        }
        else
        {
            if (Singleton<GameWorld>.Instance.Windows.TryGetByKey(NetId, out var windowBreaker))
            {
                DamageInfoStruct damageInfoStruct = new()
                {
                    HitPoint = HitPoint
                };
                windowBreaker.MakeHit(in damageInfoStruct);
            }
            else
            {
                FikaGlobals.LogWarning($"Could not find 'WindowBreaker' with Id [{NetId}]");
            }
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        SyncType = reader.GetEnum<ESyncType>();
        if (SyncType is ESyncType.LampState)
        {
            LampStates = reader.GetEnum<Turnable.EState>();
        }
        else
        {
            HitPoint = reader.GetUnmanaged<Vector3>();
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.PutEnum(SyncType);
        if (SyncType is ESyncType.LampState)
        {
            writer.PutEnum(LampStates);
        }
        else
        {
            writer.PutUnmanaged(HitPoint);
        }
    }

    public void Dispose()
    {
        if (SyncType is ESyncType.LampState)
        {
            LampStates = default;
        }
        else
        {
            HitPoint = default;
        }
        SyncType = default;
    }

    public enum ESyncType
    {
        LampState,
        WindowBreak
    }
}
