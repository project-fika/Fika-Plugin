using EFT.GlobalEvents;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public sealed class BtrSpawn : IPoolSubPacket
{
    public Vector3 Position;
    public Quaternion Rotation;
    public int PlayerRaidId;

    private BtrSpawn() { }

    public static BtrSpawn CreateInstance()
    {
        return new BtrSpawn();
    }

    public static BtrSpawn FromValue(Vector3 position, Quaternion rotation, int playerRaidId)
    {
        var packet = GenericSubPacketPoolManager.Instance.GetPacket<BtrSpawn>(EGenericSubPacketType.SpawnBTR);
        packet.Position = position;
        packet.Rotation = rotation;
        packet.PlayerRaidId = playerRaidId;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        FikaGlobals.LogInfo("Received BTR spawn event from server");
        GlobalEventsController.CreateEvent<BtrSpawnOnThePathEvent>()
            .Invoke(Position, Rotation, PlayerRaidId);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(Position);
        writer.PutUnmanaged(Rotation);
        writer.Put(PlayerRaidId);
    }

    public void Deserialize(NetDataReader reader)
    {
        Position = reader.GetUnmanaged<Vector3>();
        Rotation = reader.GetUnmanaged<Quaternion>();
        PlayerRaidId = reader.GetInt();
    }

    public void Dispose()
    {
        Position = default;
        Rotation = default;
        PlayerRaidId = 0;
    }
}
