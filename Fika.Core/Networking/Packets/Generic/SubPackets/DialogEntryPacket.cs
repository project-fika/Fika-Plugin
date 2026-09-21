using System.Collections.Generic;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class DialogEntryPacket : IPoolSubPacket
{
    public EDialogEntryOperation Operation;
    public string PointId;
    public int OccupantId;
    public readonly List<KeyValuePair<string, int>> Occupants = [];

    private DialogEntryPacket() { }

    public static DialogEntryPacket CreateInstance()
    {
        return new DialogEntryPacket();
    }

    public static DialogEntryPacket Claim(string pointId, int raidId)
    {
        return FromPoint(EDialogEntryOperation.Claim, pointId, raidId);
    }

    public static DialogEntryPacket Release(string pointId, int raidId)
    {
        return FromPoint(EDialogEntryOperation.Release, pointId, raidId);
    }

    public static DialogEntryPacket Occupancy(string pointId, int occupantId)
    {
        return FromPoint(EDialogEntryOperation.Occupancy, pointId, occupantId);
    }

    public static DialogEntryPacket RequestFullState()
    {
        var packet = GetFromPool();
        packet.Operation = EDialogEntryOperation.RequestFullState;
        return packet;
    }

    public static DialogEntryPacket FullState(Dictionary<string, int> occupants)
    {
        var packet = GetFromPool();
        packet.Operation = EDialogEntryOperation.FullState;
        foreach (var entry in occupants)
        {
            packet.Occupants.Add(entry);
        }
        return packet;
    }

    private static DialogEntryPacket FromPoint(EDialogEntryOperation operation, string pointId, int occupantId)
    {
        var packet = GetFromPool();
        packet.Operation = operation;
        packet.PointId = pointId;
        packet.OccupantId = occupantId;
        return packet;
    }

    private static DialogEntryPacket GetFromPool()
    {
        return GenericSubPacketPoolManager.Instance.GetPacket<DialogEntryPacket>(EGenericSubPacketType.DialogEntry);
    }

    public void Execute(FikaPlayer player = null)
    {
        switch (Operation)
        {
            case EDialogEntryOperation.Claim:
                DialogEntryOccupancy.HandleClaimRequest(PointId, OccupantId);
                break;
            case EDialogEntryOperation.Release:
                DialogEntryOccupancy.HandleReleaseRequest(PointId, OccupantId);
                break;
            case EDialogEntryOperation.RequestFullState:
                DialogEntryOccupancy.HandleFullStateRequest();
                break;
            case EDialogEntryOperation.Occupancy:
                DialogEntryOccupancy.ApplyOccupancy(PointId, OccupantId);
                break;
            case EDialogEntryOperation.FullState:
                DialogEntryOccupancy.ApplyFullState(Occupants);
                break;
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(Operation);

        if (Operation is EDialogEntryOperation.FullState)
        {
            writer.Put((ushort)Occupants.Count);

            for (var i = 0; i < Occupants.Count; i++)
            {
                writer.Put(Occupants[i].Key);
                writer.Put(Occupants[i].Value);
            }

            return;
        }

        if (Operation is EDialogEntryOperation.RequestFullState)
        {
            return;
        }

        writer.Put(PointId);
        writer.Put(OccupantId);
    }

    public void Deserialize(NetDataReader reader)
    {
        Operation = reader.GetEnum<EDialogEntryOperation>();

        if (Operation is EDialogEntryOperation.FullState)
        {
            int amount = reader.GetUShort();
            
            for (var i = 0; i < amount; i++)
            {
                var pointId = reader.GetString();
                Occupants.Add(new KeyValuePair<string, int>(pointId, reader.GetInt()));
            }

            return;
        }

        if (Operation is EDialogEntryOperation.RequestFullState)
        {
            return;
        }

        PointId = reader.GetString();
        OccupantId = reader.GetInt();
    }

    public void Dispose()
    {
        Operation = default;
        PointId = null;
        OccupantId = 0;
        Occupants.Clear();
    }
}

public enum EDialogEntryOperation : byte
{
    Claim,
    Release,
    Occupancy,
    FullState,
    RequestFullState
}
