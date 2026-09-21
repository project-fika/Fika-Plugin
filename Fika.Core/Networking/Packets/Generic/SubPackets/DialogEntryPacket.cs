using System.Collections.Generic;
using Fika.Core.Main.Components;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct DialogEntryPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.DialogEntry;

    private DialogEntryPacket(EDialogEntryOperation operation, string pointId = null, int occupantId = 0, List<KeyValuePair<string, int>> occupants = null)
    {
        Operation = operation;
        PointId = pointId;
        OccupantId = occupantId;
        Occupants = occupants;
    }

    public DialogEntryPacket(NetDataReader reader)
    {
        Operation = reader.GetEnum<EDialogEntryOperation>();

        if (Operation is EDialogEntryOperation.FullState)
        {
            int amount = reader.GetUShort();
            Occupants = new(amount);
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

    public readonly EDialogEntryOperation Operation;
    public readonly string PointId;
    public readonly int OccupantId;
    public readonly List<KeyValuePair<string, int>> Occupants;

    public static DialogEntryPacket Claim(string pointId, int raidId)
    {
        return new(EDialogEntryOperation.Claim, pointId, raidId);
    }

    public static DialogEntryPacket Release(string pointId, int raidId)
    {
        return new(EDialogEntryOperation.Release, pointId, raidId);
    }

    public static DialogEntryPacket Occupancy(string pointId, int occupantId)
    {
        return new(EDialogEntryOperation.Occupancy, pointId, occupantId);
    }

    public static DialogEntryPacket RequestFullState()
    {
        return new(EDialogEntryOperation.RequestFullState);
    }

    public static DialogEntryPacket FullState(Dictionary<string, int> occupants)
    {
        return new(EDialogEntryOperation.FullState, occupants: [.. occupants]);
    }

    public readonly void Execute()
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

    public readonly void Serialize(NetDataWriter writer)
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
}

public enum EDialogEntryOperation : byte
{
    Claim,
    Release,
    Occupancy,
    FullState,
    RequestFullState
}
