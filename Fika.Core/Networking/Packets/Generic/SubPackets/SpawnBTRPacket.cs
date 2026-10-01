using EFT.GlobalEvents;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct SpawnBTRPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.SpawnBTR;

    public SpawnBTRPacket(Vector3 position, Quaternion rotation, string profileId)
    {
        Position = position;
        Rotation = rotation;
        PlayerProfileId = profileId;
    }

    public SpawnBTRPacket(NetDataReader reader)
    {
        Position = reader.GetUnmanaged<Vector3>();
        Rotation = reader.GetUnmanaged<Quaternion>();
        PlayerProfileId = reader.GetString();
    }

    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
    public readonly string PlayerProfileId;

    public readonly void Execute()
    {
        FikaGlobals.LogInfo("Received BTR spawn event from server");
        GlobalEventsController.CreateEvent<BtrSpawnOnThePathEvent>()
            .Invoke(Position, Rotation, PlayerProfileId);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(Position);
        writer.PutUnmanaged(Rotation);
        writer.Put(PlayerProfileId);
    }
}
