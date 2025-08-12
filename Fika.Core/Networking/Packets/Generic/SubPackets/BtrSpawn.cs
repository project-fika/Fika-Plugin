using EFT.GlobalEvents;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Pooling;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public class BtrSpawn : IPoolSubPacket
{
    public Vector3 Position;
    public Quaternion Rotation;
    public string PlayerProfileId;

    private BtrSpawn() { }

    public static BtrSpawn CreateInstance()
    {
        return new BtrSpawn();
    }

    public static BtrSpawn FromValue(Vector3 position, Quaternion rotation, string profileId)
    {
        BtrSpawn packet = GenericSubPacketPoolManager.Instance.GetPacket<BtrSpawn>(EGenericSubPacketType.SpawnBTR);
        packet.Position = position;
        packet.Rotation = rotation;
        packet.PlayerProfileId = profileId;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        GlobalEventHandlerClass.CreateEvent<BtrSpawnOnThePathEvent>().Invoke(Position, Rotation, PlayerProfileId);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutUnmanaged(Position);
        writer.PutUnmanaged(Rotation);
        writer.Put(PlayerProfileId);
    }

    public void Deserialize(NetDataReader reader)
    {
        Position = reader.GetUnmanaged<Vector3>();
        Rotation = reader.GetUnmanaged<Quaternion>();
        PlayerProfileId = reader.GetString();
    }

    public void Dispose()
    {
        Position = default;
        Rotation = default;
        PlayerProfileId = null;
    }
}
