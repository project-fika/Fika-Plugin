using EFT.GlobalEvents;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct SpawnBTRPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.SpawnBTR;

    public SpawnBTRPacket(Vector3 position, Quaternion rotation, int playerRaidId)
    {
        Position = position;
        Rotation = rotation;
        PlayerRaidId = playerRaidId;
    }

    public SpawnBTRPacket(NetDataReader reader)
    {
        Position = reader.GetUnmanaged<Vector3>();
        Rotation = reader.GetUnmanaged<Quaternion>();
        PlayerRaidId = reader.GetInt();
    }

    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
    public readonly int PlayerRaidId;

    public readonly void Execute()
    {
        FikaGlobals.LogInfo("Received BTR spawn event from server");
        GlobalEventsController.CreateEvent<BtrSpawnOnThePathEvent>()
            .Invoke(Position, Rotation, PlayerRaidId);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(Position);
        writer.PutUnmanaged(Rotation);
        writer.Put(PlayerRaidId);
    }
}
