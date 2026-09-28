using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using Fika.Core.Main.ClientClasses;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct SyncableItemPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.SyncableItem;

    public SyncableItemPacket(int netId, Turnable.EState state)
    {
        NetId = netId;
        SyncType = ESyncType.LampState;
        LampStates = state;
    }

    public SyncableItemPacket(int netId, Vector3 hitPoint)
    {
        NetId = netId;
        SyncType = ESyncType.WindowBreak;
        HitPoint = hitPoint;
    }

    public SyncableItemPacket(NetDataReader reader)
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

    public readonly int NetId;
    public readonly ESyncType SyncType;
    public readonly Turnable.EState LampStates;
    public readonly Vector3 HitPoint;

    public readonly void Execute()
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
                DamageInfo damageInfoStruct = new()
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

    public readonly void Serialize(NetDataWriter writer)
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

    public enum ESyncType
    {
        LampState,
        WindowBreak
    }
}
