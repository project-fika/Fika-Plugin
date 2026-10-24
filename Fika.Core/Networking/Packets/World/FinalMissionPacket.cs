namespace Fika.Core.Networking.Packets.World;

public struct FinalMissionPacket : INetSerializable
{
    public EType Type;
    public int NetId;
    public int Seats;
    public string CutsceneId;
    public string ContainerId;
    public byte[] Items;

    public void Deserialize(NetDataReader reader)
    {
        Type = (EType)reader.GetByte();
        switch (Type)
        {
            case EType.Stow:
                NetId = reader.GetInt();
                Items = reader.GetByteArray();
                break;
            case EType.Stowed:
                NetId = reader.GetInt();
                ContainerId = reader.GetString();
                Items = reader.GetByteArray();
                break;
            case EType.BadEnd:
                NetId = reader.GetInt();
                break;
            case EType.ZoneArmed:
                Seats = reader.GetInt();
                break;
            case EType.Evacuate:
                NetId = reader.GetInt();
                CutsceneId = reader.GetString();
                break;
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)Type);
        switch (Type)
        {
            case EType.Stow:
                writer.Put(NetId);
                writer.PutByteArray(Items);
                break;
            case EType.Stowed:
                writer.Put(NetId);
                writer.Put(ContainerId ?? string.Empty);
                writer.PutByteArray(Items);
                break;
            case EType.BadEnd:
                writer.Put(NetId);
                break;
            case EType.ZoneArmed:
                writer.Put(Seats);
                break;
            case EType.Evacuate:
                writer.Put(NetId);
                writer.Put(CutsceneId ?? string.Empty);
                break;
        }
    }

    public enum EType : byte
    {
        // Client to host, the gear taken off this player before it spawned
        Stow,
        // Host to clients, the private cabinet that gear was put into
        Stowed,
        // Host to clients, the pier gate opened and the exit zone is live
        ZoneArmed,
        // Host to clients, the player with this net id did not make the boat
        BadEnd,
        // Host to clients, the player with this net id plays this ending
        Evacuate
    }
}
